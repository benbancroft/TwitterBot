using System;

namespace TwitterAPI.Exceptions
{
	public class InvalidRouteException : Exception
	{
		public InvalidRouteException () : base("Invalid route on request class")
		{
		}
	}
}

