using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Runtime.Caching;
using Abp.UI;
using Microsoft.AspNetCore.Mvc.Rendering;
using HCMv3.Authentication.PasswordlessLogin;
using HCMv3.Authorization.Users;

namespace HCMv3.Authorization.PasswordlessLogin;

public class PasswordlessLoginManager : HCMv3DomainServiceBase, IPasswordlessLoginManager
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager _userManager;
    private readonly ICacheManager _cacheManager;

    public PasswordlessLoginManager(
        IUserRepository userRepository,
        UserManager userManager,
        ICacheManager cacheManager)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _cacheManager = cacheManager;
    }

    public async Task<User> GetUserByPasswordlessProviderAndKeyAsync(string provider, string providerKey)
    {
        if (provider == PasswordlessLoginProviderType.Email.ToString())
        {
            return await _userManager.FindByEmailAsync(providerKey);
        }

        if (provider == PasswordlessLoginProviderType.Sms.ToString())
        {
            return await _userRepository.FindByPhoneNumberAsync(providerKey);
        }

        throw new AbpException(L("UserNotFound"));
    }

    public async Task VerifyPasswordlessLoginCode(int? tenantId, string providerValue, string code)
    {
        var cacheKey = GetPasswordlessLoginCodeCacheKey(tenantId, providerValue);
        var cache = await _cacheManager.GetPasswordlessVerificationCodeCache().GetOrDefaultAsync(cacheKey);

        if (cache == null)
        {
            throw new Exception(L("PasswordlessCodeNotFoundCache"));
        }

        if (code != cache.Code)
        {
            throw new UserFriendlyException(L("WrongPasswordlessVerificationCode"));
        }
    }

    public async Task<string> GeneratePasswordlessLoginCode(int? tenantId, string providerKey)
    {
        var code = RandomHelper.GetRandom(100000, 999999).ToString();
        var cacheItem = new PasswordlessLoginCodeCacheItem
        {
            Code = code
        };

        // Remove old code, so only 1 code will be valid at a time
        await _cacheManager.GetPasswordlessVerificationCodeCache()
            .RemoveAsync(
                GetPasswordlessLoginCodeCacheKey(tenantId, providerKey)
            );

        await _cacheManager.GetPasswordlessVerificationCodeCache().SetAsync(
            GetPasswordlessLoginCodeCacheKey(tenantId, providerKey),
            cacheItem
        );

        return code;
    }

    public async Task RemovePasswordlessLoginCode(int? tenantId, string providerKey)
    {
        await _cacheManager.GetPasswordlessVerificationCodeCache().RemoveAsync(
            GetPasswordlessLoginCodeCacheKey(tenantId, providerKey)
        );
    }

    public List<SelectListItem> GetProviders()
    {
        return Enum.GetValues(typeof(PasswordlessLoginProviderType))
            .Cast<PasswordlessLoginProviderType>()
            .Select(providerType => new SelectListItem
            {
                Text = Enum.GetName(typeof(PasswordlessLoginProviderType), providerType),
                Value = providerType.ToString("G")
            })
            .ToList();
    }

    private string GetPasswordlessLoginCodeCacheKey(int? tenantId, string providerKey)
    {
        if (tenantId.HasValue)
        {
            return tenantId.Value + "|" + providerKey;
        }

        return providerKey;
    }
}

