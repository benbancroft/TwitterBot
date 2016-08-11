using NUnit.Framework;
using System;

namespace TwitterBot
{
	[TestFixture ()]
	public class DEPQTweetTest
	{
		[Test ()]
		public void PriorityTweet ()
		{
			DEPQ<Tweet> tweetQueue = new DEPQ<Tweet>();

			var tweetData1 = new Tweet { Priority = TweetPriority.MEDIUM, Id = 1, UserName = "bob", Tweeted = DateTime.Now, Follow = false };
			var tweetData2 = new Tweet { Priority = TweetPriority.HIGH, Id = 2, UserName = "bill", Tweeted = DateTime.Now, Follow = false };
			var tweetData3 = new Tweet { Priority = TweetPriority.LOW, Id = 3, UserName = "borris", Tweeted = DateTime.Now, Follow = false };
			var tweetData4 = new Tweet { Priority = TweetPriority.VERIFIED, Id = 4, UserName = "ben", Tweeted = DateTime.Now, Follow = false };

			Assert.AreNotEqual (tweetData2, tweetData3);

			Assert.AreEqual (tweetQueue.Size (), 0);

			tweetQueue.Add (tweetData1);
			tweetQueue.Add (tweetData2);
			tweetQueue.Add (tweetData3);

			Assert.IsTrue (tweetQueue.Contains(tweetData2));
			Assert.IsFalse (tweetQueue.Contains(tweetData4));

			tweetQueue.Add (tweetData4);

			Assert.IsTrue (tweetQueue.Contains(tweetData4));

			Assert.AreEqual (tweetQueue.Size (), 4);

			Assert.AreEqual (tweetQueue.GetMost(), tweetData4);
			Assert.AreEqual (tweetQueue.GetMost(), tweetData2);
			Assert.AreEqual (tweetQueue.GetMost(), tweetData1);
			Assert.AreEqual (tweetQueue.GetMost(), tweetData3);

			Assert.AreEqual (0, tweetQueue.Size ());
		}

		[Test ()]
		public void CompareTweet ()
		{
			var tweetData1 = new Tweet { Priority = TweetPriority.HIGH, Id = 1, UserName = "bob", Tweeted = DateTime.Now, Follow = false };
			var tweetData2 = new Tweet { Priority = TweetPriority.HIGH, Id = 1, UserName = "bob", Tweeted = DateTime.Now, Follow = false };

			Assert.AreEqual (tweetData1, tweetData2);

			DEPQ<Tweet> tweetQueue = new DEPQ<Tweet>();

			Assert.AreEqual (0, tweetQueue.Size ());

			tweetQueue.Add (tweetData1);

			Assert.AreEqual (1, tweetQueue.Size ());

			if (!tweetQueue.Contains(tweetData2)) tweetQueue.Add(tweetData2);

			Assert.AreEqual (1, tweetQueue.Size ());
		}
	}
}

