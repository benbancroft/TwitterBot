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

		private ConnectionMultiplexer localRedis;
		//private ConnectionMultiplexer remoteRedis;

		private RedisList<string> followRegexs;
		private RedisList<string> favouriteRegexs;
		private RedisList<string> blockRegexs;
		private RedisList<string> blockList;

		private RedisQueue<ulong> followQueue;
		private RedisQueue<ulong> favouriteQueue;
		private RedisSet<ulong> tweetedSet;
		private RedisSet<ulong> favouritedSet;
		private RedisSet<ulong> blockedBySet;
		private RedisSortedSet<ulong> followingSet;

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

			localRedis = ConnectionMultiplexer.Connect (ConfigurationManager.AppSettings ["localRedis"]);
			//remoteRedis = ConnectionMultiplexer.Connect (ConfigurationManager.AppSettings ["remoteRedis"]);

			this.followRegexs = new RedisList<string> (localRedis, "follow_regexs");
			if (this.followRegexs.Count <= 0)
				this.followRegexs.AddRange (new List<string> () { "follow ", "follower ", " follow&" });
			this.favouriteRegexs = new RedisList<string> (localRedis, "favourite_regexs");
			if (this.favouriteRegexs.Count <= 0)
				this.favouriteRegexs.AddRange (new List<string> () { " fav ", " favorite ", " like ", " favourite " });
			this.blockRegexs = new RedisList<string> (localRedis, "block_regexs");
			if (this.blockRegexs.Count <= 0)
				this.blockRegexs.AddRange (new List<string> () { "^((?!(retweet)|(rt )|( rt)|(#rt)|(re-tweet)).)*$" });
			this.blockList = new RedisList<string> (localRedis, "block_list");
			if (this.blockList.Count <= 0)
				this.blockList.AddRange (new List<string> () { });
			this.searchKeyWords = new RedisList<string> (localRedis, "search_keywords");
			if (this.searchKeyWords.Count <= 0)
				this.searchKeyWords.AddRange (new List<string> () { "RT to win", "Retweet and win", "retweet to win" });

			followQueue = new RedisQueue<ulong> (localRedis, "follow_requests");
			favouriteQueue = new RedisQueue<ulong> (localRedis, "favourite_requests");
			tweetedSet = new RedisSet<ulong> (localRedis, "tweets");
			favouritedSet = new RedisSet<ulong> (localRedis, "favourited");
			followingSet = new RedisSortedSet<ulong> (localRedis, "following");
			blockedBySet = new RedisSet<ulong> (localRedis, "blocked_by");

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

		public void AddBlockedBy(ulong user) {
			followingSet.Remove (user);
			blockedBySet.Add(user);
		}

		public void followUsers ()
		{

			try {

				if (followingSet.Count >= 4500) {
					try {
						
						var user = followingSet.GetLeast();

						if (user != 0) {
							twitterCtx.request (new UnFollowRequest {
								UserId = user
							});

							followingSet.Remove(user);

							Logger.LogInfo ("Unfollowed {0}", user.ToString ());
						}

					} catch (WebException ex) {
						Logger.LogError ("WebException: {0}", ex.Message);
					}/* catch (Exception ex) {
						Logger.LogError ("Error: " + ex.Message + ", Inner: " + ex.InnerException != null ? ex.InnerException.Message : "");
					}*/
				} else {
					
					var user = followQueue.Pop ();

					if (user != 0) {
						if (!followingSet.Contains(user)) {
							try {
								twitterCtx.request (new FollowRequest {
									UserId = user,
									Follow = true
								});

								followingSet.Add(user, (int)(DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1))).TotalSeconds);
								Logger.LogInfo ("Followed {0}", user);


							} catch (ApiException ex) {

								foreach (var error in ex.ErrorResponse.Errors) {
									Logger.LogInfo ("Api Error - " + error.ToString ());

									//Blocked by user
									if (error.Code == 162) {
										AddBlockedBy (user);
										Logger.LogError ("Blocked from following by user: {0}", user);
									}else{
										Logger.LogError ("Api Error: {0}", error.ToString ());

										//throw back out
										throw ex;
									}
								}

							} catch (WebException ex) {
								Logger.LogError ("WebException: {0}", ex.Message);

								followQueue.Push (user);
							} catch (Exception ex) {
								Logger.LogError ("Error: " + ex.Message);

								followQueue.Push (user);

								throw ex;
							}
						} else {
							followingSet.Add(user, (int)(DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1))).TotalSeconds);
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

				var tweetId = favouriteQueue.Pop ();
				if (tweetId != 0) {
					try {
						twitterCtx.request (new FavouriteRequest {
							Id = tweetId
						});
								
						favouritedSet.Add(tweetId);
						Logger.LogInfo ("Favourited {0}", tweetId.ToString ());


					} catch (ApiException ex) {

						foreach (var error in ex.ErrorResponse.Errors) {
							Console.WriteLine ("Api Error - " + error.ToString ());

							//Already favourited
							if (error.Code == 139 || error.Code == 136) {
								favouritedSet.Add(tweetId);
								Logger.LogError ("Already favourited: {0}", tweetId);
							} else {
								favouriteQueue.Push (tweetId);

								Logger.LogError ("Api Error: {0}", error.ToString ());

								//throw back out
								throw ex;
							}
						}

					} catch (WebException ex) {
						Logger.LogError ("WebException: {0}", ex.Message);

						favouriteQueue.Push (tweetId);
					} catch (Exception ex) {
						Logger.LogError ("Error: " + ex.Message);
				
						favouriteQueue.Push (tweetId);

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

					tweetedSet.Add(tweet.Id);

					using (System.IO.StreamWriter file = new System.IO.StreamWriter (@"tweets.txt", true)) {
						file.WriteLine (tweet.Text.Replace ("\n", string.Empty).Replace ("\r", string.Empty));
					}

					try {

						if (tweet.Follow)
							followQueue.Push (tweet.UserId);

						if (tweet.Favourite)
							favouriteQueue.Push (tweet.Id);

						twitterCtx.request (new RetweetRequest {
							Id = tweet.Id
						});

						Logger.LogInfo (
							"Priority: {1}, Follow? {3}, Favourite?: {4}, QueueSize: {5}, URL: https://twitter.com/{2}/status/{0}", 
							tweet.Id, tweet.Priority, tweet.UserName, tweet.Follow, tweet.Favourite, size);

					} catch (ApiException ex) {

						foreach (var error in ex.ErrorResponse.Errors) {
							//Blocked
							switch (error.Code) {
							case 136:
								AddBlockedBy(tweet.UserId);
								Logger.LogError ("Blocked from retweeting by user: {0}", tweet.UserId);
								break;
							case 144:
								Logger.LogError ("Tweet doesn't exist anymore: {0}", tweet.Id);
								break;
							default:
								Logger.LogError ("Api Error: {0}", error.ToString ());

								//throw back out
								throw ex;
							}
						}

					} catch (WebException ex) {
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

						//TODO - in future when syncing, try sending to remote even if blocked locally
						if (blockedBySet.Contains(tweetData.UserId) || tweetedSet.Contains(tweetData.Id)) {
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