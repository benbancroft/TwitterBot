using System;
using System.Collections.Generic;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Linq;

namespace TwitterBot.Containers
{
	public class RedisSortedSet<T> : IEnumerable<T>
	{
		private ConnectionMultiplexer redis;
		protected string key;

		public RedisSortedSet (ConnectionMultiplexer redis, string key)
		{
			this.key = key;
			this.redis = redis;
		}

		protected IDatabase GetRedisDb ()
		{
			return redis.GetDatabase ();
		}

		public T GetLeast ()
		{
			return 
				ContainerUtils<T>.Deserialise (GetRedisDb ()
				.SortedSetRangeByScore (key, double.NegativeInfinity, double.PositiveInfinity, Exclude.None, Order.Ascending, 0, 1)
					.FirstOrDefault ());
		}

		public T GetMost ()
		{
			return 
				ContainerUtils<T>.Deserialise (GetRedisDb ()
					.SortedSetRangeByScore (key, double.PositiveInfinity, double.NegativeInfinity, Exclude.None, Order.Ascending, 0, 1)
					.FirstOrDefault ());
		}

		public void Add (T item, int score)
		{
			GetRedisDb ().SortedSetAdd (key, ContainerUtils<T>.Serialise (item), score);
		}

		public void Clear ()
		{
			GetRedisDb ().KeyDelete (key);
		}

		public bool Contains (T item)
		{
			return GetRedisDb ().SortedSetRank (key, ContainerUtils<T>.Serialise (item)).HasValue;
		}

		public int Count {
			get { return (int)GetRedisDb ().SortedSetLength (key); }
		}

		public bool Remove (T item)
		{
			return GetRedisDb ().SortedSetRemove (key, ContainerUtils<T>.Serialise (item));
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

