using Abp.Dependency;
using Abp.Extensions;
using Abp.IO.Extensions;
using Abp.MultiTenancy;
using Abp.Reflection.Extensions;
using HCMv3.Url;
using System;
using System.Collections.Concurrent;
using System.Text;
using Abp.Runtime.Caching;

namespace HCMv3.Net.Emailing;

public class EmailTemplateProvider : IEmailTemplateProvider, ISingletonDependency
{
    private readonly IWebUrlService _webUrlService;
    private readonly ITenantCache _tenantCache;
    private readonly ConcurrentDictionary<string, string> _defaultTemplates;

    private readonly ICacheManager _cacheManager;

    private const string GetLightTenantLogoUrl = "TenantCustomization/GetTenantLogo/light";

    public EmailTemplateProvider(IWebUrlService webUrlService
        , ITenantCache tenantCache
        , ICacheManager cacheManager)
    {
        _webUrlService = webUrlService;
        _tenantCache = tenantCache;
        _defaultTemplates = new ConcurrentDictionary<string, string>();
        _cacheManager = cacheManager;
    }

    public string GetDefaultTemplate(int? tenantId)
    {
        var tenancyKey = tenantId.HasValue ? tenantId.Value.ToString() : "host";

        var cacheItem = _cacheManager.GetEmailTemplateCache().GetOrDefault(tenancyKey);

        if (cacheItem != null)
        {
            return cacheItem.Template;
        }

        string template;

        using (var stream = typeof(EmailTemplateProvider).GetAssembly()
                   .GetManifestResourceStream(
                       "HCMv3.Net.Emailing.EmailTemplates.default.html"))
        {
            var bytes = stream.GetAllBytes();
            template = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }

        var replacedTemplate = ReplaceTemplateBaseVariables(template, tenantId);

        var emailCacheItem = new EmailTemplateCacheItem(replacedTemplate);

        _cacheManager.GetEmailTemplateCache().Set(tenancyKey, emailCacheItem);

        return replacedTemplate;
    }

    private string ReplaceTemplateBaseVariables(string template, int? tenantId)
    {
        template = template.Replace("{THIS_YEAR}", DateTime.Now.Year.ToString());
        template = template.Replace("{TWITTER_URL}", GetTwitterIconUrl());
        return template.Replace("{EMAIL_LOGO_URL}", GetTenantLogoUrl(tenantId));
    }

    private string GetTenantLogoUrl(int? tenantId)
    {
        if (!tenantId.HasValue)
        {
            return _webUrlService.GetServerRootAddress().EnsureEndsWith('/') + GetLightTenantLogoUrl +
                   "?tenantId=&extension=png";
        }

        var tenant = _tenantCache.Get(tenantId.Value);

        return _webUrlService.GetServerRootAddress(tenant.TenancyName).EnsureEndsWith('/')
               + GetLightTenantLogoUrl.EnsureEndsWith('/') + tenantId.Value + "/png";
    }

    private string GetTwitterIconUrl()
    {
        return _webUrlService.GetServerRootAddress().EnsureEndsWith('/') + "Common/Images/twitter.png";
    }
}