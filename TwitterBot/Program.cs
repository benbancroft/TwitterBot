using System;

namespace TwitterBot
{
	class MainClass
	{
		public static void Main (string[] args)
		{

			TwitterClient client = new TwitterClient ();

			client.GetTweets();

			Console.ReadLine ();
		}
	}
}
