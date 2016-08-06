using System;
using TwitterAPI.Dtos.Annotations;

namespace TwitterAPI.Dtos
{
	[Route("statuses/retweet/:id", HttpMethod.Post)]
	public class RetweetRequest : IReturn<StatusResponse>
	{
		[Parameter("id", true)]
		public ulong Id { get; set; }

		[Parameter("trim_user", false)]
		public bool TrimUser { get; set; }
	}
}

