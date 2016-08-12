using System;
using TwitterAPI.Dtos.Annotations;

namespace TwitterAPI.Dtos
{
	[Route("friendships/destroy", HttpMethod.Post)]
	public class UnFollowRequest : IReturn<UserResponse>
	{
		[Parameter("screen_name", false)]
		public string ScreenName { get; set; }

		[Parameter("user_id", false)]
		public ulong UserId { get; set; }
	}
}

