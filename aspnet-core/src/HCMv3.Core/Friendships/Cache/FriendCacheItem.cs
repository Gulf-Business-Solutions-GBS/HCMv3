using System;

namespace HCMv3.Friendships.Cache;

public class FriendCacheItem
{
    public const string CacheName = "AppUserFriendCache";

    public static readonly TimeSpan DefaultSlidingExpireTime = TimeSpan.FromMinutes(30);

    public long FriendUserId { get; set; }

    public int? FriendTenantId { get; set; }

    public string FriendUserName { get; set; }

    public string FriendTenancyName { get; set; }

    public Guid? FriendProfilePictureId { get; set; }

    public int UnreadMessageCount { get; set; }

    public FriendshipState State { get; set; }
}

