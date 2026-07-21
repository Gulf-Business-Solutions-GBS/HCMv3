using Abp.Runtime.Caching;
using HCMv3.Authentication.TwoFactor;

namespace HCMv3.Web.Authentication.TwoFactor;

public static class TwoFactorCodeCacheExtensions
{
    public static ITypedCache<string, TwoFactorCodeCacheItem> GetTwoFactorCodeCache(this ICacheManager cacheManager)
    {
        return cacheManager.GetCache<string, TwoFactorCodeCacheItem>(TwoFactorCodeCacheItem.CacheName);
    }
}

