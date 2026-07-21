using System;
using System.Text;
using Abp.AspNetCore.OpenIddict.Claims;
using Abp.OpenIddict.Applications;
using Abp.OpenIddict.Authorizations;
using Abp.OpenIddict.Scopes;
using Abp.OpenIddict.Tokens;
using Abp.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Core;

namespace HCMv3.Web.OpenIddict;

public static class OpenIddictRegistrar
{
    public static void Register(
        IServiceCollection services,
        IConfigurationRoot configuration,
        Action<OpenIddictCoreOptions> setupOptions)
    {
        services.Configure<AbpOpenIddictClaimsPrincipalOptions>(options =>
        {
            options.ClaimsPrincipalHandlers.Add<AbpDefaultOpenIddictClaimsPrincipalHandler>();
        });

        services.AddOpenIddict()

            // Register the OpenIddict core components.
            .AddCore(builder =>
            {
                builder
                    .SetDefaultApplicationEntity<OpenIddictApplicationModel>()
                    .SetDefaultAuthorizationEntity<OpenIddictAuthorizationModel>()
                    .SetDefaultScopeEntity<OpenIddictScopeModel>()
                    .SetDefaultTokenEntity<OpenIddictTokenModel>();

                builder
                    .ReplaceApplicationStore<OpenIddictApplicationModel, AbpOpenIddictApplicationStore>()
                    .ReplaceAuthorizationStore<OpenIddictAuthorizationModel, AbpOpenIddictAuthorizationStore>()
                    .ReplaceScopeStore<OpenIddictScopeModel, AbpOpenIddictScopeStore>()
                    .ReplaceTokenStore<OpenIddictTokenModel, AbpOpenIddictTokenStore>();
            })

            // Register the OpenIddict server components.
            .AddServer(options =>
            {
                // Enable the token endpoint.
                options.SetAuthorizationEndpointUris("connect/authorize", "connect/authorize/callback")
                    .SetTokenEndpointUris("connect/token")
                    .SetUserInfoEndpointUris("connect/userinfo");

                // Enable the client credentials flow.
                options.AllowClientCredentialsFlow();
                options.AllowPasswordFlow();
                options.AllowAuthorizationCodeFlow();

                // Register the signing and encryption credentials.
                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                // Register the ASP.NET Core host and configure the ASP.NET Core options.
                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableEndUserVerificationEndpointPassthrough()
                    .EnableStatusCodePagesIntegration();

                options.DisableAccessTokenEncryption();
            })

            // Register the OpenIddict validation components.
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance.
                options.UseLocalServer();

                // Register the ASP.NET Core host.
                options.UseAspNetCore();
            });

        services.AddHostedService<OpenIdDictDataSeedWorker>();
    }

    public static void Register(IServiceCollection services, IConfigurationRoot configuration)
    {
        Register(services, configuration, options => { });
    }
}

