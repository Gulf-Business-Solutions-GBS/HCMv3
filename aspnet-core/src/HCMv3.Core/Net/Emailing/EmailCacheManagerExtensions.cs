using Abp.Runtime.Caching;

namespace HCMv3.Net.Emailing;

public static class EmailCacheManagerExtensions
{
    public static ITypedCache<string, EmailTemplateCacheItem> GetEmailTemplateCache(this ICacheManager cacheManager)
    {
        return cacheManager.GetCache<string, EmailTemplateCacheItem>(EmailTemplateCacheItem.CacheName);
    }
}