using System.Collections.Generic;
using Abp.AspNetZeroCore;
using Abp.AspNetZeroCore.Web.Authentication.External;
using Abp.AspNetZeroCore.Web.Authentication.External.Facebook;
using Abp.AspNetZeroCore.Web.Authentication.External.Google;
using Abp.AspNetZeroCore.Web.Authentication.External.Microsoft;
using Abp.AspNetZeroCore.Web.Authentication.External.OpenIdConnect;
using Abp.AspNetZeroCore.Web.Authentication.External.Twitter;
using Abp.AspNetZeroCore.Web.Authentication.External.WsFederation;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Extensions;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Threading.BackgroundWorkers;
using Abp.Timing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using HCMv3.Auditing;
using HCMv3.Authorization.Users.Password;
using HCMv3.Careers;
using HCMv3.Configuration;
using HCMv3.EntityFrameworkCore;
using HCMv3.MultiTenancy;
using HCMv3.MultiTenancy.Subscription;
using HCMv3.TimeManagement;
using HCMv3.Web.Startup.ExternalLoginInfoProviders;
using Castle.MicroKernel.Registration;
using HCMv3.Web.Controllers;

namespace HCMv3.Web.Startup;

[DependsOn(
    typeof(HCMv3WebCoreModule)
)]
public class HCMv3WebHostModule : AbpModule
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfigurationRoot _appConfiguration;

    public HCMv3WebHostModule(
        IWebHostEnvironment env)
    {
        _env = env;
        _appConfiguration = env.GetAppConfiguration();
    }

    public override void PreInitialize()
    {
        Configuration.Modules.AbpWebCommon().MultiTenancy.DomainFormat =
            _appConfiguration["App:ServerRootAddress"] ?? "https://localhost:44301/";
        Configuration.Modules.AspNetZero().LicenseCode = _appConfiguration["AbpZeroLicenseCode"];

        // Careers portal (wave R2). Lets ABP resolve the ambient tenant from a careers subdomain so that
        // candidates authenticate against their own tenant through the STOCK TokenAuthController rather
        // than through a second, careers-specific login implementation. Inserted FIRST so a careers host
        // wins over the header/cookie contributors, which an anonymous careers client does not set anyway.
        //
        // Inert until App:CareersDomainFormat is configured (it is not, by instruction): with the key unset
        // the contributor returns null for every request and resolution behaves exactly as it does today.
        // See CareersTenantResolveContributor for the full blast-radius note.
        Configuration.MultiTenancy.Resolvers.Insert(0, typeof(CareersTenantResolveContributor));
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3WebHostModule).GetAssembly());

        // ABP replaces MVC's controller activator with one that resolves from Windsor, and the
        // convention scan above does not pick up the DevExpress reporting controllers (they derive
        // from DevExpress's own Controller subclass rather than from AbpController). Without these
        // three lines every request to /DXXRD, /DXXRDV and /DXXQB routes correctly and then fails
        // with "No component for supporting the service ... was found".
        IocManager.IocContainer.Register(
            Component.For<DXXRDController>().LifestyleTransient(),
            Component.For<DXXRDVController>().LifestyleTransient(),
            Component.For<DXXQBController>().LifestyleTransient()
        );
    }

    public override void PostInitialize()
    {
        using (var scope = IocManager.CreateScope())
        {
            if (!scope.Resolve<DatabaseCheckHelper>().Exist(_appConfiguration["ConnectionStrings:Default"]))
            {
                return;
            }
        }

        var workManager = IocManager.Resolve<IBackgroundWorkerManager>();
        if (IocManager.Resolve<IMultiTenancyConfig>().IsEnabled)
        {
            workManager.Add(IocManager.Resolve<SubscriptionExpirationCheckWorker>());
            workManager.Add(IocManager.Resolve<SubscriptionExpireEmailNotifierWorker>());
            workManager.Add(IocManager.Resolve<SubscriptionPaymentNotCompletedEmailNotifierWorker>());
        }

        var expiredAuditLogDeleterWorker = IocManager.Resolve<ExpiredAuditLogDeleterWorker>();
        if (Configuration.Auditing.IsEnabled && expiredAuditLogDeleterWorker.IsEnabled)
        {
            workManager.Add(expiredAuditLogDeleterWorker);
        }

        workManager.Add(IocManager.Resolve<PasswordExpirationBackgroundWorker>());

        // Wave T4: the nightly unattended Time Engine run. Registered unconditionally -- the worker itself
        // is a no-op per tenant unless that tenant's ManageTimeSettings.EnableTimeEngineAutorun is on, which
        // is a per-tenant runtime setting and therefore cannot be evaluated here at host start-up.
        workManager.Add(IocManager.Resolve<TimeEngineAutorunBackgroundWorker>());

        ConfigureExternalAuthProviders();
    }

    private void ConfigureExternalAuthProviders()
    {
        var externalAuthConfiguration = IocManager.Resolve<ExternalAuthConfiguration>();

        if (bool.Parse(_appConfiguration["Authentication:OpenId:IsEnabled"]))
        {
            if (bool.Parse(_appConfiguration["Authentication:AllowSocialLoginSettingsPerTenant"]))
            {
                externalAuthConfiguration.ExternalLoginInfoProviders.Add(
                    IocManager.Resolve<TenantBasedOpenIdConnectExternalLoginInfoProvider>());
            }
            else
            {
                var jsonClaimMappings = new List<JsonClaimMap>();
                _appConfiguration.GetSection("Authentication:OpenId:ClaimsMapping").Bind(jsonClaimMappings);

                externalAuthConfiguration.ExternalLoginInfoProviders.Add(
                    new OpenIdConnectExternalLoginInfoProvider(
                        _appConfiguration["Authentication:OpenId:ClientId"],
                        _appConfiguration["Authentication:OpenId:ClientSecret"],
                        _appConfiguration["Authentication:OpenId:Authority"],
                        _appConfiguration["Authentication:OpenId:LoginUrl"],
                        bool.Parse(_appConfiguration["Authentication:OpenId:ValidateIssuer"]),
                        _appConfiguration["Authentication:OpenId:ResponseType"],
                        jsonClaimMappings
                    )
                );
            }
        }

        if (bool.Parse(_appConfiguration["Authentication:WsFederation:IsEnabled"]))
        {
            if (bool.Parse(_appConfiguration["Authentication:AllowSocialLoginSettingsPerTenant"]))
            {
                externalAuthConfiguration.ExternalLoginInfoProviders.Add(
                    IocManager.Resolve<TenantBasedWsFederationExternalLoginInfoProvider>());
            }
            else
            {
                var jsonClaimMappings = new List<JsonClaimMap>();
                _appConfiguration.GetSection("Authentication:WsFederation:ClaimsMapping").Bind(jsonClaimMappings);

                externalAuthConfiguration.ExternalLoginInfoProviders.Add(
                    new WsFederationExternalLoginInfoProvider(
                        _appConfiguration["Authentication:WsFederation:ClientId"],
                        _appConfiguration["Authentication:WsFederation:Tenant"],
                        _appConfiguration["Authentication:WsFederation:MetaDataAddress"],
                        _appConfiguration["Authentication:WsFederation:Authority"],
                        jsonClaimMappings
                    )
                );
            }
        }

        if (bool.Parse(_appConfiguration["Authentication:Facebook:IsEnabled"]))
        {
            if (bool.Parse(_appConfiguration["Authentication:AllowSocialLoginSettingsPerTenant"]))
            {
                externalAuthConfiguration.ExternalLoginInfoProviders.Add(
                    IocManager.Resolve<TenantBasedFacebookExternalLoginInfoProvider>());
            }
            else
            {
                externalAuthConfiguration.ExternalLoginInfoProviders.Add(new FacebookExternalLoginInfoProvider(
                    _appConfiguration["Authentication:Facebook:AppId"],
                    _appConfiguration["Authentication:Facebook:AppSecret"]
                ));
            }
        }

        if (bool.Parse(_appConfiguration["Authentication:Twitter:IsEnabled"]))
        {
            if (bool.Parse(_appConfiguration["Authentication:AllowSocialLoginSettingsPerTenant"]))
            {
                externalAuthConfiguration.ExternalLoginInfoProviders.Add(
                    IocManager.Resolve<TenantBasedTwitterExternalLoginInfoProvider>());
            }
            else
            {
                var twitterExternalLoginInfoProvider = new TwitterExternalLoginInfoProvider(
                    _appConfiguration["Authentication:Twitter:ConsumerKey"],
                    _appConfiguration["Authentication:Twitter:ConsumerSecret"],
                    _appConfiguration["App:ClientRootAddress"].EnsureEndsWith('/') + "account/login"
                );

                externalAuthConfiguration.ExternalLoginInfoProviders.Add(twitterExternalLoginInfoProvider);
            }
        }

        if (bool.Parse(_appConfiguration["Authentication:Google:IsEnabled"]))
        {
            if (bool.Parse(_appConfiguration["Authentication:AllowSocialLoginSettingsPerTenant"]))
            {
                externalAuthConfiguration.ExternalLoginInfoProviders.Add(
                    IocManager.Resolve<TenantBasedGoogleExternalLoginInfoProvider>());
            }
            else
            {
                externalAuthConfiguration.ExternalLoginInfoProviders.Add(
                    new GoogleExternalLoginInfoProvider(
                        _appConfiguration["Authentication:Google:ClientId"],
                        _appConfiguration["Authentication:Google:ClientSecret"],
                        _appConfiguration["Authentication:Google:UserInfoEndpoint"]
                    )
                );
            }
        }

        if (bool.Parse(_appConfiguration["Authentication:Microsoft:IsEnabled"]))
        {
            if (bool.Parse(_appConfiguration["Authentication:AllowSocialLoginSettingsPerTenant"]))
            {
                externalAuthConfiguration.ExternalLoginInfoProviders.Add(
                    IocManager.Resolve<TenantBasedMicrosoftExternalLoginInfoProvider>());
            }
            else
            {
                externalAuthConfiguration.ExternalLoginInfoProviders.Add(
                    new MicrosoftExternalLoginInfoProvider(
                        _appConfiguration["Authentication:Microsoft:ConsumerKey"],
                        _appConfiguration["Authentication:Microsoft:ConsumerSecret"]
                    )
                );
            }
        }
    }
}

