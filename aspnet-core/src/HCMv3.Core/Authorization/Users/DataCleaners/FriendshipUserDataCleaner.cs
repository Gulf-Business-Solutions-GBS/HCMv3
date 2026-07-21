using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.RealTime;
using Microsoft.EntityFrameworkCore;
using HCMv3.Chat;
using HCMv3.Friendships;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HCMv3.Authorization.Users.DataCleaners;

public class FriendshipUserDataCleaner : IUserDataCleaner, ITransientDependency
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<Friendship, long> _friendshipRepository;
    private readonly IOnlineClientManager _onlineClientManager;
    private readonly IChatCommunicator _chatCommunicator;

    public FriendshipUserDataCleaner(
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<Friendship, long> friendshipRepository,
        IOnlineClientManager onlineClientManager,
        IChatCommunicator chatCommunicator)
    {
        _unitOfWorkManager = unitOfWorkManager;
        _friendshipRepository = friendshipRepository;
        _onlineClientManager = onlineClientManager;
        _chatCommunicator = chatCommunicator;
    }

    public async Task CleanUserData(UserIdentifier userIdentifier)
    {
        await _unitOfWorkManager.WithUnitOfWorkAsync(async () =>
        {
            // Delete all friendships of the user
            List<Friendship> userFriendList;
            using (_unitOfWorkManager.Current.SetTenantId(userIdentifier.TenantId))
            {
                var friendshipQuery = await _friendshipRepository.GetAllAsync();
                userFriendList = await friendshipQuery
                    .Where(f => f.UserId == userIdentifier.UserId)
                    .ToListAsync();

                await DeleteFriendships(userFriendList);
            }

            // Delete all reverse friendships of a friendship
            if (userFriendList.Any())
            {
                foreach (var userFriend in userFriendList)
                {
                    using (_unitOfWorkManager.Current.SetTenantId(userFriend.FriendTenantId))
                    {
                        var targetFriendshipQuery = await _friendshipRepository.GetAllAsync();
                        var targetFriendships = targetFriendshipQuery
                            .Where(f => f.UserId == userFriend.FriendUserId &&
                                        f.FriendUserId == userIdentifier.UserId &&
                                        f.FriendTenantId == userIdentifier.TenantId
                            )
                            .ToList();

                        await DeleteFriendships(targetFriendships);
                    }
                }
            }

            // inform the friend user clients about the user deletion
            await SendUserDeletedMessageToOnlineClients(userIdentifier, userFriendList);
        });
    }

    private async Task DeleteFriendships(List<Friendship> userFriendList)
    {
        foreach (var friendship in userFriendList)
        {
            await _friendshipRepository.DeleteAsync(friendship);
        }
    }

    private async Task SendUserDeletedMessageToOnlineClients(
        UserIdentifier userIdentifier,
        List<Friendship> userFriendList)
    {
        var friendUserClients = new List<IOnlineClient>();

        foreach (var friendship in userFriendList)
        {
            var friendClients = await _onlineClientManager.GetAllByUserIdAsync(
                new UserIdentifier(friendship.FriendTenantId, friendship.FriendUserId)
            );

            friendUserClients.AddRange(friendClients);
        }

        await _chatCommunicator.SendUserDeletedToClients(friendUserClients, userIdentifier);
    }
}

