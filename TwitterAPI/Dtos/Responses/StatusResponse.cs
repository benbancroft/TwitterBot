using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwitterAPI.Dtos
{
	public class StatusResponse
	{
		public ulong Id { get; set; }
		public ulong UserID { get; set; }
		public ulong SinceID { get; set; }
		public ulong MaxID { get; set; }
		public int Count { get; set; }
		public long Cursor { get; set; }
		public bool IncludeRetweets { get; set; }
		public bool ExcludeReplies { get; set; }
		public bool IncludeEntities { get; set; }
		public bool IncludeUserEntities { get; set; }
		public bool IncludeMyRetweet { get; set; }
		public string OEmbedUrl { get; set; }
		public int OEmbedMaxWidth { get; set; }
		public bool OEmbedHideMedia { get; set; }
		public bool OEmbedHideThread { get; set; }
		public bool OEmbedOmitScript { get; set; }
		//public EmbeddedStatusAlignment OEmbedAlign { get; set; }
		public string OEmbedRelated { get; set; }
		public string OEmbedLanguage { get; set; }
		public DateTime CreatedAt { get; set; }
		public ulong StatusID { get; set; }
		public string Text { get; set; }
		public string Source { get; set; }
		public bool Truncated { get; set; }
		public ulong? InReplyToStatusID { get; set; }
		public ulong? InReplyToUserID { get; set; }
		public int? FavoriteCount { get; set; }
		public bool Favorited { get; set; }
		public string InReplyToScreenName { get; set; }
		public UserResponse User { get; set; }
		public List<ulong> Users { get; set; }
		/*public List<Contributor> Contributors { get; set; }
		public Coordinate Coordinates { get; set; }
		public Place Place { get; set; }
		public Annotation Annotation { get; set; }
		public Entities Entities { get; set; }
		public Entities ExtendedEntities { get; set; }*/
		public bool TrimUser { get; set; }
		public bool IncludeContributorDetails { get; set; }
		public int RetweetCount { get; set; }
		public bool Retweeted { get; set; }
		public bool PossiblySensitive { get; set; }
		public StatusResponse RetweetedStatus { get; set; }
		public ulong CurrentUserRetweet { get; set; }
		public ulong QuotedStatusID { get; set; }
		public StatusResponse QuotedStatus { get; set; }
		public Dictionary<string, string> Scopes { get; set; }
		public bool WithheldCopyright { get; set; }
		public List<string> WithheldInCountries { get; set; }
		public string WithheldScope { get; set; }
		//public StatusMetaData MetaData { get; set; }
		public string Lang { get; set; }
		public bool Map { get; set; }
		public string TweetIDs { get; set; }
		//public FilterLevel FilterLevel { get; set; }
		//public EmbeddedStatus EmbeddedStatus { get; set; }
		//public Cursors CursorMovement { get; set; }

	}
}

