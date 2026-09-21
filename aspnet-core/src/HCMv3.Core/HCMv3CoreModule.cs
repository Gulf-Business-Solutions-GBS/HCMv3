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
using Abp.Domain.Entities;
using Abp.Events.Bus.Entities;
using Abp.Events.Bus;
using Abp.Events.Bus.Handlers;
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
using HCMv3.Integrations.Services;
using Castle.MicroKernel.Registration;
using MailKit.Security;
using HCMv3.Authentication.TwoFactor;
using HCMv3.Authorization.Delegation;
using HCMv3.Authorization.Impersonation;
using HCMv3.Authorization.Ldap;
using HCMv3.Authorization.PasswordlessLogin;
using HCMv3.Authorization.QrLogin;
using HCMv3.Authorization.Roles;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.BusinessRules;
using HCMv3.PayrollManagement.PayrollLock;
using HCMv3.SystemSecurity;
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

        // Hides workflow-managed rows that are not approved yet, from every query, by default. See
        // HCMv3DataFilters.ApprovedOnly -- this is what lets a pending leave live in EmployeeAbsences
        // itself instead of a shadow LeaveRequest table. Registered enabled (the second argument): opting
        // OUT has to be the deliberate act, because the failure mode of forgetting is pending data leaking
        // into payroll and reports.
        Configuration.UnitOfWork.RegisterFilter(HCMv3DataFilters.ApprovedOnly, true);

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
        else
        {
            // Honour the Integration Center's SMTP switch. Registered only outside debug so that the
            // blanket debug suppression above still wins -- a developer who turned the switch on should
            // not start sending real mail from a local build.
            Configuration.ReplaceService<IEmailSender, HCMv3GatedEmailSender>(DependencyLifeStyle.Transient);
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

        RegisterBusinessRuleEntityHooks();
        RegisterPayrollLockHooks();
        RegisterIntegrationConnectors();
    }

    /// <summary>
    /// Exposes every Integration Center connector under <see cref="IIntegrationConnector"/>.
    ///
    /// THIS IS NOT REDUNDANT with the convention registration above, and leaving it out is a silent
    /// failure rather than a loud one. ABP's conventional registrar registers a class under itself and
    /// under its DEFAULT interface -- the one whose name matches, so <c>BioTimeConnector</c> would be
    /// registered as <c>IBioTimeConnector</c>, which does not exist. Nothing is registered as
    /// <see cref="IIntegrationConnector"/>, <c>ResolveAll</c> returns an empty array, and the Integration
    /// Center renders a page with no cards on it and no error anywhere.
    ///
    /// Scanned rather than listed so that the design promise holds: a new integration is one new class,
    /// with no registration list to remember to update. The explicit name keeps this registration from
    /// colliding with the conventional one for the same implementation.
    /// </summary>
    private void RegisterIntegrationConnectors()
    {
        IocManager.IocContainer.Register(
            Classes.FromAssembly(typeof(HCMv3CoreModule).GetAssembly())
                .BasedOn<IIntegrationConnector>()
                .WithService.Base()
                .Configure(c => c.Named(c.Implementation.FullName + ":IIntegrationConnector"))
                .LifestyleTransient());
    }

    /// <summary>
    /// The payroll lock check on every save, and the cache reset when System Objects change. By hand, and with
    /// the concrete types in the list, for the reasons given on <see cref="RegisterBusinessRuleEntityHooks"/>.
    /// </summary>
    private void RegisterPayrollLockHooks()
    {
        IocManager.IocContainer.Register(
            Component
                .For<IEventHandler<EntityChangingEventData<Entity<int>>>, PayrollLockEntityHook>()
                .ImplementedBy<PayrollLockEntityHook>()
                .LifestyleTransient());

        IocManager.IocContainer.Register(
            Component
                .For<IEventHandler<EntityChangedEventData<SysObject>>, PayrollSensitiveCacheInvalidator>()
                .ImplementedBy<PayrollSensitiveCacheInvalidator>()
                .LifestyleTransient());
    }

    /// <summary>
    /// Wires the business rule engine to entity save events, one entity at a time.
    ///
    /// <para>
    /// Registered by hand rather than by convention, and that is not a style choice. Castle binds a
    /// conventionally registered class to the interfaces whose names match it by suffix; no reasonable
    /// class name matches <c>IEventHandler&lt;EntityCreatingEventData&lt;T&gt;&gt;</c>, so a hook left to
    /// convention registers as itself, the event bus never subscribes it, and every rule armed on that
    /// entity silently never runs. The same silent-registration trap as
    /// <c>{X}WorkflowTransactionHandler</c>, with no resolver to notice the absence.
    /// </para>
    ///
    /// <para>
    /// Adding an entity to the engine is a line here plus a hook class plus a base object provider. Leaving
    /// an entity out is the default, which is the intended bias: a rule can only reach what somebody
    /// deliberately exposed.
    /// </para>
    ///
    /// <para>
    /// THE CONCRETE TYPE MUST BE IN THE <c>For&lt;&gt;</c> LIST, not only the handler interfaces. ABP's
    /// <c>IocHandlerFactory</c> resolves a handler by its implementation type when the event fires, so
    /// registering the interfaces alone leaves the container able to enumerate the handler but unable to
    /// build it -- every save of that entity then dies with "no component for supporting the service". The
    /// resolver tests caught this; without them it would have surfaced as the Employee screen failing to
    /// save at all.
    /// </para>
    /// </summary>
    private void RegisterBusinessRuleEntityHooks()
    {
        IocManager.IocContainer.Register(
            Component
                .For<IEventHandler<EntityCreatingEventData<Employee>>,
                    IEventHandler<EntityUpdatingEventData<Employee>>,
                    IEventHandler<EntityDeletingEventData<Employee>>,
                    EmployeeBusinessRuleHook>()
                .ImplementedBy<EmployeeBusinessRuleHook>()
                .LifestyleTransient());

        IocManager.IocContainer.Register(
            Component
                .For<IEventHandler<EntityCreatingEventData<EmployeeAbsence>>,
                    IEventHandler<EntityUpdatingEventData<EmployeeAbsence>>,
                    IEventHandler<EntityDeletingEventData<EmployeeAbsence>>,
                    EmployeeAbsenceBusinessRuleHook>()
                .ImplementedBy<EmployeeAbsenceBusinessRuleHook>()
                .LifestyleTransient());
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

