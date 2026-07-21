using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using HCMv3.Authorization.Users.Dto;
using HCMv3.MultiTenancy;

namespace HCMv3.Authorization.Users;

[AbpAuthorize]
public class UserLinkAppService : HCMv3AppServiceBase, IUserLinkAppService
{
    private readonly AbpLoginResultTypeHelper _abpLoginResultTypeHelper;
    private readonly IUserLinkManager _userLinkManager;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IRepository<UserAccount, long> _userAccountRepository;
    private readonly IRepository<UserAccountLink, long> _userAccountLinkRepository;
    private readonly LogInManager _logInManager;

    public UserLinkAppService(
        AbpLoginResultTypeHelper abpLoginResultTypeHelper,
        IUserLinkManager userLinkManager,
        IRepository<Tenant> tenantRepository,
        IRepository<UserAccount, long> userAccountRepository,
        IRepository<UserAccountLink, long> userAccountLinkRepository,
        LogInManager logInManager)
    {
        _abpLoginResultTypeHelper = abpLoginResultTypeHelper;
        _userLinkManager = userLinkManager;
        _tenantRepository = tenantRepository;
        _userAccountRepository = userAccountRepository;
        _userAccountLinkRepository = userAccountLinkRepository;
        _logInManager = logInManager;
    }

    public async Task LinkToUser(LinkToUserInput input)
    {
        var loginResult = await _logInManager.LoginAsync(input.UsernameOrEmailAddress, input.Password, input.TenancyName);

        if (loginResult.Result != AbpLoginResultType.Success)
        {
            throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(loginResult.Result, input.UsernameOrEmailAddress, input.TenancyName);
        }

        if (AbpSession.IsUser(loginResult.User))
        {
            throw new UserFriendlyException(L("YouCannotLinkToSameAccount"));
        }

        if (loginResult.User.ShouldChangePasswordOnNextLogin)
        {
            throw new UserFriendlyException(L("ChangePasswordBeforeLinkToAnAccount"));
        }

        var currentUser = await GetCurrentUserAsync();
        await _userLinkManager.Link(currentUser, loginResult.User);
    }

    public async Task<PagedResultDto<LinkedUserDto>> GetLinkedUsers(GetLinkedUsersInput input)
    {
        var currentUserAccount = await _userLinkManager.GetUserAccountAsync(AbpSession.ToUserIdentifier());
        if (currentUserAccount == null)
        {
            return new PagedResultDto<LinkedUserDto>(0, new List<LinkedUserDto>());
        }

        var query = CreateLinkedUsersQuery(currentUserAccount, input.Sorting);

        var totalCount = await query.CountAsync();

        var linkedUsers = await query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToListAsync();

        return new PagedResultDto<LinkedUserDto>(
            totalCount,
            linkedUsers
        );
    }

    [DisableAuditing]
    public async Task<ListResultDto<LinkedUserDto>> GetRecentlyUsedLinkedUsers()
    {
        var currentUserAccount = await _userLinkManager.GetUserAccountAsync(AbpSession.ToUserIdentifier());
        if (currentUserAccount == null)
        {
            return new ListResultDto<LinkedUserDto>();
        }

        var query = CreateLinkedUsersQuery(currentUserAccount, "TenancyName, Username");
        var recentlyUsedlinkedUsers = await query.Take(3).ToListAsync();

        return new ListResultDto<LinkedUserDto>(recentlyUsedlinkedUsers);
    }

    public async Task UnlinkUser(UnlinkUserInput input)
    {
        if (!await _userLinkManager.AreUsersLinked(AbpSession.ToUserIdentifier(), input.ToUserIdentifier()))
        {
            return;
        }

        await _userLinkManager.Unlink(AbpSession.ToUserIdentifier(), input.ToUserIdentifier());
    }

    private IQueryable<LinkedUserDto> CreateLinkedUsersQuery(UserAccount currentUserAccount, string sorting)
    {
        var currentUserIdentifier = AbpSession.ToUserIdentifier();
        var explicitLinksQuery = from link in _userAccountLinkRepository.GetAll()
                                 join linkedAccount in _userAccountRepository.GetAll() on link.LinkedUserAccountId equals linkedAccount.Id
                                 join tenant in _tenantRepository.GetAll() on linkedAccount.TenantId equals tenant.Id into tenantJoined
                                 from tenant in tenantJoined.DefaultIfEmpty()
                                 where link.UserAccountId == currentUserAccount.Id &&
                                       (linkedAccount.TenantId != currentUserIdentifier.TenantId ||
                                        linkedAccount.UserId != currentUserIdentifier.UserId)
                                 select new LinkedUserDto
                                 {
                                     Id = linkedAccount.UserId,
                                     TenantId = linkedAccount.TenantId,
                                     TenancyName = tenant == null ? "." : tenant.TenancyName,
                                     Username = linkedAccount.UserName
                                 };

        return explicitLinksQuery.OrderBy(sorting);
    }
}
