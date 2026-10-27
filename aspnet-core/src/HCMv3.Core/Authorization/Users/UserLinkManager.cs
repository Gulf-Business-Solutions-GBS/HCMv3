using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Abp;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Caching;
using Abp.Runtime.Security;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.Authorization.Impersonation;
using Abp.Timing;

namespace HCMv3.Authorization.Users;

public class UserLinkManager : HCMv3DomainServiceBase, IUserLinkManager
{
    private readonly IRepository<UserAccount, long> _userAccountRepository;
    private readonly ICacheManager _cacheManager;
    private readonly UserManager _userManager;
    private readonly UserClaimsPrincipalFactory _principalFactory;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<UserAccountLink, long> _userAccountLinkRepository;

    public IAbpSession AbpSession { get; set; }

    public UserLinkManager(
        IRepository<UserAccount, long> userAccountRepository,
        ICacheManager cacheManager,
        UserManager userManager,
        UserClaimsPrincipalFactory principalFactory,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<UserAccountLink, long> userAccountLinkRepository)
    {
        _userAccountRepository = userAccountRepository;
        _cacheManager = cacheManager;
        _userManager = userManager;
        _principalFactory = principalFactory;
        _unitOfWorkManager = unitOfWorkManager;
        _userAccountLinkRepository = userAccountLinkRepository;

        AbpSession = NullAbpSession.Instance;
    }

    public virtual async Task Link(User firstUser, User secondUser)
    {
        await _unitOfWorkManager.WithUnitOfWorkAsync(async () =>
        {
            var firstUserAccount = await GetUserAccountAsync(firstUser.ToUserIdentifier());
            var secondUserAccount = await GetUserAccountAsync(secondUser.ToUserIdentifier());

            await EnsureExplicitLinkExists(firstUserAccount.Id, secondUserAccount.Id);
        });
    }

    public virtual async Task<bool> AreUsersLinked(UserIdentifier firstUserIdentifier, UserIdentifier secondUserIdentifier)
    {
        return await _unitOfWorkManager.WithUnitOfWorkAsync(async () =>
        {
            var firstUserAccount = await GetUserAccountAsync(firstUserIdentifier);
            var secondUserAccount = await GetUserAccountAsync(secondUserIdentifier);

            var userLink = await _userAccountLinkRepository.FirstOrDefaultAsync(e =>
                e.UserAccountId == firstUserAccount.Id && e.LinkedUserAccountId == secondUserAccount.Id
            );

            return userLink != null;
        });
    }

    public virtual async Task Unlink(UserIdentifier firstUserIdentifier, UserIdentifier secondUserIdentifier)
    {
        await _unitOfWorkManager.WithUnitOfWorkAsync(async () =>
        {
            var firstUserAccount = await GetUserAccountAsync(firstUserIdentifier);
            var secondUserAccount = await GetUserAccountAsync(secondUserIdentifier);

            await _userAccountLinkRepository.DeleteAsync(e =>
                e.UserAccountId == firstUserAccount.Id && e.LinkedUserAccountId == secondUserAccount.Id
            );

            await CurrentUnitOfWork.SaveChangesAsync();
        });
    }

    private async Task EnsureExplicitLinkExists(long sourceUserAccountId, long targetUserAccountId)
    {
        var existing = await _userAccountLinkRepository.FirstOrDefaultAsync(l =>
            l.UserAccountId == sourceUserAccountId && l.LinkedUserAccountId == targetUserAccountId
        );

        if (existing != null)
        {
            return;
        }

        await _userAccountLinkRepository.InsertAsync(new UserAccountLink
        {
            UserAccountId = sourceUserAccountId,
            LinkedUserAccountId = targetUserAccountId,
            CreationTime = Clock.Now
        });
    }

    public virtual async Task<UserAccount> GetUserAccountAsync(UserIdentifier userIdentifier)
    {
        return await _unitOfWorkManager.WithUnitOfWorkAsync(async () =>
        {
            return await _userAccountRepository.FirstOrDefaultAsync(
                ua => ua.TenantId == userIdentifier.TenantId && ua.UserId == userIdentifier.UserId
            );
        });
    }

    public async Task<string> GetAccountSwitchToken(long targetUserId, int? targetTenantId)
    {
        // Create a cache item
        var cacheItem = new SwitchToLinkedAccountCacheItem(
            targetTenantId,
            targetUserId,
            AbpSession.ImpersonatorTenantId,
            AbpSession.ImpersonatorUserId
        );

        // Create a random token and save to the cache
        var token = Guid.NewGuid().ToString();

        await _cacheManager
            .GetSwitchToLinkedAccountCache()
            .SetAsync(token, cacheItem);

        return token;
    }

    public async Task<UserAndIdentity> GetSwitchedUserAndIdentity(string switchAccountToken)
    {
        var cacheItem = await _cacheManager.GetSwitchToLinkedAccountCache().GetOrDefaultAsync(switchAccountToken);
        if (cacheItem == null)
        {
            throw new UserFriendlyException(L("SwitchToLinkedAccountTokenErrorMessage"));
        }

        //Get the user from tenant
        var user = await _userManager.FindByIdAsync(cacheItem.TargetUserId.ToString());

        //Create identity
        var identity = (ClaimsIdentity)(await _principalFactory.CreateAsync(user)).Identity;

        //Add claims for audit logging
        if (cacheItem.ImpersonatorTenantId.HasValue)
        {
            identity.AddClaim(new Claim(AbpClaimTypes.ImpersonatorTenantId, cacheItem.ImpersonatorTenantId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        if (cacheItem.ImpersonatorUserId.HasValue)
        {
            identity.AddClaim(new Claim(AbpClaimTypes.ImpersonatorUserId, cacheItem.ImpersonatorUserId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        //Remove the cache item to prevent re-use
        await _cacheManager.GetSwitchToLinkedAccountCache().RemoveAsync(switchAccountToken);

        return new UserAndIdentity(user, identity);
    }
}

