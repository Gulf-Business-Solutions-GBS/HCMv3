using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Configuration;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Authorization.Roles;
using HCMv3.Configuration;
using HCMv3.SetupChecks;
using HCMv3.SetupChecks.Dto;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SetupChecks;

// ReSharper disable once InconsistentNaming
public class TenantVersionCheckAppService_Tests : TenantVersionCheckTestBase
{
    private const string SetupCheckPermission = AppPermissions.Pages_Administration_TenantVersionCheck;

    private readonly ITenantVersionCheckAppService _appService;

    public TenantVersionCheckAppService_Tests()
    {
        _appService = Resolve<ITenantVersionCheckAppService>();
    }

    /// <summary>A tenant last seen on <paramref name="version"/>, i.e. updated since then to 3.0.5.</summary>
    private int CreateTenantUpdatedFrom(string tenancyName, string version)
    {
        var tenantId = CreateEmptyTenant(tenancyName);
        Resolve<ISettingManager>().ChangeSettingForTenant(tenantId, AppSettings.Releases.LastSeenVersion, version);
        return tenantId;
    }

    private async Task<BuildComparisonDto> CompareFrom(string version, int? tenantId = null)
    {
        LoginAsAdminOf(tenantId ?? CreateEmptyTenant("t" + System.Guid.NewGuid().ToString("N")[..8]));
        return await _appService.Compare(new CompareBuildsInput { FromVersion = version });
    }

    private static SetupItemDto Item(BuildComparisonDto comparison, SetupItemKind kind, string technicalName) =>
        comparison.SetupItems.Single(i => i.Kind == kind && i.TechnicalName == technicalName);

    // ---------------------------------------------------------------- versions

    [Fact]
    public async Task Should_Show_The_Installed_Version_And_No_Previous_When_None_Was_Recorded()
    {
        LoginAsAdminOf(CreateEmptyTenant("fresh"));

        var status = await _appService.GetStatus();

        status.CurrentVersion.ShouldBe("3.0.5");
        status.PreviousVersion.ShouldBeNull();
        status.CanComparePrevious.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Show_The_Previous_Version_After_An_Update()
    {
        LoginAsAdminOf(CreateTenantUpdatedFrom("updated", "3.0.3"));

        var status = await _appService.GetStatus();

        status.CurrentVersion.ShouldBe("3.0.5");
        status.PreviousVersion.ShouldBe("3.0.3");
        status.CanComparePrevious.ShouldBeTrue();
        status.UpdatedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Not_Offer_To_Compare_With_A_Previous_Version_It_Cannot_Place()
    {
        LoginAsAdminOf(CreateTenantUpdatedFrom("foreign", "15.2.0"));

        var status = await _appService.GetStatus();

        status.PreviousVersion.ShouldBe("15.2.0");
        status.CanComparePrevious.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Offer_Every_Earlier_Build_Newest_First()
    {
        LoginAsAdminOf(CreateEmptyTenant("builds"));

        var status = await _appService.GetStatus();

        status.Builds.Select(b => b.Version).ShouldBe(new[] { "3.0.4", "3.0.3", "3.0.2", "3.0.1" });
        status.Builds[0].Subject.ShouldBe("A default workflow chain");
    }

    // ---------------------------------------------------------------- what changed

    [Fact]
    public async Task Should_List_Every_Change_After_The_Older_Version_Newest_First()
    {
        var comparison = await CompareFrom("3.0.3");

        comparison.FromVersion.ShouldBe("3.0.3");
        comparison.ToVersion.ShouldBe("3.0.5");
        comparison.Changes.Select(c => c.Version).ShouldBe(new[] { "3.0.5", "3.0.4" });
        comparison.Changes[0].Subject.ShouldBe("Add geo punch");
        comparison.Changes[0].SetupItemCount.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Name_Each_Changes_Module_In_Words()
    {
        var comparison = await CompareFrom("3.0.2");

        comparison.Changes.Single(c => c.Version == "3.0.4").ModuleName.ShouldBe("Zz Unnamed Module");
        comparison.Changes.Single(c => c.Version == "3.0.3").ModuleName.ShouldBe("General");
    }

    [Fact]
    public async Task Should_Find_Nothing_Changed_Since_The_Installed_Version()
    {
        var comparison = await CompareFrom("3.0.5");

        comparison.Changes.ShouldBeEmpty();
        comparison.SetupItems.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Compare_From_The_Start_For_A_Version_Recorded_Before_Build_Numbers()
    {
        var comparison = await CompareFrom("3.0.0");

        comparison.Changes.Count.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Refuse_A_Version_That_Is_Not_One_Of_Ours()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => CompareFrom("15.2.0"));
    }

    // ---------------------------------------------------------------- setup items

    [Fact]
    public async Task Should_Ask_For_A_New_Permission_To_Be_Granted_When_No_Role_Has_It()
    {
        var item = Item(await CompareFrom("3.0.4"), SetupItemKind.Permission, SetupCheckPermission);

        item.Status.ShouldBe(SetupItemStatus.Review);
        item.Title.ShouldBe("Tenant Version Check");
        item.Version.ShouldBe("3.0.5");
        item.ChangeSubject.ShouldBe("Add geo punch");
        item.FixScreenRoute.ShouldBe("/app/admin/roles");
    }

    [Fact]
    public async Task Should_Still_Ask_When_Only_The_Administrator_Role_Has_The_Permission()
    {
        var tenantId = CreateEmptyTenant("onlyadmin");
        CreateRole(tenantId, StaticRoleNames.Tenants.Admin);
        CreateRole(tenantId, "HR");

        var item = Item(await CompareFrom("3.0.4", tenantId), SetupItemKind.Permission, SetupCheckPermission);

        item.Status.ShouldBe(SetupItemStatus.Review);
        item.Detail.ShouldContain(StaticRoleNames.Tenants.Admin);
        item.Detail.ShouldNotContain("HR");
    }

    [Fact]
    public async Task Should_Call_A_Permission_Done_Once_Another_Role_Has_It()
    {
        var tenantId = CreateEmptyTenant("granted");
        CreateRole(tenantId, StaticRoleNames.Tenants.Admin);
        CreateRole(tenantId, "HR", SetupCheckPermission);

        var item = Item(await CompareFrom("3.0.4", tenantId), SetupItemKind.Permission, SetupCheckPermission);

        item.Status.ShouldBe(SetupItemStatus.Done);
        item.Detail.ShouldContain("HR");
    }

    [Fact]
    public async Task Should_List_A_Thing_Once_By_The_Latest_Change_That_Touched_It()
    {
        var comparison = await CompareFrom("3.0.1");

        Item(comparison, SetupItemKind.Permission, SetupCheckPermission).Version.ShouldBe("3.0.5");
    }

    [Fact]
    public async Task Should_Leave_Out_What_Is_Not_A_Tenants_Business_Or_No_Longer_Exists()
    {
        var comparison = await CompareFrom("3.0.1");

        comparison.SetupItems.ShouldNotContain(i => i.TechnicalName == "Pages.Tenants");
        comparison.SetupItems.ShouldNotContain(i => i.TechnicalName == "SZZ999");
    }

    [Fact]
    public async Task Should_Point_To_A_New_Screen()
    {
        var item = Item(await CompareFrom("3.0.4"), SetupItemKind.Screen, "SGM015");

        item.Status.ShouldBe(SetupItemStatus.Info);
        item.FixScreenRoute.ShouldBe("/app/admin/currencies");
    }

    [Fact]
    public async Task Should_Point_To_A_New_Screen_That_Needs_No_Permission()
    {
        var item = Item(await CompareFrom("3.0.2"), SetupItemKind.Screen, "SGEN012");

        item.Status.ShouldBe(SetupItemStatus.Info);
        item.FixScreenRoute.ShouldBe("/app/notifications");
    }

    [Fact]
    public async Task Should_Mention_A_New_Tenant_Setting()
    {
        var item = Item(await CompareFrom("3.0.3"), SetupItemKind.Setting, AppSettings.Releases.LastSeenVersion);

        item.Status.ShouldBe(SetupItemStatus.Info);
    }

    [Fact]
    public async Task Should_Carry_A_Setup_Step_Written_By_Hand()
    {
        var item = Item(await CompareFrom("3.0.3"), SetupItemKind.Manual, "Create a default workflow chain");

        item.Status.ShouldBe(SetupItemStatus.Review);
        item.Title.ShouldBe("Create a default workflow chain");
        item.Version.ShouldBe("3.0.4");
    }

    [Fact]
    public async Task Should_Demand_Action_For_A_Database_Change_That_Was_Not_Applied()
    {
        var comparison = await CompareFrom("3.0.3");

        var missing = Item(comparison, SetupItemKind.Database, MissingMigration);
        missing.Status.ShouldBe(SetupItemStatus.ActionRequired);
        missing.Title.ShouldBe("Added Geo Punch");
        Item(comparison, SetupItemKind.Database, AppliedMigration).Status.ShouldBe(SetupItemStatus.Done);
    }

    [Fact]
    public async Task Should_Only_Inform_About_Database_Changes_When_The_History_Cannot_Be_Read()
    {
        Migrations.Applied = null;

        var comparison = await CompareFrom("3.0.3");

        Item(comparison, SetupItemKind.Database, MissingMigration).Status.ShouldBe(SetupItemStatus.Info);
    }

    [Fact]
    public async Task Should_Put_The_Most_Urgent_Setup_Items_First()
    {
        var statuses = (await CompareFrom("3.0.0")).SetupItems.Select(i => i.Status).ToList();

        statuses.First().ShouldBe(SetupItemStatus.ActionRequired);
        statuses.Last().ShouldBe(SetupItemStatus.Done);
        statuses.ShouldBe(statuses.OrderBy(s => s switch
        {
            SetupItemStatus.ActionRequired => 0,
            SetupItemStatus.Review => 1,
            SetupItemStatus.Info => 2,
            _ => 3,
        }).ToList());
    }

    // ---------------------------------------------------------------- who may use it

    [Fact]
    public async Task Should_Not_Be_Available_To_The_Host()
    {
        LoginAsHostAdmin();

        await Should.ThrowAsync<AbpAuthorizationException>(() => _appService.GetStatus());
    }

    [Fact]
    public async Task Should_Not_Be_Available_To_Tenant_Users_Without_The_Permission()
    {
        LoginAsAdminOf(CreateEmptyTenant("notforyou"), withPermission: false);

        await Should.ThrowAsync<AbpAuthorizationException>(() => _appService.GetStatus());
    }

    // ---------------------------------------------------------------- export

    [Fact]
    public async Task Should_Export_The_Comparison_To_Excel()
    {
        LoginAsAdminOf(CreateEmptyTenant("export"));

        var file = await _appService.ExportToExcel(new CompareBuildsInput { FromVersion = "3.0.1" });

        file.FileName.ShouldBe("Tenant Version Check 3.0.1 - 3.0.5.xlsx");
        Resolve<ITempFileCacheManager>().GetFile(file.FileToken).ShouldNotBeEmpty();
    }
}
