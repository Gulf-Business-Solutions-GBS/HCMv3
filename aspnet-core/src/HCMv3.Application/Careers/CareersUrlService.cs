using Abp.Dependency;
using Abp.Extensions;
using HCMv3.Configuration;

namespace HCMv3.Careers;

/// <summary>
/// Builds candidate email links from the SAME configuration key the tenant resolver reads
/// (<c>App:CareersDomainFormat</c>), so a link can only ever be produced for a host that the resolver would
/// also accept. There is deliberately no separate "careers base url" setting to drift out of sync.
///
/// Fails closed exactly like <see cref="CareersTenantResolver"/>: with the key unset (its state today --
/// this wave does not touch appsettings.json) every method returns <c>null</c>, no candidate email carries a
/// link, and nothing is mailed to a wrong or attacker-chosen host.
/// </summary>
public class CareersUrlService : ICareersUrlService, ITransientDependency
{
    /// <summary>Optional. Defaults to https; only exists so local http testing is possible.</summary>
    public const string SchemeConfigurationKey = "App:CareersUrlScheme";

    public const string EmailActivationRoute = "account/confirm-email";

    public const string PasswordResetRoute = "account/reset-password";

    private readonly IAppConfigurationAccessor _appConfigurationAccessor;

    public CareersUrlService(IAppConfigurationAccessor appConfigurationAccessor)
    {
        _appConfigurationAccessor = appConfigurationAccessor;
    }

    public string CreateEmailActivationUrlFormatOrNull(string tenancyName)
    {
        var root = GetSiteRootOrNull(tenancyName);
        if (root == null)
        {
            return null;
        }

        return root + EmailActivationRoute +
               "?userId={userId}&confirmationCode={confirmationCode}&tenantId={tenantId}";
    }

    public string CreatePasswordResetUrlFormatOrNull(string tenancyName)
    {
        var root = GetSiteRootOrNull(tenancyName);
        if (root == null)
        {
            return null;
        }

        return root + PasswordResetRoute +
               "?userId={userId}&resetCode={resetCode}&expireDate={expireDate}&tenantId={tenantId}";
    }

    private string GetSiteRootOrNull(string tenancyName)
    {
        if (tenancyName.IsNullOrWhiteSpace())
        {
            return null;
        }

        var domainFormatSetting =
            _appConfigurationAccessor.Configuration[CareersTenantResolver.DomainFormatConfigurationKey];
        if (domainFormatSetting.IsNullOrWhiteSpace())
        {
            return null;
        }

        // The setting may list several formats separated by ';' (same as the resolver). Links can only be
        // built from one, so take the first that actually carries the tenancy-name placeholder -- a format
        // without it would produce an apex-domain link shared by every tenant.
        foreach (var rawFormat in domainFormatSetting.Split(';'))
        {
            var domainFormat = rawFormat.Trim();
            if (!domainFormat.Contains(CareersTenantResolver.TenancyNamePlaceHolder))
            {
                continue;
            }

            var scheme = _appConfigurationAccessor.Configuration[SchemeConfigurationKey];
            if (scheme.IsNullOrWhiteSpace())
            {
                scheme = "https";
            }

            var host = domainFormat
                .RemovePreFix("http://", "https://")
                .RemovePostFix("/")
                .Replace(CareersTenantResolver.TenancyNamePlaceHolder, tenancyName);

            return scheme + "://" + host + "/";
        }

        return null;
    }
}
