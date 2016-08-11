using System;
using System.Linq;
using System.Configuration;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;

using TwitterAPI;
using TwitterAPI.Dtos;
using TwitterAPI.Exceptions;

using TwitterBot.Containers;

namespace TwitterBot
{
	public class TwitterClient
	{
		private TwitterApiClient twitterCtx;

		private ConnectionMultiplexer redis;
		private IDatabase db;

		private RedisList<string> followRegexs;
		private RedisList<string> favouriteRegexs;
		private RedisList<string> blockRegexs;
		private RedisList<string> blockList;

		private RedisQueue<string> followQueue;
		private RedisQueue<ulong> favouriteQueue;


		private int searchIndex = 0;
		private RedisList<string> searchKeyWords;

		private readonly object tweetQueueLock = new object ();
		private DEPQ<Tweet> tweetQueue = new DEPQ<Tweet> ();

		private Timer getTweetsTimer;

		public TwitterClient ()
		{

			twitterCtx = new TwitterApiClient (new OAuthTokens {
				ConsumerKey = ConfigurationManager.AppSettings ["consumerKey"],
				ConsumerSecret = ConfigurationManager.AppSettings ["consumerSecret"],
				AccessToken = ConfigurationManager.AppSettings ["accessToken"],
				AccessTokenSecret = ConfigurationManager.AppSettings ["accessTokenSecret"]
			});

			redis = ConnectionMultiplexer.Connect (ConfigurationManager.AppSettings ["localRedis"]);

			this.followRegexs = new RedisList<string> (redis, "follow_regexs");
			if (this.followRegexs.Count <= 0)
				this.followRegexs.AddRange (new List<string> () { "follow ", "follower ", " follow&" });
			this.favouriteRegexs = new RedisList<string> (redis, "favourite_regexs");
			if (this.favouriteRegexs.Count <= 0)
				this.favouriteRegexs.AddRange (new List<string> () { " fav ", " favorite ", " like ", " favourite " });
			this.blockRegexs = new RedisList<string> (redis, "block_regexs");
			if (this.blockRegexs.Count <= 0)
				this.blockRegexs.AddRange (new List<string> () { "^((?!(retweet)|(rt )|( rt)|(#rt)|(re-tweet)).)*$" });
			this.blockList = new RedisList<string> (redis, "block_list");
			if (this.blockList.Count <= 0)
				this.blockList.AddRange (new List<string> () { });
			this.searchKeyWords = new RedisList<string> (redis, "search_keywords");
			if (this.searchKeyWords.Count <= 0)
				this.searchKeyWords.AddRange (new List<string> () { "RT to win", "Retweet and win", "retweet to win" });

			followQueue = new RedisQueue<string> (redis, "follow_requests");
			favouriteQueue = new RedisQueue<ulong> (redis, "favourite_requests");

			db = redis.GetDatabase ();


			//start timers

			//getTweetsTimer
			getTweetsTimer = new Timer (
				e => GetTweets (),  
				null, 
				TimeSpan.Zero, 
				TimeSpan.FromSeconds (5));

			//followUsersTimer
			new System.Threading.Timer (
				e => followUsers (),  
				null, 
				TimeSpan.Zero, 
				TimeSpan.FromSeconds (5));

			//favouriteTweetsTimer
			new System.Threading.Timer (
				e => favouriteTweets (),  
				null, 
				TimeSpan.Zero, 
				TimeSpan.FromSeconds (5));

			//retweetTweetsTimer
			new System.Threading.Timer (
				e => retweetTweet (),  
				null, 
				TimeSpan.FromSeconds (5), 
				TimeSpan.FromSeconds (45));

		}

		public void followUsers ()
		{

			try {

				if (db.SortedSetLength ("following") >= 4500) {
					try {

						var userRange = db.SortedSetRangeByScore ("following", double.NegativeInfinity, double.PositiveInfinity, Exclude.None, Order.Ascending, 0, 1);

						var user = userRange.First ();

						if (user.HasValue) {
							twitterCtx.request (new UnFollowRequest {
								UserId = user.ToString ()
							});

							db.SortedSetRemove ("following", user.ToString ());

							Logger.LogInfo ("Unfollowed {0}", user.ToString ());
						}

					} catch (WebException ex) {
						Logger.LogError ("WebException: {0}", ex.Message);
					}/* catch (Exception ex) {
						Logger.LogError ("Error: " + ex.Message + ", Inner: " + ex.InnerException != null ? ex.InnerException.Message : "");
					}*/
				} else {
					
					var user = followQueue.Pop();

					if (user != null){
						if (!db.SortedSetRank ("following", user).HasValue) {
							try {
								twitterCtx.request (new FollowRequest {
									UserId = user,
									Follow = true
								});

								db.SortedSetAdd ("following", user.ToString (), (int)(DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1))).TotalSeconds);
								Logger.LogInfo ("Followed {0}", user.ToString ());


							} catch (ApiException ex) {

								foreach (var error in ex.ErrorResponse.Errors) {
									Logger.LogInfo ("Api Error - " + error.ToString ());

									//Blocked by user
									if (error.Code != 162) {
										followQueue.Push(user);
									}
								}

							} catch (WebException ex) {
								Logger.LogError ("WebException: {0}", ex.Message);

								followQueue.Push(user);
							} catch (Exception ex) {
								Logger.LogError ("Error: " + ex.Message);

								followQueue.Push(user);

								throw ex;
							}
						} else {
							db.SortedSetAdd ("following", user.ToString (), (int)(DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1))).TotalSeconds);
							Logger.LogInfo ("Already followed {0}", user.ToString ());
						}
					}
				}

			} catch (TimeoutException ex) {

				Logger.LogError ("Redis operation timed out: {0}", ex.Message);

			}
		}

		public void favouriteTweets ()
		{

			try {

				var tweetId = favouriteQueue.Pop();
				if (tweetId != 0){
					try {
						twitterCtx.request (new FavouriteRequest {
							Id = tweetId
						});

						db.SetAdd ("favourited", tweetId.ToString ());
						Logger.LogInfo ("Favourited {0}", tweetId.ToString ());


					} catch (ApiException ex) {

						foreach (var error in ex.ErrorResponse.Errors) {
							Console.WriteLine ("Api Error - " + error.ToString ());

							//Already favourited
							if (error.Code == 139 || error.Code == 136) {
								db.SetAdd ("favourited", tweetId.ToString ());
							} else {
								favouriteQueue.Push(tweetId);
							}
						}

					} catch (WebException ex) {
						Logger.LogError ("WebException: {0}", ex.Message);

						favouriteQueue.Push(tweetId);
					} catch (Exception ex) {
						Logger.LogError ("Error: " + ex.Message);
				
						favouriteQueue.Push(tweetId);

						throw ex;
					}
				}
			} catch (TimeoutException ex) {

				Logger.LogError ("Redis operation timed out: {0}", ex.Message);

			}
		}

		public void retweetTweet ()
		{

			try {

				Tweet tweet = null;

				int size = 0;

				lock (tweetQueueLock) {
					tweet = tweetQueue.GetMost ();
					size = tweetQueue.Size ();
				}
					
				if (tweet != null) {

					db.SetAdd ("tweets", tweet.Id.ToString ());

					using (System.IO.StreamWriter file = new System.IO.StreamWriter (@"tweets.txt", true)) {
						file.WriteLine (tweet.Text.Replace ("\n", string.Empty).Replace ("\r", string.Empty));
					}

					try {

						if (tweet.Follow)
							followQueue.Push(tweet.UserId.ToString ());

						if (tweet.Favourite)
							favouriteQueue.Push(tweet.Id);

						twitterCtx.request (new RetweetRequest {
							Id = tweet.Id
						});

						Logger.LogInfo (
							"Priority: {1}, Follow? {3}, Favourite?: {4}, QueueSize: {5}, URL: https://twitter.com/{2}/status/{0}", 
							tweet.Id, tweet.Priority, tweet.UserName, tweet.Follow, tweet.Favourite, size);

					} catch (ApiException ex) {

						foreach (var error in ex.ErrorResponse.Errors) {
							//Blocked
							if (error.Code == 136) {
								Logger.LogInfo ("Blocked from retweeting by user: {0}", tweet.UserName);
							} else {
								Logger.LogError ("Api Error: {0}", error.ToString ());

								//throw back out
								throw ex;
							}
						}

					}catch (WebException ex) {
						Logger.LogError ("WebException: {0}", ex.Message);
					}/*catch (Exception ex) {
						Logger.LogError ("Error: " + ex.Message);
						//Console.WriteLine ("Error: " + ex.Message + ", Inner: " + ex.InnerException != null ? ex.InnerException.Message : "");
					}*/
				}

			} catch (TimeoutException ex) {

				Logger.LogError ("Redis operation timed out: {0}", ex.Message);

			}
		}

		public void GetTweets ()
		{

			try {

				if (searchKeyWords.Count <= 0)
					return;

				if (searchKeyWords.Count - 1 < searchIndex)
					searchIndex = 0;

				var keyword = searchKeyWords [searchIndex++];

				var searchResponse = twitterCtx.request (new SearchTweetsRequest {
					Query = keyword
				});

				//var tweets = db.SetScan ("tweets").ToArray();

				if (searchResponse != null && searchResponse.Statuses != null) {
					foreach (var topTweet in searchResponse.Statuses) {

						StatusResponse tweet = topTweet;

						if (topTweet.RetweetedStatus != null && topTweet.RetweetedStatus.Id > 0) {
							//block tweet.User.ID
							tweet = topTweet.RetweetedStatus;
						}
													
						TweetPriority prioity = TweetPriority.LOW;

						if (tweet.User.Verified)
							prioity = TweetPriority.VERIFIED;
						else if (tweet.RetweetCount > 500 || tweet.User.FollowersCount > 5000)
							prioity = TweetPriority.HIGH;
						else if (tweet.RetweetCount > 100 || tweet.User.FollowersCount > 3000)
							prioity = TweetPriority.MEDIUM;


						var tweetData = new Tweet {
							Priority = prioity,
							Id = tweet.Id,
							UserId = tweet.User.Id,
							UserName = tweet.User.ScreenName,
							Tweeted = tweet.CreatedAt,
							Text = tweet.Text
						};

						tweetData.Follow = followRegexs.Any (i => Regex.Matches (tweet.Text.ToLower (), i.ToLower ()).Count > 0);
						tweetData.Favourite = favouriteRegexs.Any (i => Regex.Matches (tweet.Text.ToLower (), i.ToLower ()).Count > 0);

						if (db.SetContains ("tweets", tweetData.Id.ToString ())) {
							//Console.WriteLine("Already have tweet: " + tweetData.Id);
							continue;
						}
							
						lock (tweetQueueLock) {

							if (blockList.Any (i => i.Equals (tweetData.UserId))) {
							} else if (blockRegexs.Any (i => Regex.Matches (tweet.Text.ToLower (), i.ToLower ()).Count > 0)) {
								//Logger.LogInfo("Matched blocked tweet: " + tweetData.Id);
							} else {

								if (!tweetQueue.Contains (tweetData))
									tweetQueue.Add (tweetData);
								if (tweetQueue.Size () > 100)
									tweetQueue.GetLeast ();

							}
						}

					}
				}

			} catch (TimeoutException ex) {

				Logger.LogError ("Redis operation timed out: {0}", ex.Message);

			} catch (WebException ex) {
				Logger.LogError ("WebException: {0}", ex.Message);
			} catch (ApiException ex) {

				foreach (var error in ex.ErrorResponse.Errors) {
					//Rate limit
					if (error.Code == 88) {
						Logger.LogInfo ("Rate limit exceeded for GetTweets - tailing back");
						getTweetsTimer.Change (TimeSpan.FromSeconds (15), TimeSpan.FromSeconds (5));
					} else {
						Logger.LogError ("Api Error: {0}", error.ToString ());

						//throw back out
						throw ex;
					}
				}

			} /*catch (Exception ex) {
				Logger.LogError ("Error: " + ex.Message + " ExType: " + ex.GetType ().ToString ());
			}*/
		}
	}
}