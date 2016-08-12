using System;
using Newtonsoft.Json;

namespace TwitterBot.Containers
{
	public static class ContainerUtils<T>
	{

		public static string Serialise (object obj)
		{
			var t = typeof (T);
			if (t.IsPrimitive || t.IsValueType || (t == typeof(string)))
				return obj.ToString();
			else
				return JsonConvert.SerializeObject (obj);
		}

		public static T Deserialise (string serialised)
		{
			var t = typeof (T);
			if (t.IsPrimitive || t.IsValueType || (t == typeof(string)))
				return serialised != null ? (T)Convert.ChangeType(serialised, t) : default(T);
			else
				return JsonConvert.DeserializeObject<T> (serialised);
		}

	}
}

