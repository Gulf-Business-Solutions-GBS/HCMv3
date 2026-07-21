using Abp.Mapperly;
using HCMv3.Chat;
using HCMv3.Chat.Dto;
using HCMv3.Friendships;
using HCMv3.Friendships.Cache;
using HCMv3.Friendships.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class FriendshipToFriendDtoMapper : MapperBase<Friendship, FriendDto>
{
    public override partial FriendDto Map(Friendship source);
    public override partial void Map(Friendship source, FriendDto destination);
}

[Mapper]
public partial class FriendCacheItemToFriendDtoMapper : MapperBase<FriendCacheItem, FriendDto>
{
    public override partial FriendDto Map(FriendCacheItem source);
    public override partial void Map(FriendCacheItem source, FriendDto destination);
}

[Mapper]
public partial class ChatMessageToChatMessageDtoMapper : MapperBase<ChatMessage, ChatMessageDto>
{
    public override partial ChatMessageDto Map(ChatMessage source);
    public override partial void Map(ChatMessage source, ChatMessageDto destination);
}

[Mapper]
public partial class ChatMessageToChatMessageExportDtoMapper : MapperBase<ChatMessage, ChatMessageExportDto>
{
    [MapperIgnoreTarget(nameof(ChatMessageExportDto.TargetTenantName))]
    [MapperIgnoreTarget(nameof(ChatMessageExportDto.TargetUserName))]
    public override partial ChatMessageExportDto Map(ChatMessage source);

    [MapperIgnoreTarget(nameof(ChatMessageExportDto.TargetTenantName))]
    [MapperIgnoreTarget(nameof(ChatMessageExportDto.TargetUserName))]
    public override partial void Map(ChatMessage source, ChatMessageExportDto destination);
}
