using System;
using TwitterAPI.Dtos;

namespace TwitterAPI.Dtos.Annotations
{
	[System.AttributeUsage(System.AttributeTargets.Property)]
	public class Parameter : System.Attribute
	{
		public string Name { get; private set; }
		public bool Required { get; private set; }

		public Parameter(string name, bool required = false)
		{
			this.Name = name;
			this.Required = required;
		}
	}
}

