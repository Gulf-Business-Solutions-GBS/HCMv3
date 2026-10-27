using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Notifications;
using HCMv3.Authorization.Users;

namespace HCMv3.HcmNotifications;

/// <inheritdoc cref="IHcmNotificationSubscriptionGate"/>
public class HcmNotificationSubscriptionGate : HCMv3DomainServiceBase, IHcmNotificationSubscriptionGate
{
    private readonly INotificationSubscriptionManager _subscriptionManager;
    private readonly IRepository<User, long> _userRepository;

    public HcmNotificationSubscriptionGate(
        INotificationSubscriptionManager subscriptionManager,
        IRepository<User, long> userRepository)
    {
        _subscriptionManager = subscriptionManager;
        _userRepository = userRepository;
    }

    [UnitOfWork]
    public virtual async Task<List<UserIdentifier>> FilterSubscribedAsync(
        string notificationName,
        IEnumerable<UserIdentifier> users)
    {
        var subscribed = new List<UserIdentifier>();
        var candidates = (users ?? Enumerable.Empty<UserIdentifier>()).ToList();

        if (candidates.Count == 0)
        {
            return subscribed;
        }

        var tenantId = CurrentUnitOfWork.GetTenantId();
        if (tenantId.HasValue)
        {
            await EnsureDefaultSubscriptionsAsync(tenantId.Value);
        }

        foreach (var user in candidates)
        {
            if (await _subscriptionManager.IsSubscribedAsync(user, notificationName))
            {
                subscribed.Add(user);
            }
        }

        return subscribed;
    }

    [UnitOfWork]
    public virtual async Task EnsureDefaultSubscriptionsAsync(int tenantId)
    {
        var handedOut = Split(await SettingManager.GetSettingValueForTenantAsync(
            HcmNotificationSettingNames.SubscribedDefinitions,
            tenantId));

        var missing = HcmNotificationNames.All.Where(name => !handedOut.Contains(name)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        List<long> userIds;
        using (CurrentUnitOfWork.SetTenantId(tenantId))
        {
            userIds = _userRepository.GetAll()
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToList();
        }

        foreach (var userId in userIds)
        {
            var user = new UserIdentifier(tenantId, userId);

            foreach (var name in missing)
            {
                // ABP's SubscribeAsync is a no-op for an existing subscription, so a user Zero already
                // subscribed at creation is not given a second row.
                await _subscriptionManager.SubscribeAsync(user, name);
            }
        }

        handedOut.UnionWith(missing);

        await SettingManager.ChangeSettingForTenantAsync(
            tenantId,
            HcmNotificationSettingNames.SubscribedDefinitions,
            string.Join(",", handedOut.OrderBy(n => n, StringComparer.Ordinal)));
    }

    private static HashSet<string> Split(string value)
    {
        return new HashSet<string>(
            (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.Ordinal);
    }
}
