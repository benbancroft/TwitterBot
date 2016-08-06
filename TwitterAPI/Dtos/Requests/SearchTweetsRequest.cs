using System;
using TwitterAPI.Dtos.Annotations;

namespace TwitterAPI.Dtos
{
	[Route("search/tweets", HttpMethod.Get)]
	public class SearchTweetsRequest : IReturn<SearchTweetsResponse>
	{
		[Parameter("q", true)]
		public string Query { get; set; }
	}
}

