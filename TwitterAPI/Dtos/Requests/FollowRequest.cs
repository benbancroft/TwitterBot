using System;
using TwitterAPI.Dtos.Annotations;

namespace TwitterAPI.Dtos
{
	[Route("friendships/create", HttpMethod.Post)]
	public class FollowRequest : IReturn<UserResponse>
	{
		[Parameter("screen_name", false)]
		public string ScreenName { get; set; }

		[Parameter("user_id", false)]
		public ulong UserId { get; set; }

		[Parameter("follow", false)]
		public bool Follow { get; set; }
	}
}

