using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Payroll JV (Custom 016) "Leave Provision" grid report (SuperAdmin-only, legacy line 254;
/// <c>uscRptPayrollJV_Custom016.ascx</c>, <c>Table1()</c>, <c>cmbType == "Leave Provision"</c> branch --
/// lines 2397-2456).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom016LeaveProvisionReportAppService_Tests : AppTestBase
{
    private static readonly DateTime JuneEnd = new(2024, 6, 30);
    private const string LeaveProvisionWageTypeCode = "3001";

    private readonly IPayrollJVCustom016LeaveProvisionReportAppService _appService;
    private readonly PayrollJVReportTestDataBuilder _data;
    private PayrollArea _area;

    public PayrollJVCustom016LeaveProvisionReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom016LeaveProvisionReportAppService>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private PayrollArea Area => _area ??= _data.CreatePayrollArea("PROV01", "PROV");

    private static GetPayrollJVCustom016ProvisionDiffReportInput Input(
        string payrollAreaCode, int month = 6, int year = 2024, int maxResultCount = 100, int skipCount = 0)
    {
        return new GetPayrollJVCustom016ProvisionDiffReportInput
        {
            PayrollAreaCode = payrollAreaCode,
            Month = month,
            Year = year,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    private Employee ArrangeProvisionRegister(string employeeCode, double amount)
    {
        var employee = _data.CreateEmployee(employeeCode);
        _data.CreateOrgAssignment(employee, Area, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        var wageType = _data.CreateWageType(LeaveProvisionWageTypeCode, BillingType.Provision);
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, amount);
        return employee;
    }

    /// <summary>
    /// Legacy lines 2439-2451: an employee with a non-zero Leave Provision register line who has NO
    /// matching wage type "3001" line in the JV posting output is listed, AmountJV is always 0 by
    /// construction, and Difference always equals AmountReg.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_AmountJv_Always_Zero_Behavior()
    {
        var employee = ArrangeProvisionRegister("JV16LVP-1", 42.5);

        var result = await _appService.GetPayrollJVCustom016LeaveProvisionReport(Input(Area.Code));

        var row = result.Items.Single(r => r.EmployeeCode == employee.Code);
        row.AmountReg.ShouldBe(42.5);
        row.AmountJV.ShouldBe(0);
        row.Difference.ShouldBe(42.5);
    }

    /// <summary>Legacy line 2427: a zero-amount provision line never enters <c>dsProv</c> at all.</summary>
    [Fact]
    public async Task Should_Exclude_Zero_Amount_Lines()
    {
        ArrangeProvisionRegister("JV16LVP-ZERO", 0);

        var result = await _appService.GetPayrollJVCustom016LeaveProvisionReport(Input(Area.Code));

        result.TotalCount.ShouldBe(0);
    }

    /// <summary>Legacy line 595 (outer gate): no payroll area means no output at all.</summary>
    [Fact]
    public async Task Should_Be_Empty_Without_A_Payroll_Area_Code()
    {
        ArrangeProvisionRegister("JV16LVP-NOAREA", 10);

        var result = await _appService.GetPayrollJVCustom016LeaveProvisionReport(Input(null));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_To_Excel_With_The_Full_Filtered_Set_Not_Just_One_Page()
    {
        for (var i = 0; i < 3; i++)
        {
            ArrangeProvisionRegister("JV16LVP-EXP-" + i, 10 + i);
        }

        var file = await _appService.GetPayrollJVCustom016LeaveProvisionReportToExcel(
            Input(Area.Code, maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom016LeaveProvision.xlsx");
        file.FileType.ShouldNotBeNullOrEmpty();
    }
}
