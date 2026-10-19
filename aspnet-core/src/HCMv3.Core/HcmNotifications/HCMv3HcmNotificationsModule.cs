using Abp.Events.Bus.Entities;
using Abp.Events.Bus.Handlers;
using Abp.Modules;
using Castle.MicroKernel.Registration;
using HCMv3.Licensing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.HcmNotifications;

/// <summary>
/// Plugs HCM providers into ABP's configuration: the notification definitions (Zero's Notification
/// settings screen), the settings they keep, and the HCM licensing features.
///
/// <para>
/// A module of its own so that Zero's template <c>HCMv3CoreModule</c> needs exactly one line -- this type in
/// its <c>[DependsOn]</c> list -- however many HCM providers are added here later. Its classes are registered
/// by the core module's own assembly scan; the only thing registered here is an entity event hook, which
/// convention cannot subscribe.
/// </para>
/// </summary>
public class HCMv3HcmNotificationsModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Notifications.Providers.Add<HcmNotificationProvider>();
        Configuration.Settings.Providers.Add<HcmNotificationSettingProvider>();

        // HCM editions/features (module on/off, max active employees, per-user module licences). Zero's own
        // AppFeatureProvider is left untouched.
        Configuration.Features.Providers.Add<HcmFeatureProvider>();
    }

    public override void Initialize()
    {
        // Max Active Employees on every new employee saved as active. The concrete type must be in the list:
        // ABP resolves a handler by its implementation type when the event fires.
        IocManager.IocContainer.Register(
            Component
                .For<IEventHandler<EntityCreatingEventData<Employee>>, ActiveEmployeeLimitEntityHook>()
                .ImplementedBy<ActiveEmployeeLimitEntityHook>()
                .LifestyleTransient());
    }
}
