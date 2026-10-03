using System.Threading.Tasks;
using Abp.Configuration;
using HCMv3.Configuration;
using HCMv3.SetupChecks;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SetupChecks;

// ReSharper disable once InconsistentNaming
public class ReleaseVersionTracker_Tests : TenantVersionCheckTestBase
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

    /// <summary>As if the tenant had last been seen on an older build, before this update.</summary>
    private Task LastSeen(int tenantId, string version) =>
        _settings.ChangeSettingForTenantAsync(tenantId, AppSettings.Releases.LastSeenVersion, version);

    [Fact]
    public async Task Should_Record_The_Current_Version_And_Have_No_Previous_The_First_Time()
    {
        var tenantId = CreateEmptyTenant("first");

        var state = await _tracker.RecordAsync(tenantId);

        state.CurrentVersion.ShouldBe("3.0.5");
        state.HasPrevious.ShouldBeFalse();
        state.PreviousVersion.ShouldBeNull();
        (await Setting(tenantId, AppSettings.Releases.LastSeenVersion)).ShouldBe("3.0.5");
    }

    [Fact]
    public async Task Should_Have_No_Previous_While_The_Version_Is_Unchanged()
    {
        var tenantId = CreateEmptyTenant("unchanged");
        await _tracker.RecordAsync(tenantId);

        var state = await _tracker.RecordAsync(tenantId);

        state.HasPrevious.ShouldBeFalse();
        (await Setting(tenantId, AppSettings.Releases.PreviousVersion)).ShouldBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Keep_The_Old_Version_As_Previous_When_The_System_Is_Updated()
    {
        var tenantId = CreateEmptyTenant("updated");
        await LastSeen(tenantId, "3.0.3");

        var state = await _tracker.RecordAsync(tenantId);

        state.HasPrevious.ShouldBeTrue();
        state.PreviousVersion.ShouldBe("3.0.3");
        state.CurrentVersion.ShouldBe("3.0.5");
        state.UpdatedOn.ShouldNotBeNull();
        (await Setting(tenantId, AppSettings.Releases.LastSeenVersion)).ShouldBe("3.0.5");
    }

    [Fact]
    public async Task Should_Keep_The_Previous_Version_On_Later_Visits_Until_The_Next_Update()
    {
        var tenantId = CreateEmptyTenant("later");
        await LastSeen(tenantId, "3.0.3");
        var first = await _tracker.RecordAsync(tenantId);

        var again = await _tracker.RecordAsync(tenantId);

        again.PreviousVersion.ShouldBe("3.0.3");
        again.UpdatedOn.ShouldBe(first.UpdatedOn);
    }

    [Fact]
    public async Task Should_Track_Each_Tenant_Separately()
    {
        var updated = CreateEmptyTenant("tenanta");
        var fresh = CreateEmptyTenant("tenantb");
        await LastSeen(updated, "3.0.4");

        (await _tracker.RecordAsync(updated)).PreviousVersion.ShouldBe("3.0.4");
        (await _tracker.RecordAsync(fresh)).HasPrevious.ShouldBeFalse();
    }
}
