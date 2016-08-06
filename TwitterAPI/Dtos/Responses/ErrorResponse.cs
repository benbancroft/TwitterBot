using System;

namespace TwitterAPI.Dtos
{
	public class ErrorResponse
	{
		public string Message { get; set; }

		public int Code { get; set; }

		public override string ToString(){
			return "Code: " + this.Code + ", Message: " + this.Message;
		}
	}
}

