using System.IO;
using System.Text;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.OpenIddict;
using Abp.AspNetCore.SignalR;
using Abp.AspNetZeroCore.Web;
using Abp.Configuration.Startup;
using Abp.Hangfire;
using Abp.Hangfire.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Runtime.Caching.Redis;
using Abp.Zero.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using HCMv3.Configuration;
using HCMv3.EntityFrameworkCore;
using HCMv3.Startup;
using HCMv3.Web.Authentication.JwtBearer;
using HCMv3.Web.Common;
using HCMv3.Web.Configuration;
using Abp.HtmlSanitizer;
using Abp.HtmlSanitizer.Configuration;
using HCMv3.Authorization.Accounts;

namespace HCMv3.Web;

[DependsOn(
    typeof(HCMv3ApplicationModule),
    typeof(HCMv3EntityFrameworkCoreModule),
    typeof(AbpAspNetZeroCoreWebModule),
    typeof(AbpAspNetCoreSignalRModule),
    typeof(HCMv3GraphQLModule),
    typeof(AbpRedisCacheModule), //AbpRedisCacheModule dependency (and Abp.RedisCache nuget package) can be removed if not using Redis cache
    typeof(AbpHangfireAspNetCoreModule), //AbpHangfireModule dependency (and Abp.Hangfire.AspNetCore nuget package) can be removed if not using Hangfire
    typeof(AbpHtmlSanitizerModule),
    typeof(AbpAspNetCoreOpenIddictModule)
)]
public class HCMv3WebCoreModule : AbpModule
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfigurationRoot _appConfiguration;

    public HCMv3WebCoreModule(IWebHostEnvironment env)
    {
        _env = env;
        _appConfiguration = env.GetAppConfiguration();
    }

    public override void PreInitialize()
    {
        //Set default connection string
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            HCMv3Consts.ConnectionStringName
        );

        //Use database for language management
        Configuration.Modules.Zero().LanguageManagement.EnableDbLocalization();

        Configuration.Modules.AbpAspNetCore()
            .CreateControllersForAppServices(
                typeof(HCMv3ApplicationModule).GetAssembly()
            );

        if (_appConfiguration["Authentication:JwtBearer:IsEnabled"] != null &&
            bool.Parse(_appConfiguration["Authentication:JwtBearer:IsEnabled"]))
        {
            ConfigureTokenAuth();
        }

        Configuration.ReplaceService<IAppConfigurationAccessor, AppConfigurationAccessor>();

        Configuration.ReplaceService<IAppConfigurationWriter, AppConfigurationWriter>();

        if (WebConsts.HangfireDashboardEnabled)
        {
            Configuration.BackgroundJobs.UseHangfire();
        }

        //Uncomment this line to use Redis cache instead of in-memory cache.
        //See app.config for Redis configuration and connection string
        //Configuration.Caching.UseRedis(options =>
        //{
        //    options.ConnectionString = _appConfiguration["Abp:RedisCache:ConnectionString"];
        //    options.DatabaseId = _appConfiguration.GetValue<int>("Abp:RedisCache:DatabaseId");
        //});

        // HTML Sanitizer configuration
        Configuration.Modules.AbpHtmlSanitizer()
            .KeepChildNodes()
            .AddSelector<IAccountAppService>(x => nameof(x.IsTenantAvailable))
            .AddSelector<IAccountAppService>(x => nameof(x.Register));
    }

    private void ConfigureTokenAuth()
    {
        IocManager.Register<TokenAuthConfiguration>();
        var tokenAuthConfig = IocManager.Resolve<TokenAuthConfiguration>();

        tokenAuthConfig.SecurityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_appConfiguration["Authentication:JwtBearer:SecurityKey"])
        );

        tokenAuthConfig.Issuer = _appConfiguration["Authentication:JwtBearer:Issuer"];
        tokenAuthConfig.Audience = _appConfiguration["Authentication:JwtBearer:Audience"];
        tokenAuthConfig.SigningCredentials =
            new SigningCredentials(tokenAuthConfig.SecurityKey, SecurityAlgorithms.HmacSha256);
        tokenAuthConfig.AccessTokenExpiration = AppConsts.AccessTokenExpiration;
        tokenAuthConfig.RefreshTokenExpiration = AppConsts.RefreshTokenExpiration;
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3WebCoreModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        SetAppFolders();

        IocManager.Resolve<ApplicationPartManager>()
            .AddApplicationPartsIfNotAddedBefore(typeof(HCMv3WebCoreModule).Assembly);
    }

    private void SetAppFolders()
    {
        var appFolders = IocManager.Resolve<AppFolders>();

        appFolders.SampleProfileImagesFolder = Path.Combine(_env.WebRootPath,
            $"Common{Path.DirectorySeparatorChar}Images{Path.DirectorySeparatorChar}SampleProfilePics");
        appFolders.WebLogsFolder = Path.Combine(_env.ContentRootPath, $"App_Data{Path.DirectorySeparatorChar}Logs");
    }
}

