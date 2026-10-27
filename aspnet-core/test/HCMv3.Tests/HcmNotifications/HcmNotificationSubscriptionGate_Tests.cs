using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Configuration;
using Abp.Notifications;
using HCMv3.HcmNotifications;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HcmNotifications;

/// <summary>
/// Zero's own subscription machinery, used for HCM notices: the definitions are registered (so they appear on
/// Zero's Notification settings screen), existing users are given each one once, and a user who switches one
/// off stays off.
///
/// <para>
/// These run against the real ABP notification and setting managers, so they need
/// <c>HCMv3HcmNotificationsModule</c> in <c>HCMv3CoreModule</c>'s DependsOn.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class HcmNotificationSubscriptionGate_Tests : HcmNotificationTestBase
{
    private readonly IHcmNotificationSubscriptionGate _gate;
    private readonly INotificationSubscriptionManager _subscriptions;
    private readonly ISettingManager _settings;

    public HcmNotificationSubscriptionGate_Tests()
    {
        _gate = Resolve<IHcmNotificationSubscriptionGate>();
        _subscriptions = Resolve<INotificationSubscriptionManager>();
        _settings = Resolve<ISettingManager>();
    }

    private UserIdentifier Admin => new(TenantId, AbpSession.UserId.Value);

    [Fact]
    public void Should_Register_Every_Hcm_Definition_With_Zero()
    {
        var definitions = Resolve<INotificationDefinitionManager>();

        foreach (var name in HcmNotificationNames.All)
        {
            definitions.GetOrNull(name).ShouldNotBeNull(name);
        }
    }

    [Fact]
    public void Should_Name_Twelve_Definitions_Six_Fixed_And_One_Per_Source()
    {
        HcmNotificationNames.All.Count.ShouldBe(12);
        HcmNotificationNames.All.Distinct().Count().ShouldBe(12);
        HcmNotificationNames.DisplayNameKey(HcmNotificationNames.ForRuleSource(NotificationRuleSourceCodes.Loan))
            .ShouldBe("NotificationDefinition_Rule_Loan");
    }

    [Fact]
    public async Task Should_Subscribe_Existing_Users_Once_And_Record_It()
    {
        await _gate.EnsureDefaultSubscriptionsAsync(TenantId);

        foreach (var name in HcmNotificationNames.All)
        {
            (await _subscriptions.IsSubscribedAsync(Admin, name)).ShouldBeTrue(name);
        }

        var recorded = await _settings.GetSettingValueForTenantAsync(HcmNotificationSettingNames.SubscribedDefinitions, TenantId);
        recorded.Split(',').ShouldBe(HcmNotificationNames.All, ignoreOrder: true);
    }

    [Fact]
    public async Task Should_Leave_A_User_Who_Switched_A_Notice_Off_Switched_Off()
    {
        await _gate.EnsureDefaultSubscriptionsAsync(TenantId);
        await _subscriptions.UnsubscribeAsync(Admin, HcmNotificationNames.News);

        var stillWanted = await _gate.FilterSubscribedAsync(HcmNotificationNames.News, new[] { Admin });
        var otherNotice = await _gate.FilterSubscribedAsync(HcmNotificationNames.ProbationEnd, new[] { Admin });

        stillWanted.ShouldBeEmpty();
        otherNotice.Single().ShouldBe(Admin);
        (await _subscriptions.IsSubscribedAsync(Admin, HcmNotificationNames.News)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Hand_Out_Only_A_Definition_The_Tenant_Has_Not_Had_Yet()
    {
        // As if a release had just added the News definition: every other one was handed out before, and the
        // user has since switched them all off.
        var earlier = string.Join(",", HcmNotificationNames.All.Where(n => n != HcmNotificationNames.News));
        await _settings.ChangeSettingForTenantAsync(TenantId, HcmNotificationSettingNames.SubscribedDefinitions, earlier);
        foreach (var name in HcmNotificationNames.All)
        {
            await _subscriptions.UnsubscribeAsync(Admin, name);
        }

        await _gate.EnsureDefaultSubscriptionsAsync(TenantId);

        (await _subscriptions.IsSubscribedAsync(Admin, HcmNotificationNames.News)).ShouldBeTrue();
        (await _subscriptions.IsSubscribedAsync(Admin, HcmNotificationNames.ProbationEnd)).ShouldBeFalse();
    }
}
