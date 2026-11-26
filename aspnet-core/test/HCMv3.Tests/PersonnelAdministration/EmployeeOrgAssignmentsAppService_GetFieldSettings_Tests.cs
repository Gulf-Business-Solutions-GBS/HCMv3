using System.Linq;
using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// Legacy OrgUnitCustomizeController: the org assignment screen shows Position, Posting Group and Budgeting Group
/// only when <see cref="ManageOrgSettings"/> switches them on.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeOrgAssignmentsAppService_GetFieldSettings_Tests : AppTestBase
{
    private readonly IEmployeeOrgAssignmentsAppService _appService;

    public EmployeeOrgAssignmentsAppService_GetFieldSettings_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeOrgAssignmentsAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private void SetOrgSettings(bool position, bool postingGroup, bool budgetingGroup)
    {
        UsingDbContext(context =>
        {
            var org = context.Set<ManageOrgSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                      ?? context.Set<ManageOrgSettings>().Add(new ManageOrgSettings { TenantId = TenantId }).Entity;
            org.EnablePosition = position;
            org.EnablePostingGroup = postingGroup;
            org.EnableBudgetingGroup = budgetingGroup;
        });
    }

    [Fact]
    public async Task Should_Return_All_Fields_Enabled_When_Settings_Switch_Them_On()
    {
        // Arrange
        SetOrgSettings(position: true, postingGroup: true, budgetingGroup: true);

        // Act
        var result = await _appService.GetFieldSettings();

        // Assert
        result.EnablePosition.ShouldBeTrue();
        result.EnablePostingGroup.ShouldBeTrue();
        result.EnableBudgetingGroup.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Return_All_Fields_Disabled_When_Settings_Switch_Them_Off()
    {
        // Arrange
        SetOrgSettings(position: false, postingGroup: false, budgetingGroup: false);

        // Act
        var result = await _appService.GetFieldSettings();

        // Assert
        result.EnablePosition.ShouldBeFalse();
        result.EnablePostingGroup.ShouldBeFalse();
        result.EnableBudgetingGroup.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Report_Each_Field_Independently()
    {
        // Arrange
        SetOrgSettings(position: false, postingGroup: true, budgetingGroup: false);

        // Act
        var result = await _appService.GetFieldSettings();

        // Assert
        result.EnablePosition.ShouldBeFalse();
        result.EnablePostingGroup.ShouldBeTrue();
        result.EnableBudgetingGroup.ShouldBeFalse();
    }
}
