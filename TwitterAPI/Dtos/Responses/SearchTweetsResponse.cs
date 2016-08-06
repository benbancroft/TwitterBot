using System;
using System.Collections.Generic;

namespace TwitterAPI.Dtos
{
	public class SearchTweetsResponse
	{
		public List<StatusResponse> Statuses { get; set; }
	}
}

