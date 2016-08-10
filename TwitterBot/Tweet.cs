using System;
using System.Collections.Generic;

namespace TwitterBot
{
	public class Tweet : IComparable<Tweet>
	{
		public TweetPriority Priority { get; set; }

		public ulong Id { get; set; }

		public ulong UserId { get; set; }

		public String UserName { get; set; }

		public DateTime Tweeted { get; set; }

		public bool Follow { get; set; }

		public bool Favourite { get; set; }

		public String Text { get; set; }

		public Tweet ()
		{
		}

		public int CompareTo(Tweet obj)
		{
			return obj.Priority.CompareTo (this.Priority);
		}

		public override bool Equals(System.Object obj)
		{
			// If parameter is null return false.
			if (obj == null)
			{
				return false;
			}

			// If parameter cannot be cast to Point return false.
			Tweet p = obj as Tweet;
			if ((System.Object)p == null)
			{
				return false;
			}
				
			return this.Id == p.Id;
		}

		public override int GetHashCode(){
			return (int)this.Id;
		}
	}
}

