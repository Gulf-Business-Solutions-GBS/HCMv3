using System;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Authorization;
using Abp.Configuration;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Runtime.Security;
using Abp.UI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HCMv3.Authorization;
using HCMv3.Authorization.Delegation;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Session;
using HCMv3.Authorization.Users;
using HCMv3.Configuration;
using HCMv3.MultiTenancy;

namespace HCMv3.Identity;

public class SecurityStampValidator : AbpSecurityStampValidator<Tenant, Role, User>
{
    private readonly IUserDelegationManager _userDelegationManager;
    private readonly IUserDelegationConfiguration _userDelegationConfiguration;
    private readonly PermissionChecker _permissionChecker;
    private readonly IUserSessionManager _userSessionManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecurityStampValidator(
        IOptions<SecurityStampValidatorOptions> options,
        SignInManager signInManager,
        ILoggerFactory loggerFactory,
        IUserDelegationConfiguration userDelegationConfiguration,
        IUserDelegationManager userDelegationManager,
        PermissionChecker permissionChecker,
        IUnitOfWorkManager unitOfWorkManager,
        IUserSessionManager userSessionManager,
        IHttpContextAccessor httpContextAccessor)
        : base(options, signInManager, loggerFactory, unitOfWorkManager)
    {
        _userDelegationConfiguration = userDelegationConfiguration;
        _userDelegationManager = userDelegationManager;
        _permissionChecker = permissionChecker;
        _userSessionManager = userSessionManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        ValidateUserDelegation(context);

        await base.ValidateAsync(context);

        // If base validation already rejected, skip session check
        if (context.Principal == null)
        {
            return;
        }

        await ValidateUserSession(context);
    }

    private async Task ValidateUserSession(CookieValidatePrincipalContext context)
    {
        var isEnabled = await _userSessionManager.IsSessionManagementEnabledAsync();
        if (!isEnabled)
        {
            return;
        }

        var sessionTokenClaim = context.Principal?.Claims.FirstOrDefault(c => c.Type == HCMv3Consts.SessionTokenKey);
        if (sessionTokenClaim == null)
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var currentIpAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var currentUserAgent = httpContext?.Request.Headers["User-Agent"].ToString();

        var isValid = await _userSessionManager.ValidateSessionAsync(
            sessionTokenClaim.Value,
            currentIpAddress,
            currentUserAgent);

        if (!isValid)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    }

    private void ValidateUserDelegation(CookieValidatePrincipalContext context)
    {
        if (!_userDelegationConfiguration.IsEnabled)
        {
            return;
        }

        var impersonatorTenant = context.Principal.Claims.FirstOrDefault(c => c.Type == AbpClaimTypes.ImpersonatorTenantId);
        var user = context.Principal.Claims.FirstOrDefault(c => c.Type == AbpClaimTypes.UserId);
        var impersonatorUser = context.Principal.Claims.FirstOrDefault(c => c.Type == AbpClaimTypes.ImpersonatorUserId);

        if (impersonatorUser == null || user == null)
        {
            return;
        }

        var impersonatorTenantId = impersonatorTenant == null ? null : impersonatorTenant.Value.IsNullOrEmpty() ? (int?)null : Convert.ToInt32(impersonatorTenant.Value);
        var sourceUserId = Convert.ToInt64(user.Value);
        var targetUserId = Convert.ToInt64(impersonatorUser.Value);

        if (_permissionChecker.IsGranted(new UserIdentifier(impersonatorTenantId, targetUserId), AppPermissions.Pages_Administration_Users_Impersonation))
        {
            return;
        }

        var hasActiveDelegation = _userDelegationManager.HasActiveDelegation(sourceUserId, targetUserId);

        if (!hasActiveDelegation)
        {
            throw new UserFriendlyException("ThereIsNoActiveUserDelegationBetweenYourUserAndCurrentUser");
        }
    }
}

