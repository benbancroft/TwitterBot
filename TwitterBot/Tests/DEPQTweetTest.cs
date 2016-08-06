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

			Assert.AreEqual (tweetQueue.size (), 0);

			tweetQueue.add (tweetData1);
			tweetQueue.add (tweetData2);
			tweetQueue.add (tweetData3);

			Assert.IsTrue (tweetQueue.Contains(tweetData2));
			Assert.IsFalse (tweetQueue.Contains(tweetData4));

			tweetQueue.add (tweetData4);

			Assert.IsTrue (tweetQueue.Contains(tweetData4));

			Assert.AreEqual (tweetQueue.size (), 4);

			Assert.AreEqual (tweetQueue.getMost(), tweetData4);
			Assert.AreEqual (tweetQueue.getMost(), tweetData2);
			Assert.AreEqual (tweetQueue.getMost(), tweetData1);
			Assert.AreEqual (tweetQueue.getMost(), tweetData3);

			Assert.AreEqual (0, tweetQueue.size ());
		}

		[Test ()]
		public void CompareTweet ()
		{
			var tweetData1 = new Tweet { Priority = TweetPriority.HIGH, Id = 1, UserName = "bob", Tweeted = DateTime.Now, Follow = false };
			var tweetData2 = new Tweet { Priority = TweetPriority.HIGH, Id = 1, UserName = "bob", Tweeted = DateTime.Now, Follow = false };

			Assert.AreEqual (tweetData1, tweetData2);

			DEPQ<Tweet> tweetQueue = new DEPQ<Tweet>();

			Assert.AreEqual (0, tweetQueue.size ());

			tweetQueue.add (tweetData1);

			Assert.AreEqual (1, tweetQueue.size ());

			if (!tweetQueue.Contains(tweetData2)) tweetQueue.add(tweetData2);

			Assert.AreEqual (1, tweetQueue.size ());
		}
	}
}

