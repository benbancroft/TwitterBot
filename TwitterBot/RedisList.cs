using System;
using System.Collections.Generic;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace TwitterBot
{
	public class RedisList<T> : IList<T>
	{
		private ConnectionMultiplexer redis;
		private string key;

		public RedisList (ConnectionMultiplexer redis, string key)
		{
			this.key = key;
			this.redis = redis;
		}

		private IDatabase GetRedisDb ()
		{
			return redis.GetDatabase ();
		}

		private string Serialise (object obj)
		{
			return JsonConvert.SerializeObject (obj);
		}

		private T Deserialise (string serialized)
		{
			return JsonConvert.DeserializeObject<T> (serialized);
		}

		public void Insert (int index, T item)
		{
			var db = GetRedisDb ();
			var before = db.ListGetByIndex (key, index);
			db.ListInsertBefore (key, before, Serialise (item));
		}

		public void RemoveAt (int index)
		{
			var db = GetRedisDb ();
			var value = db.ListGetByIndex (key, index);
			if (!value.IsNull) {
				db.ListRemove (key, value);
			}
		}

		public T this [int index] {
			get {
				var value = GetRedisDb ().ListGetByIndex (key, index);
				return Deserialise (value.ToString ());
			}
			set {
				Insert (index, value);
			}
		}

		public void Add (T item)
		{
			GetRedisDb ().ListRightPush (key, Serialise (item));
		}

		public void AddRange (IList<T> items)
		{
			foreach (var item in items)
				this.Add (item);
		}

		public void Clear ()
		{
			GetRedisDb ().KeyDelete (key);
		}

		public bool Contains (T item)
		{
			for (int i = 0; i < Count; i++) {
				if (GetRedisDb ().ListGetByIndex (key, i).ToString ().Equals (Serialise (item))) {
					return true;
				}
			}
			return false;
		}

		public void CopyTo (T[] array, int arrayIndex)
		{
			GetRedisDb ().ListRange (key).CopyTo (array, arrayIndex);
		}

		public int IndexOf (T item)
		{
			for (int i = 0; i < Count; i++) {
				if (GetRedisDb ().ListGetByIndex (key, i).ToString ().Equals (Serialise (item))) {
					return i;
				}
			}
			return -1;
		}

		public int Count {
			get { return (int)GetRedisDb ().ListLength (key); }
		}

		public bool IsReadOnly {
			get { return false; }
		}

		public bool Remove (T item)
		{
			return GetRedisDb ().ListRemove (key, Serialise (item)) > 0;
		}

		public IEnumerator<T> GetEnumerator ()
		{
			for (int i = 0; i < this.Count; i++) {
				yield return Deserialise (GetRedisDb ().ListGetByIndex (key, i).ToString ());
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			for (int i = 0; i < this.Count; i++) {
				yield return Deserialise (GetRedisDb ().ListGetByIndex (key, i).ToString ());
			}
		}
	}
}

