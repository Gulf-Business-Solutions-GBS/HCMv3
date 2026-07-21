using Abp.Dependency;
using Abp.Extensions;
using Abp.MultiTenancy;
using HCMv3.Url;

namespace HCMv3.Web.Url;

public abstract class AppUrlServiceBase : IAppUrlService, ITransientDependency
{
    public abstract string EmailActivationRoute { get; }

    public abstract string EmailChangeRequestRoute { get; }

    public abstract string PasswordResetRoute { get; }

    protected readonly IWebUrlService WebUrlService;
    protected readonly ITenantCache TenantCache;

    protected AppUrlServiceBase(IWebUrlService webUrlService, ITenantCache tenantCache)
    {
        WebUrlService = webUrlService;
        TenantCache = tenantCache;
    }

    public string CreateEmailActivationUrlFormat(int? tenantId)
    {
        return CreateEmailActivationUrlFormat(GetTenancyName(tenantId));
    }

    public string CreateEmailActivationUrlFormat(string tenancyName)
    {
        var activationLink = GetSiteRootAddressWithoutQueryParams(tenancyName).EnsureEndsWith('/') +
                             EmailActivationRoute + "?userId={userId}&confirmationCode={confirmationCode}";

        if (tenancyName != null)
        {
            activationLink = activationLink + "&tenantId={tenantId}";
        }

        return activationLink;
    }

    public string CreateEmailChangeRequestUrlFormat(int? tenantId)
    {
        return CreateEmailChangeRequestUrlFormat(GetTenancyName(tenantId));
    }

    public string CreateEmailChangeRequestUrlFormat(string tenancyName)
    {
        var activationLink = GetSiteRootAddressWithoutQueryParams(tenancyName).EnsureEndsWith('/') +
                             EmailChangeRequestRoute + "?userId={userId}&emailAddress={emailAddress}&old={oldMailAddress}";

        if (tenancyName != null)
        {
            activationLink = activationLink + "&tenantId={tenantId}";
        }

        return activationLink;
    }

    public string CreatePasswordResetUrlFormat(int? tenantId)
    {
        return CreatePasswordResetUrlFormat(GetTenancyName(tenantId));
    }

    public string CreatePasswordResetUrlFormat(string tenancyName)
    {
        var resetLink = GetSiteRootAddressWithoutQueryParams(tenancyName).EnsureEndsWith('/') + PasswordResetRoute +
                        $"?userId={{userId}}&resetCode={{resetCode}}&expireDate={{expireDate}}";

        if (tenancyName != null)
        {
            resetLink += "&tenantId={tenantId}";
        }

        return resetLink;
    }

    private string GetSiteRootAddressWithoutQueryParams(string tenancyName)
    {
        return WebUrlService.GetSiteRootAddress(tenancyName).Split('?')[0];
    }

    private string GetTenancyName(int? tenantId)
    {
        return tenantId.HasValue ? TenantCache.Get(tenantId.Value).TenancyName : null;
    }
}

