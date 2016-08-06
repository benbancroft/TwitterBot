using System;
using TwitterAPI.Dtos;

namespace TwitterAPI.Dtos.Annotations
{
	[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
	public class Route : System.Attribute
	{
		public string Uri { get; private set; }
		public HttpMethod Method { get; private set; }

		public Route(string uri, HttpMethod method)
		{
			this.Uri = uri;
			this.Method = method;
		}
	}
}

