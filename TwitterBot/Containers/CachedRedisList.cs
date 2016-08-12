using System;
using StackExchange.Redis;

namespace TwitterBot.Containers
{
	public class CachedRedisList<T> : RedisList<T>
	{
		//same apart from send message on add/remove with client id (addr and port)
		//register recieve message
		//if is from self, ignore else reset lists from redis

		public CachedRedisList (ConnectionMultiplexer redis, string key): base(redis, key)
		{
		}
	}
}

