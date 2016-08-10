using System;
using System.Linq;
using System.Configuration;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using TwitterAPI;
using TwitterAPI.Dtos;
using TwitterAPI.Exceptions;

namespace TwitterBot
{
	public class TwitterClient
	{
		private TwitterApiClient twitterCtx;

		private ConnectionMultiplexer redis;
		private IDatabase db;

		private RedisList<String> followRegexs;
		private RedisList<String> favouriteRegexs;
		private RedisList<String> blockRegexs;
		private RedisList<String> blockList;

		private int searchIndex = 0;
		private RedisList<String> searchKeyWords;

		private readonly object tweetQueueLock = new object ();
		private DEPQ<Tweet> tweetQueue = new DEPQ<Tweet> ();

		private long lockCount = 0;
		private long getThreadCount = 0;

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

			this.followRegexs = new RedisList<String> (redis, "FollowRegexs");
			if (this.followRegexs.Count <= 0)
				this.followRegexs.AddRange (new List<String> () { "follow ", "follower ", " follow&" });
			this.favouriteRegexs = new RedisList<String> (redis, "FavouriteRegexs");
			if (this.favouriteRegexs.Count <= 0)
				this.favouriteRegexs.AddRange (new List<String> () { " fav ", " favorite ", " like ", " favourite " });
			this.blockRegexs = new RedisList<String> (redis, "BlockRegexs");
			if (this.blockRegexs.Count <= 0)
				this.blockRegexs.AddRange (new List<String> () { "^((?!(retweet)|(rt )|( rt)|(#rt)|(re-tweet)).)*$" });
			this.blockList = new RedisList<String> (redis, "BlockList");
			if (this.blockList.Count <= 0)
				this.blockList.AddRange (new List<String> () { });
			this.searchKeyWords = new RedisList<String> (redis, "SearchKeywords");
			if (this.searchKeyWords.Count <= 0)
				this.searchKeyWords.AddRange (new List<String> () { "RT to win", "Retweet and win", "retweet to win" });

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

					} catch (Exception ex) {
						Logger.LogError ("Error: " + ex.Message + ", Inner: " + ex.InnerException != null ? ex.InnerException.Message : "");
					}
				} else {

					RedisValue user = db.ListLeftPop ("follow_requests");

					if (user.HasValue) {
						if (!db.SortedSetRank ("following", user).HasValue) {
							try {
								twitterCtx.request (new FollowRequest {
									UserId = user.ToString (),
									Follow = true
								});

								db.SortedSetAdd ("following", user.ToString (), (int)(DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1))).TotalSeconds);
								Logger.LogInfo ("Followed {0}", user.ToString ());


							} catch (ApiException ex) {

								foreach (var error in ex.ErrorResponse.Errors) {
									Logger.LogInfo ("Api Error - " + error.ToString ());

									//Blocked by user
									if (error.Code != 162) {
										db.ListRightPush ("follow_requests", user.ToString ());
									}
								}

							} catch (Exception ex) {
								Logger.LogError ("Error: " + ex.Message);

								db.ListRightPush ("follow_requests", user.ToString ());

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

				RedisValue tweetId = db.ListLeftPop ("favourite_requests");

				if (tweetId.HasValue) {
					try {
						twitterCtx.request (new FavouriteRequest {
							Id = ulong.Parse (tweetId.ToString ())
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
								db.ListRightPush ("favourite_requests", tweetId.ToString ());
							}
						}

					} catch (Exception ex) {
						Logger.LogError ("Error: " + ex.Message);
				
						db.ListRightPush ("favourite_requests", tweetId.ToString ());

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

				Interlocked.Increment (ref lockCount);
				lock (tweetQueueLock) {
					tweet = tweetQueue.getMost ();
					size = tweetQueue.size ();
				}
				Interlocked.Decrement (ref lockCount);
					
				if (tweet != null) {

					db.SetAdd ("tweets", tweet.Id.ToString ());

					using (System.IO.StreamWriter file = new System.IO.StreamWriter (@"tweets.txt", true)) {
						file.WriteLine (tweet.Text.Replace ("\n", String.Empty).Replace ("\r", String.Empty));
					}

					//try {

						if (tweet.Follow)
							db.ListRightPush ("follow_requests", tweet.UserId.ToString ());

						if (tweet.Favourite)
							db.ListRightPush ("favourite_requests", tweet.Id.ToString ());

						twitterCtx.request (new RetweetRequest {
							Id = tweet.Id
						});

						Logger.LogInfo (
							"Priority: {1}, Follow? {3}, Favourite?: {4}, QueueSize: {5}, URL: https://twitter.com/{2}/status/{0}, LC: {6}", 
							tweet.Id, tweet.Priority, tweet.UserName, tweet.Follow, tweet.Favourite, size, Interlocked.Read (ref lockCount));

					/*} catch (Exception ex) {
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

			//Console.WriteLine ("Get thread count: {0}", Interlocked.Read(ref getThreadCount));

			Interlocked.Increment (ref getThreadCount);

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

						Interlocked.Increment (ref lockCount);
						lock (tweetQueueLock) {

							if (blockList.Any (i => i.Equals (tweetData.UserId))) {
							} else if (blockRegexs.Any (i => Regex.Matches (tweet.Text.ToLower (), i.ToLower ()).Count > 0)) {
								//Logger.LogInfo("Matched blocked tweet: " + tweetData.Id);
							} else {

								if (!tweetQueue.Contains (tweetData))
									tweetQueue.add (tweetData);
								if (tweetQueue.size () > 100)
									tweetQueue.getLeast ();

							}
						}
						Interlocked.Decrement (ref lockCount);

					}
				}

			} catch (TimeoutException ex) {

				Logger.LogError ("Redis operation timed out: {0}", ex.Message);

			} catch (ApiException ex) {

				foreach (var error in ex.ErrorResponse.Errors) {
					//Rate limit
					if (error.Code == 88) {
						Logger.LogInfo ("Rate limit exceeded for GetTweets - tailing back");
						getTweetsTimer.Change (15, 5);
					} else {
						Logger.LogError ("Api Error - " + error.ToString ());

						//throw back out
						throw ex;
					}
				}

			} /*catch (Exception ex) {
				Logger.LogError ("Error: " + ex.Message + " ExType: " + ex.GetType ().ToString ());
			}*/

			Interlocked.Decrement (ref getThreadCount);
		}
	}
}