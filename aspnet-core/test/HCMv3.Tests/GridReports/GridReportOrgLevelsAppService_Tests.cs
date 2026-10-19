using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports;
using HCMv3.OrganizationManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the OrgLevel1..OrgLevel7 column captions (port of legacy
/// <c>OrgService.GetOrgUnitTypeColName</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class GridReportOrgLevelsAppService_Tests : AppTestBase
{
    private readonly IGridReportOrgLevelsAppService _appService;

    public GridReportOrgLevelsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IGridReportOrgLevelsAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private void CreateUnitType(string code, string nameEn, string nameAr, int? tenantId = null)
    {
        UsingDbContext(context =>
        {
            context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
            {
                TenantId = tenantId ?? TenantId,
                Code = code,
                NameEn = nameEn,
                NameAr = nameAr
            });
        });
    }

    private static async Task<T> WithUiCulture<T>(string cultureName, System.Func<Task<T>> action)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
        try
        {
            return await action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public async Task Should_Return_Only_The_Levels_The_Tenant_Has_Defined()
    {
        CreateUnitType(OrganizationUnitType.Level1Code, "Company", "الشركة");
        CreateUnitType(OrganizationUnitType.Level3Code, "Department", "الإدارة");

        var result = await WithUiCulture("en", () => _appService.GetOrgLevelCaptions());

        result.Items.Select(i => i.Level).ShouldBe(new[] { 1, 3 });
        result.Items.Select(i => i.Code).ShouldBe(new[] { "Level1", "Level3" });
    }

    [Fact]
    public async Task Should_Leave_Out_Level0_And_Levels_Above_7()
    {
        CreateUnitType(OrganizationUnitType.Level0Code, "Group", "المجموعة");
        CreateUnitType(OrganizationUnitType.Level2Code, "Division", "القطاع");
        CreateUnitType(OrganizationUnitType.Level8Code, "Team", "الفريق");
        CreateUnitType(OrganizationUnitType.Level9Code, "Unit", "الوحدة");

        var result = await WithUiCulture("en", () => _appService.GetOrgLevelCaptions());

        result.Items.Count.ShouldBe(1);
        result.Items[0].Level.ShouldBe(2);
        result.Items[0].Caption.ShouldBe("Division");
    }

    [Fact]
    public async Task Should_Return_English_Names_For_An_English_Request()
    {
        CreateUnitType(OrganizationUnitType.Level2Code, "Division", "القطاع");

        var result = await WithUiCulture("en", () => _appService.GetOrgLevelCaptions());

        result.Items.Single().Caption.ShouldBe("Division");
    }

    [Fact]
    public async Task Should_Return_Arabic_Names_For_An_Arabic_Request()
    {
        CreateUnitType(OrganizationUnitType.Level2Code, "Division", "القطاع");

        var result = await WithUiCulture("ar", () => _appService.GetOrgLevelCaptions());

        result.Items.Single().Caption.ShouldBe("القطاع");
    }

    [Fact]
    public async Task Should_Return_Levels_In_Level_Order_Regardless_Of_Creation_Order()
    {
        CreateUnitType(OrganizationUnitType.Level7Code, "Section", "القسم");
        CreateUnitType(OrganizationUnitType.Level4Code, "Branch", "الفرع");
        CreateUnitType(OrganizationUnitType.Level1Code, "Company", "الشركة");

        var result = await WithUiCulture("en", () => _appService.GetOrgLevelCaptions());

        result.Items.Select(i => i.Level).ShouldBe(new[] { 1, 4, 7 });
        result.Items.Select(i => i.Caption).ShouldBe(new[] { "Company", "Branch", "Section" });
    }

    [Fact]
    public async Task Should_Not_Return_Another_Tenants_Levels()
    {
        // Host-side tenant id that is not the signed-in default tenant.
        CreateUnitType(OrganizationUnitType.Level5Code, "Other Tenant Level", "مستأجر آخر", tenantId: TenantId + 1000);

        var result = await WithUiCulture("en", () => _appService.GetOrgLevelCaptions());

        result.Items.ShouldNotContain(i => i.Level == 5);
    }

    [Fact]
    public async Task Should_Return_Nothing_When_No_Level_Is_Defined()
    {
        var result = await WithUiCulture("en", () => _appService.GetOrgLevelCaptions());

        result.Items.ShouldBeEmpty();
    }
}
