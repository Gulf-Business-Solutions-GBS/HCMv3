using Abp.Mapperly;
using HCMv3.Friendships;
using HCMv3.Friendships.Cache;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class FriendshipToFriendCacheItemMapper : MapperBase<Friendship, FriendCacheItem>
{
    public override partial FriendCacheItem Map(Friendship source);
    public override partial void Map(Friendship source, FriendCacheItem destination);
}
