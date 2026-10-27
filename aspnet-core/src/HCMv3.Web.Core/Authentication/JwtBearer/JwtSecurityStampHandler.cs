using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Runtime.Caching;
using Microsoft.AspNetCore.Http;
using HCMv3.Authorization.Session;
using HCMv3.Authorization.Users;
using HCMv3.Identity;

namespace HCMv3.Web.Authentication.JwtBearer;

public class JwtSecurityStampHandler : IJwtSecurityStampHandler, ITransientDependency
{
    private readonly ICacheManager _cacheManager;
    private readonly SignInManager _signInManager;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly UserManager _userManager;
    private readonly IUserSessionManager _userSessionManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtSecurityStampHandler(
        ICacheManager cacheManager,
        SignInManager signInManager,
        IUnitOfWorkManager unitOfWorkManager,
        UserManager userManager,
        IUserSessionManager userSessionManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _cacheManager = cacheManager;
        _signInManager = signInManager;
        _unitOfWorkManager = unitOfWorkManager;
        _userManager = userManager;
        _userSessionManager = userSessionManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Validate(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal?.Claims == null || !claimsPrincipal.Claims.Any())
        {
            return false;
        }

        var securityStampKey = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == AppConsts.SecurityStampKey);
        if (securityStampKey == null)
        {
            return false;
        }

        var userIdentifierString = claimsPrincipal.Claims.First(c => c.Type == AppConsts.UserIdentifier);
        var userIdentifier = UserIdentifier.Parse(userIdentifierString.Value);

        var isValid = await ValidateSecurityStampFromCache(userIdentifier, securityStampKey.Value);
        if (!isValid)
        {
            isValid = await ValidateSecurityStampFromDb(userIdentifier, securityStampKey.Value);
        }

        if (!isValid)
        {
            return false;
        }

        // Validate user session if session management is enabled
        if (await _userSessionManager.IsSessionManagementEnabledAsync())
        {
            var sessionTokenClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == AppConsts.SessionTokenKey);
            if (sessionTokenClaim != null)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var currentIpAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
                var currentUserAgent = httpContext?.Request.Headers["User-Agent"].ToString();

                var isSessionValid = await _userSessionManager.ValidateSessionAsync(
                    sessionTokenClaim.Value,
                    currentIpAddress,
                    currentUserAgent);

                if (!isSessionValid)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public async Task SetSecurityStampCacheItem(int? tenantId, long userId, string securityStamp)
    {
        await _cacheManager.GetCache(AppConsts.SecurityStampKey)
            .SetAsync(GenerateCacheKey(tenantId, userId), securityStamp);
    }

    public async Task RemoveSecurityStampCacheItem(int? tenantId, long userId)
    {
        await _cacheManager.GetCache(AppConsts.SecurityStampKey).RemoveAsync(GenerateCacheKey(tenantId, userId));
    }

    private string GenerateCacheKey(int? tenantId, long userId) => $"{tenantId}.{userId}";

    private async Task<bool> ValidateSecurityStampFromCache(UserIdentifier userIdentifier, string securityStamp)
    {
        var securityStampKey = await _cacheManager
            .GetCache(AppConsts.SecurityStampKey)
            .GetOrDefaultAsync(GenerateCacheKey(userIdentifier.TenantId, userIdentifier.UserId));

        return securityStampKey != null && (string)securityStampKey == securityStamp;
    }

    private async Task<bool> ValidateSecurityStampFromDb(UserIdentifier userIdentifier, string securityStamp)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            using (_unitOfWorkManager.Current.SetTenantId(userIdentifier.TenantId))
            {
                var user = await _userManager.GetUserAsync(userIdentifier);
                await uow.CompleteAsync();

                //cache last requested value
                await SetSecurityStampCacheItem(userIdentifier.TenantId, userIdentifier.UserId, user.SecurityStamp);

                return await _signInManager.ValidateSecurityStampAsync(user, securityStamp);
            }
        }
    }
}

