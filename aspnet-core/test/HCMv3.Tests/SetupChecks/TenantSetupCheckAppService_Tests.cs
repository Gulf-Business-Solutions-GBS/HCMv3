using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Configuration;
using Abp.UI;
using HCMv3.Configuration;
using HCMv3.SetupChecks;
using HCMv3.SetupChecks.Dto;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SetupChecks;

// ReSharper disable once InconsistentNaming
public class TenantSetupCheckAppService_Tests : TenantSetupCheckTestBase
{
    private readonly ITenantSetupCheckAppService _appService;

    public TenantSetupCheckAppService_Tests()
    {
        _appService = Resolve<ITenantSetupCheckAppService>();
    }

    /// <summary>A tenant last seen on <paramref name="version"/>, i.e. updated since then to 3.2.0.</summary>
    private int CreateTenantUpdatedFrom(string tenancyName, string version)
    {
        var tenantId = CreateEmptyTenant(tenancyName);
        Resolve<ISettingManager>().ChangeSettingForTenant(tenantId, AppSettings.Releases.LastSeenVersion, version);
        return tenantId;
    }

    private async Task<ReleaseUpdateStatusDto> StatusOf(int tenantId)
    {
        LoginAsAdminOf(tenantId);
        return await _appService.GetUpdateStatus();
    }

    private static ReleaseFeatureDto Feature(ReleaseUpdateStatusDto status, string key) =>
        status.Releases.SelectMany(r => r.Features).Single(f => f.Key == key);

    // ---------------------------------------------------------------- versions

    [Fact]
    public async Task Should_Show_The_Installed_Version_And_No_Comparison_When_None_Was_Recorded()
    {
        var status = await StatusOf(CreateEmptyTenant("fresh"));

        status.CurrentVersion.ShouldBe(V320);
        status.HasComparison.ShouldBeFalse();
        status.PreviousVersion.ShouldBeNull();
        status.Releases.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_List_Every_Release_Since_The_Previous_Version()
    {
        var status = await StatusOf(CreateTenantUpdatedFrom("from300", V300));

        status.HasComparison.ShouldBeTrue();
        status.PreviousVersion.ShouldBe(V300);
        status.Releases.Select(r => r.Version).ShouldBe(new[] { V310, V320 });
        status.Releases[0].Features.Select(f => f.Key).ShouldBe(new[] { "310-plain", "310-currency", "310-broken", "310-unregistered" });
    }

    [Fact]
    public async Task Should_Leave_Out_Releases_The_Tenant_Already_Had()
    {
        var status = await StatusOf(CreateTenantUpdatedFrom("from310", V310));

        status.Releases.Select(r => r.Version).ShouldBe(new[] { V320 });
    }

    [Fact]
    public async Task Should_Offer_Every_Release_Newest_First_To_Look_Up()
    {
        var status = await StatusOf(CreateEmptyTenant("lookup"));

        status.AllReleases.Select(r => r.Version).ShouldBe(new[] { V320, V310, V300 });
    }

    // ---------------------------------------------------------------- setup status

    [Fact]
    public async Task Should_Say_A_Feature_Needs_No_Setup_When_It_Names_No_Check()
    {
        var status = await StatusOf(CreateTenantUpdatedFrom("plain", V300));

        var feature = Feature(status, "310-plain");
        feature.SetupStatus.ShouldBe(FeatureSetupStatus.NoSetupNeeded);
        feature.SetupItems.ShouldBeEmpty();
        feature.ModuleName.ShouldBe("Workflow");
    }

    [Fact]
    public async Task Should_Say_A_Feature_Needs_Setup_Until_It_Is_Done()
    {
        var tenantId = CreateTenantUpdatedFrom("currency", V300);

        var before = Feature(await StatusOf(tenantId), "310-currency");
        AddCurrency(tenantId);
        var after = Feature(await StatusOf(tenantId), "310-currency");

        before.SetupStatus.ShouldBe(FeatureSetupStatus.NeedsSetup);
        before.SetupItems.Single().Outcome.ShouldBe(SetupCheckOutcome.Missing);
        before.SetupItems.Single().FixScreenRoute.ShouldBe("/app/admin/currencies");
        after.SetupStatus.ShouldBe(FeatureSetupStatus.Done);
    }

    [Fact]
    public async Task Should_Not_Pass_A_Feature_Whose_Check_Failed_Or_Is_Missing()
    {
        var status = await StatusOf(CreateTenantUpdatedFrom("broken", V300));

        var broken = Feature(status, "310-broken");
        broken.SetupStatus.ShouldBe(FeatureSetupStatus.CheckFailed);
        broken.SetupItems.Single().Message.ShouldBe("boom");
        Feature(status, "310-unregistered").SetupStatus.ShouldBe(FeatureSetupStatus.CheckFailed);
    }

    [Fact]
    public async Task Should_Check_Only_The_Signed_In_Tenant()
    {
        var withCurrency = CreateTenantUpdatedFrom("tenanta", V310);
        AddCurrency(withCurrency);
        var without = CreateTenantUpdatedFrom("tenantb", V310);

        Feature(await StatusOf(without), "320-currency").SetupStatus.ShouldBe(FeatureSetupStatus.NeedsSetup);
        Feature(await StatusOf(withCurrency), "320-currency").SetupStatus.ShouldBe(FeatureSetupStatus.Done);
    }

    // ---------------------------------------------------------------- looking up one release

    [Fact]
    public async Task Should_Describe_Any_Release_With_This_Tenants_Setup_Status()
    {
        LoginAsAdminOf(CreateEmptyTenant("anyrelease"));

        var release = await _appService.GetRelease(V310);

        release.Version.ShouldBe(V310);
        release.Features.Count.ShouldBe(4);
        release.Features.Single(f => f.Key == "310-currency").SetupStatus.ShouldBe(FeatureSetupStatus.NeedsSetup);
    }

    [Fact]
    public async Task Should_Refuse_A_Release_That_Does_Not_Exist()
    {
        LoginAsAdminOf(CreateEmptyTenant("unknown"));

        await Should.ThrowAsync<UserFriendlyException>(() => _appService.GetRelease("2.9.0"));
    }

    // ---------------------------------------------------------------- who may use it

    [Fact]
    public async Task Should_Not_Be_Available_To_The_Host()
    {
        LoginAsHostAdmin();

        await Should.ThrowAsync<AbpAuthorizationException>(() => _appService.GetUpdateStatus());
    }

    [Fact]
    public async Task Should_Not_Be_Available_To_Tenant_Users_Without_The_Permission()
    {
        LoginAsAdminOf(CreateEmptyTenant("notforyou"), withPermission: false);

        await Should.ThrowAsync<AbpAuthorizationException>(() => _appService.GetUpdateStatus());
    }

    // ---------------------------------------------------------------- export

    [Fact]
    public async Task Should_Export_What_The_Update_Brought_To_Excel()
    {
        LoginAsAdminOf(CreateTenantUpdatedFrom("exportupdate", V300));

        var file = await _appService.ExportToExcel(new ExportReleaseChangesInput());

        file.FileName.ShouldEndWith(".xlsx");
        Resolve<ITempFileCacheManager>().GetFile(file.FileToken).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Should_Export_One_Release_To_Excel()
    {
        LoginAsAdminOf(CreateEmptyTenant("exportone"));

        var file = await _appService.ExportToExcel(new ExportReleaseChangesInput { Version = V310 });

        file.FileName.ShouldContain(V310);
        Resolve<ITempFileCacheManager>().GetFile(file.FileToken).ShouldNotBeEmpty();
    }
}
