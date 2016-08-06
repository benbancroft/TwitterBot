using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;

using TwitterAPI.Dtos;

namespace TwitterAPI
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			TwitterApiClient client = new TwitterApiClient (new OAuthTokens {
				ConsumerKey = "8X2kK03AnyxnwXE9ghixiOPhY",
				ConsumerSecret = "q7np1ikW7jNDem7lok2PWnUL9Bd0jFPuzjAFUzb3qCroXB8dwP",
				AccessToken = "4867472865-2I1dI5VO8nZ8niZTm2ewMtATtMK70ulgkuuSBNL",
				AccessTokenSecret = "rZCsw476GQ81AMdUEOPRPeqYLm5LVJkdTLgfVW6ZbnqfN"
			});

			var response = client.request (new SearchTweetsRequest {
				Query = "testing123"
			});

			Console.WriteLine (JsonConvert.SerializeObject(response));


		}
	}
}
