using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The data HR's leave form loads before anything is entered: the Leave Type drop-down (legacy
/// <c>EmployeeAbsence.EmpAbsenceType</c>, 73-127) and the settings that show or hide fields (legacy controller
/// 58-150).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeAbsencesAppService_FormData_Tests : EmployeeAbsencesAppServiceTestBase
{
    [Fact]
    public async Task GetLeaveTypesForEmployee_Should_Leave_Out_Inactive_Types()
    {
        // Arrange
        var employeeId = CreateEmployee("EFD01");
        var active = CreateAbsenceType("EFDA1");
        var inactive = CreateAbsenceType("EFDI1", t => t.IsActive = false);

        // Act
        var result = await Service.GetLeaveTypesForEmployee(new EntityDto(employeeId));

        // Assert
        var ids = result.Items.Select(i => i.Id).ToList();
        ids.ShouldContain(active);
        ids.ShouldNotContain(inactive);
    }

    [Fact]
    public async Task GetLeaveTypesForEmployee_Should_Leave_Out_Types_For_The_Other_Gender()
    {
        // Arrange
        var employeeId = CreateEmployee("EFD02", gender: Gender.Male);
        var forMen = CreateAbsenceType("EFDM2", t => t.LeaveGender = LeaveGender.Male);
        var forWomen = CreateAbsenceType("EFDW2", t => t.LeaveGender = LeaveGender.Female);

        // Act
        var result = await Service.GetLeaveTypesForEmployee(new EntityDto(employeeId));

        // Assert
        var ids = result.Items.Select(i => i.Id).ToList();
        ids.ShouldContain(forMen);
        ids.ShouldNotContain(forWomen);
    }

    [Fact]
    public async Task GetLeaveTypesForEmployee_Should_Leave_Out_Salary_Profile_Types_Not_On_The_Employees_Profile()
    {
        // Arrange
        var employeeId = CreateEmployee("EFD03");
        var onProfile = CreateAbsenceType("EFDP3", t => t.IncludeInSalaryProfile = true);
        var offProfile = CreateAbsenceType("EFDO3", t => t.IncludeInSalaryProfile = true);
        var general = CreateAbsenceType("EFDG3");
        GiveSalaryProfile(employeeId, new[] { onProfile });

        // Act
        var result = await Service.GetLeaveTypesForEmployee(new EntityDto(employeeId));

        // Assert
        var ids = result.Items.Select(i => i.Id).ToList();
        ids.ShouldContain(onProfile);
        ids.ShouldContain(general);
        ids.ShouldNotContain(offProfile);
    }

    [Fact]
    public async Task GetLeaveTypesForEmployee_Should_Not_Offer_Partial_Leave_On_A_Full_Day_Only_Type()
    {
        // Arrange
        var employeeId = CreateEmployee("EFD04");
        var fullDayOnly = CreateAbsenceType("EFDF4", t =>
        {
            t.AllowPartialLeave = true;
            t.LeaveDurationType = LeaveDurationType.FullDay;
        });

        // Act
        var result = await Service.GetLeaveTypesForEmployee(new EntityDto(employeeId));

        // Assert
        result.Items.Single(i => i.Id == fullDayOnly).AllowPartialLeave.ShouldBeFalse();
    }

    [Fact]
    public async Task GetLeaveTypesForEmployee_Should_Return_Nothing_For_An_Unknown_Employee()
    {
        // Arrange
        CreateAbsenceType("EFDU5");

        // Act
        var result = await Service.GetLeaveTypesForEmployee(new EntityDto(987654));

        // Assert
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLeaveFormSettings_Should_Return_The_Time_And_Payroll_Settings()
    {
        // Arrange
        SetTimeSettings(s =>
        {
            s.EnableAutoHalfDayLeave = true;
            s.EnablePayFullWithPayroll = false;
            s.EnableSettleFractionalLeaveBalance = true;
        });
        SetPayrollSettings(s => s.DefaultLeavePaymentType = LeavePaymentType.PayInAdvance);

        // Act
        var result = await Service.GetLeaveFormSettings();

        // Assert
        result.EnableAutoHalfDayLeave.ShouldBeTrue();
        result.EnablePayFullWithPayroll.ShouldBeFalse();
        result.EnableSettleFractionalLeaveBalance.ShouldBeTrue();
        result.DefaultLeavePaymentType.ShouldBe(LeavePaymentType.PayInAdvance);
    }
}
