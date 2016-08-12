using System;
using StackExchange.Redis;

namespace TwitterBot.Containers
{
	public class RedisQueue<T> : RedisList<T>
	{

		public RedisQueue (ConnectionMultiplexer redis, string key): base(redis, key)
		{
		}

		public void Push (T item)
		{
			this.Add (item);
		}

		public T Pop ()
		{
			var value = GetRedisDb ().ListLeftPop (key);
			if (value.HasValue)
				return ContainerUtils<T>.Deserialise (value.ToString ());
			else
				return default(T);
		}
	}
}

