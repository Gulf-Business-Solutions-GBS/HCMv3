using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Abp;
using Abp.AspNetZeroCore;
using Abp.AspNetZeroCore.Timing;
using Abp.Mapperly;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Modules;
using Abp.Net.Mail;
using Abp.Reflection.Extensions;
using Abp.Timing;
using Abp.Configuration.Startup;
using Abp.Domain.Uow;
using Abp.Events.Bus;
using Abp.Events.Bus.Exceptions;
using Abp.Json;
using Abp.Localization.Dictionaries.Xml;
using Abp.Localization.Sources;
using Abp.MailKit;
using Abp.Net.Mail.Smtp;
using Abp.OpenIddict;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Zero;
using Abp.Zero.Configuration;
using Abp.Zero.Ldap;
using Abp.Zero.Ldap.Configuration;
using Castle.MicroKernel.Registration;
using MailKit.Security;
using HCMv3.Authentication.TwoFactor;
using HCMv3.Authorization.Delegation;
using HCMv3.Authorization.Impersonation;
using HCMv3.Authorization.Ldap;
using HCMv3.Authorization.PasswordlessLogin;
using HCMv3.Authorization.QrLogin;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Users;
using HCMv3.Chat;
using HCMv3.Configuration;
using HCMv3.DashboardCustomization.Definitions;
using HCMv3.Debugging;
using HCMv3.DynamicEntityProperties;
using HCMv3.Features;
using HCMv3.Friendships;
using HCMv3.Friendships.Cache;
using HCMv3.Localization;
using HCMv3.MultiTenancy;
using HCMv3.Net.Emailing;
using HCMv3.Notifications;
using HCMv3.WebHooks;
using HCMv3.Authorization.Session;

namespace HCMv3;

[DependsOn(
    typeof(HCMv3CoreSharedModule),
    typeof(AbpZeroCoreModule),
    typeof(AbpZeroLdapModule),
    typeof(AbpMapperlyModule),
    typeof(AbpAspNetZeroCoreModule),
    typeof(AbpMailKitModule),
    typeof(AbpZeroCoreOpenIddictModule))]
public class HCMv3CoreModule : AbpModule
{
    public override void PreInitialize()
    {
        //workaround for issue: https://github.com/aspnet/EntityFrameworkCore/issues/9825
        //related github issue: https://github.com/aspnet/EntityFrameworkCore/issues/10407
        AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue9825", true);

        Configuration.Auditing.IsEnabledForAnonymousUsers = true;

        //Declare entity types
        Configuration.Modules.Zero().EntityTypes.Tenant = typeof(Tenant);
        Configuration.Modules.Zero().EntityTypes.Role = typeof(Role);
        Configuration.Modules.Zero().EntityTypes.User = typeof(User);

        HCMv3LocalizationConfigurer.Configure(Configuration.Localization);

        //Adding feature providers
        Configuration.Features.Providers.Add<AppFeatureProvider>();

        //Adding setting providers
        Configuration.Settings.Providers.Add<AppSettingProvider>();

        //Adding notification providers
        Configuration.Notifications.Providers.Add<AppNotificationProvider>();

        //Adding webhook definition providers
        Configuration.Webhooks.Providers.Add<AppWebhookDefinitionProvider>();
        Configuration.Webhooks.TimeoutDuration = TimeSpan.FromMinutes(1);
        Configuration.Webhooks.IsAutomaticSubscriptionDeactivationEnabled = false;

        //Enable this line to create a multi-tenant application.
        Configuration.MultiTenancy.IsEnabled = HCMv3Consts.MultiTenancyEnabled;

        //Enable LDAP authentication
        //Configuration.Modules.ZeroLdap().Enable(typeof(AppLdapAuthenticationSource));

        //Twilio - Enable this line to activate Twilio SMS integration
        //Configuration.ReplaceService<ISmsSender,TwilioSmsSender>();

        //Adding DynamicEntityParameters definition providers
        Configuration.DynamicEntityProperties.Providers.Add<AppDynamicEntityPropertyDefinitionProvider>();

        // MailKit configuration
        Configuration.Modules.AbpMailKit().SecureSocketOption = SecureSocketOptions.Auto;
        Configuration.ReplaceService<IMailKitSmtpBuilder, HCMv3MailKitSmtpBuilder>(DependencyLifeStyle.Transient);

        //Configure roles
        AppRoleConfig.Configure(Configuration.Modules.Zero().RoleManagement);

        if (DebugHelper.IsDebug)
        {
            //Disabling email sending in debug mode
            Configuration.ReplaceService<IEmailSender, NullEmailSender>(DependencyLifeStyle.Transient);
        }

        Configuration.ReplaceService(typeof(IEmailSenderConfiguration), () =>
        {
            Configuration.IocManager.IocContainer.Register(
                Component.For<IEmailSenderConfiguration, ISmtpEmailSenderConfiguration>()
                         .ImplementedBy<HCMv3SmtpEmailSenderConfiguration>()
                         .LifestyleTransient()
            );
        });

        // Configures caching with sliding expiration times for  cache items.
        ConfigureCaching();

        IocManager.Register<DashboardConfiguration>();

        Configuration.Notifications.Notifiers.Add<SmsRealTimeNotifier>();
        Configuration.Notifications.Notifiers.Add<EmailRealTimeNotifier>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(HCMv3CoreModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        IocManager.RegisterIfNot<IChatCommunicator, NullChatCommunicator>();
        IocManager.Register<IUserDelegationConfiguration, UserDelegationConfiguration>();

        IocManager.Resolve<ChatUserStateWatcher>().Initialize();
        IocManager.Resolve<AppTimes>().StartupTime = Clock.Now;

        var workManager = IocManager.Resolve<IBackgroundWorkerManager>();
        workManager.Add(IocManager.Resolve<ExpiredSessionCleanupWorker>());
    }

    private void ConfigureCaching()
    {
        Configuration.Caching.Configure(FriendCacheItem.CacheName, cache =>
        {
            cache.DefaultSlidingExpireTime = FriendCacheItem.DefaultSlidingExpireTime;
        });

        Configuration.Caching.Configure(TwoFactorCodeCacheItem.CacheName, cache =>
        {
            cache.DefaultSlidingExpireTime = TwoFactorCodeCacheItem.DefaultSlidingExpireTime;
        });

        Configuration.Caching.Configure(PasswordlessLoginCodeCacheItem.CacheName, cache =>
        {
            cache.DefaultSlidingExpireTime = PasswordlessLoginCodeCacheItem.DefaultSlidingExpireTime;
        });

        Configuration.Caching.Configure(ImpersonationCacheItem.CacheName, cache =>
        {
            cache.DefaultSlidingExpireTime = ImpersonationCacheItem.DefaultSlidingExpireTime;
        });

        Configuration.Caching.Configure(SwitchToLinkedAccountCacheItem.CacheName, cache =>
        {
            cache.DefaultSlidingExpireTime = SwitchToLinkedAccountCacheItem.DefaultSlidingExpireTime;
        });

        Configuration.Caching.Configure(QrLoginSessionIdCacheItem.CacheName, cache =>
        {
            cache.DefaultSlidingExpireTime = QrLoginSessionIdCacheItem.DefaultSlidingExpireTime;
        });
    }
}

