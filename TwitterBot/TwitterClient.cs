using System;
using System.Linq;
using System.Configuration;
using StackExchange.Redis;
using Newtonsoft.Json.Linq;
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

		private List<String> followRegexs = new List<String> () { "follow ", "follower ", " follow&" };
		private List<String> favouriteRegexs = new List<String> () { " fav ", " favorite ", " like ", " favourite " };
		private List<String> blockRegexs = new List<String> () { "^((?!(retweet)|(rt )|( rt)|(#rt)|(re-tweet)).)*$" };
		private List<String> blockList = new List<String> () { };

		private int searchIndex = 0;
		private List<String> searchKeyWords = new List<String> () { "RT to win", "Retweet and win", "retweet to win" };

		private readonly object tweetQueueLock = new object ();
		private DEPQ<Tweet> tweetQueue = new DEPQ<Tweet> ();

		private System.Threading.Timer getTweetsTimer;
		private System.Threading.Timer followUsersTimer;
		private System.Threading.Timer favouriteTweetsTimer;
		private System.Threading.Timer retweetTweetsTimer;

		private long lockCount = 0;
		private long getThreadCount = 0;

		public TwitterClient ()
		{

			twitterCtx = new TwitterApiClient (new OAuthTokens {
				ConsumerKey = ConfigurationManager.AppSettings ["consumerKey"],
				ConsumerSecret = ConfigurationManager.AppSettings ["consumerSecret"],
				AccessToken = ConfigurationManager.AppSettings ["accessToken"],
				AccessTokenSecret = ConfigurationManager.AppSettings ["accessTokenSecret"]
			});

			redis = ConnectionMultiplexer.Connect ("127.0.0.1");

			db = redis.GetDatabase ();

			//start timers

			getTweetsTimer = new System.Threading.Timer (
				e => GetTweets (),  
				null, 
				TimeSpan.Zero, 
				TimeSpan.FromSeconds (5));

			followUsersTimer = new System.Threading.Timer (
				e => followUsers (),  
				null, 
				TimeSpan.Zero, 
				TimeSpan.FromSeconds (5));

			favouriteTweetsTimer = new System.Threading.Timer (
				e => favouriteTweets (),  
				null, 
				TimeSpan.Zero, 
				TimeSpan.FromSeconds (5));

			retweetTweetsTimer = new System.Threading.Timer (
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

							Console.WriteLine ("Unfollowed {0}", user.ToString ());
						}

					} catch (Exception ex) {
						Console.WriteLine ("Error: " + ex.Message + ", Inner: " + ex.InnerException != null ? ex.InnerException.Message : "");
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
								Console.WriteLine ("Followed {0}", user.ToString ());


							} catch (ApiException ex) {

								foreach (var error in ex.ErrorResponse.Errors) {
									Console.WriteLine ("Api Error - " + error.ToString ());

									//Blocked by user
									if (error.Code != 162) {
										db.ListRightPush ("follow_requests", user.ToString ());
									}
								}

							} catch (Exception ex) {
								Console.WriteLine ("Error: " + ex.Message);

								db.ListRightPush ("follow_requests", user.ToString ());
							}
						} else {
							db.SortedSetAdd ("following", user.ToString (), (int)(DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1))).TotalSeconds);
							Console.WriteLine ("Already followed {0}", user.ToString ());
						}
					}
				}

			} catch (TimeoutException ex) {

				Console.WriteLine ("Redis operation timed out: {0}", ex.Message);

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
						Console.WriteLine ("Favourited {0}", tweetId.ToString ());


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
						Console.WriteLine ("Error: " + ex.Message);
				
						db.ListRightPush ("favourite_requests", tweetId.ToString ());
					}
				}

			} catch (TimeoutException ex) {

				Console.WriteLine ("Redis operation timed out: {0}", ex.Message);

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

				if (tweet != null)
					db.SetAdd ("tweets", tweet.Id.ToString ());
					
				if (tweet != null) {

					try {

						if (tweet.Follow)
							db.ListRightPush ("follow_requests", tweet.UserId.ToString ());

						if (tweet.Favourite)
							db.ListRightPush ("favourite_requests", tweet.Id.ToString ());

						twitterCtx.request (new RetweetRequest {
							Id = tweet.Id
						});

						Console.WriteLine (
							"Priority: {1}, Follow? {3}, Favourite?: {4}, QueueSize: {5}, URL: https://twitter.com/{2}/status/{0}, LC: {6}", 
							tweet.Id, tweet.Priority, tweet.UserName, tweet.Follow, tweet.Favourite, size, Interlocked.Read (ref lockCount));

					} catch (Exception ex) {
						Console.WriteLine ("Error: " + ex.Message);
						//Console.WriteLine ("Error: " + ex.Message + ", Inner: " + ex.InnerException != null ? ex.InnerException.Message : "");
					}
				}

			} catch (TimeoutException ex) {

				Console.WriteLine ("Redis operation timed out: {0}", ex.Message);

			}
		}

		public void GetTweets ()
		{

			//Console.WriteLine ("Get thread count: {0}", Interlocked.Read(ref getThreadCount));

			Interlocked.Increment (ref getThreadCount);

			if (searchKeyWords.Count <= 0)
				return;

			if (searchKeyWords.Count - 1 < searchIndex)
				searchIndex = 0;

			var keyword = searchKeyWords [searchIndex++];

			try {
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
							Tweeted = tweet.CreatedAt
						};

						tweetData.Follow = followRegexs.Any (i => Regex.Matches (tweet.Text.ToLower (), i.ToLower ()).Count > 0);
						tweetData.Favourite = favouriteRegexs.Any (i => Regex.Matches (tweet.Text.ToLower (), i.ToLower ()).Count > 0);

						if (db.SetContains ("tweets", tweetData.Id.ToString ())) {
							//Console.WriteLine("Already have tweet: " + tweetData.Id);
							continue;
						}

						Interlocked.Increment (ref lockCount);
						lock (tweetQueueLock) {

							if (!blockList.Any (i => i.Equals (tweetData.UserName)) && !blockRegexs.Any (i => Regex.Matches (tweet.Text.ToLower (), i.ToLower ()).Count > 0)) {

								if (!tweetQueue.Contains (tweetData))
									tweetQueue.add (tweetData);
								if (tweetQueue.size () > 100)
									tweetQueue.getLeast ();

							}

						}
						Interlocked.Decrement (ref lockCount);

					}
				}

			} catch (Exception ex) {
				Console.WriteLine ("Error: " + ex.Message + " ExType: " + ex.GetType ().ToString ());
			}

			Interlocked.Decrement (ref getThreadCount);
		}
	}
}