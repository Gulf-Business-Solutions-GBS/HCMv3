using System.Linq;
using System.Threading.Tasks;
using Abp.Configuration;
using HCMv3.Configuration;
using HCMv3.SetupChecks;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SetupChecks;

// ReSharper disable once InconsistentNaming
public class ReleaseVersionTracker_Tests : TenantSetupCheckTestBase
{
    private readonly ReleaseVersionTracker _tracker;
    private readonly ISettingManager _settings;

    public ReleaseVersionTracker_Tests()
    {
        LoginAsHostAdmin();
        _tracker = Resolve<ReleaseVersionTracker>();
        _settings = Resolve<ISettingManager>();
    }

    private Task<string> Setting(int tenantId, string name) => _settings.GetSettingValueForTenantAsync(name, tenantId);

    /// <summary>As if the tenant had last been seen on an older release, before this update.</summary>
    private Task LastSeen(int tenantId, string version) =>
        _settings.ChangeSettingForTenantAsync(tenantId, AppSettings.Releases.LastSeenVersion, version);

    [Fact]
    public async Task Should_Record_The_Current_Version_And_Offer_No_Comparison_The_First_Time()
    {
        var tenantId = CreateEmptyTenant("first");

        var state = await _tracker.RecordAsync(tenantId);

        state.CurrentVersion.ShouldBe(V320);
        state.HasComparison.ShouldBeFalse();
        state.PreviousVersion.ShouldBeNull();
        (await Setting(tenantId, AppSettings.Releases.LastSeenVersion)).ShouldBe(V320);
    }

    [Fact]
    public async Task Should_Offer_No_Comparison_While_The_Version_Is_Unchanged()
    {
        var tenantId = CreateEmptyTenant("unchanged");
        await _tracker.RecordAsync(tenantId);

        var state = await _tracker.RecordAsync(tenantId);

        state.HasComparison.ShouldBeFalse();
        (await Setting(tenantId, AppSettings.Releases.PreviousVersion)).ShouldBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Keep_The_Old_Version_As_Previous_When_The_System_Is_Updated()
    {
        var tenantId = CreateEmptyTenant("updated");
        await LastSeen(tenantId, V300);

        var state = await _tracker.RecordAsync(tenantId);

        state.HasComparison.ShouldBeTrue();
        state.PreviousVersion.ShouldBe(V300);
        state.CurrentVersion.ShouldBe(V320);
        state.UpdatedOn.ShouldNotBeNull();
        (await Setting(tenantId, AppSettings.Releases.LastSeenVersion)).ShouldBe(V320);
    }

    [Fact]
    public async Task Should_Keep_The_Comparison_On_Later_Visits_Until_The_Next_Update()
    {
        var tenantId = CreateEmptyTenant("later");
        await LastSeen(tenantId, V300);
        var first = await _tracker.RecordAsync(tenantId);

        var again = await _tracker.RecordAsync(tenantId);

        again.PreviousVersion.ShouldBe(V300);
        again.UpdatedOn.ShouldBe(first.UpdatedOn);
    }

    [Fact]
    public async Task Should_Track_Each_Tenant_Separately()
    {
        var updated = CreateEmptyTenant("tenanta");
        var fresh = CreateEmptyTenant("tenantb");
        await LastSeen(updated, V310);

        (await _tracker.RecordAsync(updated)).PreviousVersion.ShouldBe(V310);
        (await _tracker.RecordAsync(fresh)).HasComparison.ShouldBeFalse();
    }

    [Fact]
    public void Should_List_The_Releases_Newer_Than_The_Previous_One_Oldest_First()
    {
        _tracker.ReleasesSince(V300).Select(r => r.Version).ShouldBe(new[] { V310, V320 });
        _tracker.ReleasesSince(V310).Select(r => r.Version).ShouldBe(new[] { V320 });
        _tracker.ReleasesSince(V320).ShouldBeEmpty();
    }

    [Fact]
    public void Should_List_Nothing_For_A_Version_It_Cannot_Read()
    {
        _tracker.ReleasesSince(null).ShouldBeEmpty();
        _tracker.ReleasesSince("not a version").ShouldBeEmpty();
    }
}
