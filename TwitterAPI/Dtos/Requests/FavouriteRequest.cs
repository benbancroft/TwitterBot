using System;
using TwitterAPI.Dtos.Annotations;

namespace TwitterAPI.Dtos
{
	[Route("favorites/create", HttpMethod.Post)]
	public class FavouriteRequest : IReturn<StatusResponse>
	{
		[Parameter("id", true)]
		public ulong Id { get; set; }

		[Parameter("include_entities", false)]
		public bool IncludeEntities { get; set; }
	}
}

