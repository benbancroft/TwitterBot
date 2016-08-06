using System;
using Newtonsoft.Json;

namespace TwitterAPI.Dtos
{
	[JsonObject]
	public class UserResponse
	{
		//public UserType Type { get; set; }
		public ulong Id { get; set; }
		public string ScreenName { get; set; }
		public int Page { get; set; }
		public int Count { get; set; }
		public long Cursor { get; set; }
		public string Slug { get; set; }
		public string Query { get; set; }
		public bool IncludeEntities { get; set; }
		public bool SkipStatus { get; set; }
		public string UserIDResponse { get; set; }
		public string ScreenNameResponse { get; set; }
		//public ProfileImageSize ImageSize { get; set; }
		//public Cursors CursorMovement { get; internal set; }
		public string Name { get; set; }
		public string Location { get; set; }
		public string Description { get; set; }
		public string ProfileImageUrl { get; set; }
		public string ProfileImageUrlHttps { get; set; }
		public bool DefaultProfileImage{ get; set; }
		public string Url { get; set; }
		public bool DefaultProfile { get; set; }
		public bool Protected { get; set; }
		public int FollowersCount { get; set; }
		public string ProfileBackgroundColor { get; set; }
		public string ProfileTextColor { get; set; }
		public string ProfileLinkColor { get; set; }
		public string ProfileSidebarFillColor { get; set; }
		public string ProfileSidebarBorderColor { get; set; }
		public int FriendsCount { get; set; }
		public DateTime CreatedAt { get; set; }
		public int FavoritesCount { get; set; }
		public int? UtcOffset { get; set; }
		public string TimeZone { get; set; }
		public string ProfileBackgroundImageUrl { get; set; }
		public string ProfileBackgroundImageUrlHttps { get; set; }
		public bool ProfileBackgroundTile { get; set; }
		public bool ProfileUseBackgroundImage { get; set; }
		public int StatusesCount { get; set; }
		public bool Notifications { get; set; }
		public bool GeoEnabled { get; set; }
		public bool Verified { get; set; }
		public bool ContributorsEnabled { get; set; }
		public bool IsTranslator { get; set; }
		public bool Following { get; set; }
		public StatusResponse Status { get; set; }
		//public List<Category> Categories { get; set; }
		public string Lang { get; set; }
		public string LangResponse { get; set; }
		public bool ShowAllInlineMedia { get; set; }
		public int ListedCount { get; set; }
		public bool FollowRequestSent { get; set; }
		public string ProfileImage { get; set; }
		public string ProfileBannerUrl { get; set; }
		//public List<BannerSize> BannerSizes { get; set; }
		public string Email { get; set; }

	}
}

