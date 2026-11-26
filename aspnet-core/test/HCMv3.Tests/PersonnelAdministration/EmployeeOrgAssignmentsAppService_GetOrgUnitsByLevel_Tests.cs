using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Legacy OrgLevel1..OrgLevel7 pickers on the org assignment screen: each unit comes with the level of its
/// Organization Unit Type and its parent, so a level can be filtered by the one above.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeOrgAssignmentsAppService_GetOrgUnitsByLevel_Tests : AppTestBase
{
    private readonly IEmployeeOrgAssignmentsAppService _appService;

    public EmployeeOrgAssignmentsAppService_GetOrgUnitsByLevel_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeOrgAssignmentsAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateUnitType(string code)
    {
        OrganizationUnitType type = null;
        UsingDbContext(context => type = context.Set<OrganizationUnitType>().Add(new OrganizationUnitType
        {
            TenantId = TenantId, Code = code, NameEn = code, NameAr = code
        }).Entity);
        return type.Id;
    }

    private long CreateUnit(string name, int unitTypeId, long? parentId = null)
    {
        OrganizationUnit unit = null;
        UsingDbContext(context =>
        {
            unit = context.OrganizationUnits.Add(new OrganizationUnit(TenantId, name, parentId)
            {
                Code = Guid.NewGuid().ToString("N")[..5]
            }).Entity;
            context.SaveChanges();
            context.Set<OrgUnitProfile>().Add(new OrgUnitProfile
            {
                TenantId = TenantId, OrganizationUnitId = unit.Id, Code = name, NameAr = name, OrganizationUnitTypeId = unitTypeId
            });
        });
        return unit.Id;
    }

    [Fact]
    public async Task Should_Return_Each_Unit_With_Its_Level_From_The_Unit_Type_Code()
    {
        // Arrange
        var level1Type = CreateUnitType(OrganizationUnitType.Level1Code);
        var level3Type = CreateUnitType(OrganizationUnitType.Level3Code);
        var company = CreateUnit("OL-Company", level1Type);
        var section = CreateUnit("OL-Section", level3Type, company);

        // Act
        var result = await _appService.GetOrgUnitsByLevel();

        // Assert
        result.Items.Single(u => u.Id == company).Level.ShouldBe(1);
        result.Items.Single(u => u.Id == section).Level.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Return_The_Parent_And_Display_Name_Of_Each_Unit()
    {
        // Arrange
        var level1Type = CreateUnitType(OrganizationUnitType.Level1Code);
        var level2Type = CreateUnitType(OrganizationUnitType.Level2Code);
        var company = CreateUnit("OL-Parent", level1Type);
        var division = CreateUnit("OL-Child", level2Type, company);

        // Act
        var result = await _appService.GetOrgUnitsByLevel();

        // Assert
        var child = result.Items.Single(u => u.Id == division);
        child.ParentId.ShouldBe(company);
        child.DisplayName.ShouldBe("OL-Child");
    }

    [Fact]
    public async Task Should_Leave_Out_Units_Whose_Type_Is_Not_A_Level()
    {
        // Arrange
        var rootType = CreateUnitType(OrganizationUnitType.Level0Code);
        var root = CreateUnit("OL-Root", rootType);

        // Act
        var result = await _appService.GetOrgUnitsByLevel();

        // Assert
        result.Items.ShouldNotContain(u => u.Id == root);
    }
}
