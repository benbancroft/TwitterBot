using System;
using System.Collections.Generic;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace TwitterBot.Containers
{
	public class RedisSet<T> : IEnumerable<T>
	{
		private ConnectionMultiplexer redis;
		protected string key;

		public RedisSet (ConnectionMultiplexer redis, string key)
		{
			this.key = key;
			this.redis = redis;
		}

		protected IDatabase GetRedisDb ()
		{
			return redis.GetDatabase ();
		}

		public void Add (T item)
		{
			GetRedisDb().SetAdd (key, ContainerUtils<T>.Serialise (item));
		}

		public void Clear ()
		{
			GetRedisDb ().KeyDelete (key);
		}

		public bool Contains (T item)
		{
			return GetRedisDb ().SetContains (key, ContainerUtils<T>.Serialise (item));
		}

		public int Count {
			get { return (int)GetRedisDb ().SetLength (key); }
		}

		public bool Remove (T item)
		{
			return GetRedisDb ().SetRemove (key, ContainerUtils<T>.Serialise (item));
		}

		public IEnumerator<T> GetEnumerator ()
		{
			for (int i = 0; i < this.Count; i++) {
				yield return ContainerUtils<T>.Deserialise (GetRedisDb ().ListGetByIndex (key, i).ToString ());
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			for (int i = 0; i < this.Count; i++) {
				yield return ContainerUtils<T>.Deserialise (GetRedisDb ().ListGetByIndex (key, i).ToString ());
			}
		}
	}
}

