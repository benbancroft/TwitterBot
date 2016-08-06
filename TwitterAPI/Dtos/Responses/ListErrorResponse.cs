using System;
using System.Collections.Generic;

namespace TwitterAPI.Dtos
{
	public class ListErrorResponse
	{
		public List<ErrorResponse> Errors { get; set; }

		public override string ToString(){
			return string.Join(",", this.Errors);
		}
	}
}

