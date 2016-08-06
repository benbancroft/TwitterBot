using System;
using TwitterAPI.Dtos;
using System.Net;

namespace TwitterAPI.Exceptions
{
	public class ApiException : Exception
	{
		public ListErrorResponse ErrorResponse { get; set; }
		public HttpStatusCode StatusCode { get; set; }


		public ApiException (ListErrorResponse errorResponse, HttpStatusCode statusCode) : base(errorResponse != null ? errorResponse.ToString() : "Http Error Code: " + statusCode.ToString())
		{
			this.ErrorResponse = errorResponse;
			this.StatusCode = statusCode;
		}
	}
}

