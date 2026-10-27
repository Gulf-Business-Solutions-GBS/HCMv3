using System.Collections.Generic;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using HCMv3.Chat;
using System.Linq;
using System.Threading.Tasks;

namespace HCMv3.Authorization.Users.DataCleaners;

public class ChatMessageUserDataCleaner : IUserDataCleaner, ITransientDependency
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<ChatMessage, long> _chatMessageRepository;

    public ChatMessageUserDataCleaner(
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<ChatMessage, long> chatMessageRepository)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _chatMessageRepository = chatMessageRepository;
    }

    public async Task CleanUserData(UserIdentifier userIdentifier)
    {
        List<ChatMessage> chatMessages = new List<ChatMessage>();
        await _unitOfWorkManager.WithUnitOfWorkAsync(async () =>
        {
            // Delete all messages of the user
            using (_unitOfWorkManager.Current.SetTenantId(userIdentifier.TenantId))
            {
                var chatMessagesQuery = await _chatMessageRepository
                    .GetAllAsync();
                chatMessages = await chatMessagesQuery
                    .Where(f => f.UserId == userIdentifier.UserId)
                    .ToListAsync();

                await DeleteChatMessages(chatMessages);
            }

            if (!chatMessages.Any())
            {
                return;
            }

            // Delete all reverse friendships of a friendship
            foreach (var chatMessage in chatMessages)
            {
                using (_unitOfWorkManager.Current.SetTenantId(chatMessage.TargetTenantId))
                {
                    var targetChatMessagesQuery = await _chatMessageRepository.GetAllAsync();
                    var targetChatMessages = await targetChatMessagesQuery
                        .Where(f =>
                            f.UserId == chatMessage.TargetUserId &&
                            f.TargetTenantId == userIdentifier.TenantId &&
                            f.TargetUserId == userIdentifier.UserId
                        )
                        .ToListAsync();

                    await DeleteChatMessages(targetChatMessages);
                }
            }
        });
    }

    private async Task DeleteChatMessages(List<ChatMessage> chatMessages)
    {
        foreach (var chatMessage in chatMessages)
        {
            await _chatMessageRepository.DeleteAsync(chatMessage);
        }
    }
}

