using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.MultiTenancy;
using Abp.Text;
using Castle.Core.Logging;
using HCMv3.Configuration;
using HCMv3.MultiTenancy;

namespace HCMv3.Careers;

/// <summary>
/// Resolves the careers-portal tenant from the request subdomain. See <see cref="ICareersTenantResolver"/>
/// for why the public surface cannot use <c>AbpSession.TenantId</c>.
///
/// This follows the same extraction mechanics ASP.NET Zero already uses in
/// <c>HCMv3.Web.MultiTenancy.DomainTenantCheckMiddleware</c> (<c>Abp.Text.FormattedStringValueExtracter</c>
/// against a <c>{TENANCY_NAME}</c>-bearing domain format, <c>;</c>-separated for multiple formats, <c>www</c>
/// treated as "not a tenant"), but reads its OWN configuration key rather than ABP's global
/// <c>Configuration.Modules.AbpWebCommon().MultiTenancy.DomainFormat</c>. That is deliberate: the global
/// format is currently set to <c>App:ServerRootAddress</c> in <c>HCMv3WebHostModule.PreInitialize</c>
/// (<c>https://localhost:44301/</c> -- no <c>{TENANCY_NAME}</c> placeholder at all), and repointing it would
/// change tenant resolution for the entire authenticated admin API. The careers portal gets an independent,
/// additive key instead.
///
/// CONFIGURATION REQUIRED: <c>App:CareersDomainFormat</c>, e.g.
/// <c>{TENANCY_NAME}.careers.example.com</c> (multiple formats may be separated by <c>;</c>). This key is
/// intentionally NOT added to appsettings.json by this wave. Until it is configured this resolver returns
/// <c>null</c> for every request and the whole careers API returns "portal not available" -- fails closed,
/// never open.
/// </summary>
public class CareersTenantResolver : ICareersTenantResolver, ITransientDependency
{
    public const string DomainFormatConfigurationKey = "App:CareersDomainFormat";

    public const string TenancyNamePlaceHolder = "{TENANCY_NAME}";

    private readonly ICareersHostNameProvider _hostNameProvider;
    private readonly IAppConfigurationAccessor _appConfigurationAccessor;
    private readonly IRepository<Tenant> _tenantRepository;

    public ILogger Logger { get; set; }

    public CareersTenantResolver(
        ICareersHostNameProvider hostNameProvider,
        IAppConfigurationAccessor appConfigurationAccessor,
        IRepository<Tenant> tenantRepository)
    {
        _hostNameProvider = hostNameProvider;
        _appConfigurationAccessor = appConfigurationAccessor;
        _tenantRepository = tenantRepository;
        Logger = NullLogger.Instance;
    }

    public async Task<int?> ResolveTenantIdOrNullAsync()
    {
        var tenancyName = ExtractTenancyNameOrNull();
        if (tenancyName == null)
        {
            return null;
        }

        // Tenant is a host-side entity, but the ambient MayHaveTenant/MustHaveTenant filters are disabled
        // for it anyway; the lookup is by tenancy name and returns at most one row.
        var tenant = await _tenantRepository.FirstOrDefaultAsync(t => t.TenancyName == tenancyName);

        if (tenant == null)
        {
            Logger.Warn($"Careers portal: no tenant matches tenancy name '{tenancyName}'.");
            return null;
        }

        if (!tenant.IsActive)
        {
            Logger.Warn($"Careers portal: tenant '{tenancyName}' is deactivated.");
            return null;
        }

        return tenant.Id;
    }

    /// <summary>
    /// Pulls the <c>{TENANCY_NAME}</c> segment out of the current request's host name, or <c>null</c> if it
    /// cannot be determined. Exposed as <c>internal</c>-style protected virtual for focused unit tests.
    /// </summary>
    protected internal virtual string ExtractTenancyNameOrNull()
    {
        var domainFormatSetting = _appConfigurationAccessor.Configuration[DomainFormatConfigurationKey];
        if (domainFormatSetting.IsNullOrWhiteSpace())
        {
            // Not configured -> fail closed. Never fall back to the host/null tenant.
            return null;
        }

        var hostName = _hostNameProvider.GetHostNameOrNull();
        if (hostName.IsNullOrWhiteSpace())
        {
            return null;
        }

        hostName = hostName.RemovePreFix("http://", "https://").Split(':')[0].RemovePostFix("/");

        foreach (var rawFormat in domainFormatSetting.Split(';'))
        {
            var domainFormat = rawFormat.Trim()
                .RemovePreFix("http://", "https://")
                .Split(':')[0]
                .RemovePostFix("/");

            if (domainFormat.IsNullOrWhiteSpace() || !domainFormat.Contains(TenancyNamePlaceHolder))
            {
                // A format with no placeholder can only ever match the bare apex domain, which would mean
                // "every tenant". Refuse to use it.
                continue;
            }

            var result = new FormattedStringValueExtracter().Extract(hostName, domainFormat, true, '/');
            if (!result.IsMatch || !result.Matches.Any())
            {
                continue;
            }

            var tenancyName = result.Matches[0].Value;
            if (tenancyName.IsNullOrWhiteSpace())
            {
                continue;
            }

            if (string.Equals(tenancyName, "www", StringComparison.OrdinalIgnoreCase))
            {
                // Same carve-out as DomainTenantCheckMiddleware: www is the marketing apex, not a tenant.
                continue;
            }

            return tenancyName;
        }

        return null;
    }
}
