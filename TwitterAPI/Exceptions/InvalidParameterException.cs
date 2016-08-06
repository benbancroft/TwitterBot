using System;

namespace TwitterAPI.Exceptions
{
	public class InvalidParameterException : Exception
	{
		public InvalidParameterException () : base("A required parameter is null")
		{
		}
	}
}

