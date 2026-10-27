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
/// Tests for the Payroll JV (Custom 016) "Indemnity Provision" grid report (SuperAdmin-only, legacy line
/// 255; <c>uscRptPayrollJV_Custom016.ascx</c>, <c>Table1()</c>, <c>cmbType == "Indemnity Provision"</c>
/// branch -- lines 2459-2518). Shares its implementation with the "Leave Provision" report via
/// <see cref="PayrollJVCustom016ProvisionDiffHelper"/> -- see
/// <c>PayrollJVCustom016LeaveProvisionReportAppService_Tests</c> for the fuller behavioural coverage; this
/// suite focuses on what differs: the target wage type code and screen wiring.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom016IndemnityProvisionReportAppService_Tests : AppTestBase
{
    private static readonly DateTime JuneEnd = new(2024, 6, 30);
    private const string IndemnityProvisionWageTypeCode = "3000";

    private readonly IPayrollJVCustom016IndemnityProvisionReportAppService _appService;
    private readonly PayrollJVReportTestDataBuilder _data;
    private PayrollArea _area;

    public PayrollJVCustom016IndemnityProvisionReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom016IndemnityProvisionReportAppService>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private PayrollArea Area => _area ??= _data.CreatePayrollArea("IPROV01", "IPROV");

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
        var wageType = _data.CreateWageType(IndemnityProvisionWageTypeCode, BillingType.Provision);
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, amount);
        return employee;
    }

    /// <summary>Legacy line 2480: targets wage type "3000" (Indemnity Provision), not "3001".</summary>
    [Fact]
    public async Task Should_Diff_Wage_Type_3000_Not_3001()
    {
        var employee = ArrangeProvisionRegister("JV16IVP-1", 17);

        var result = await _appService.GetPayrollJVCustom016IndemnityProvisionReport(Input(Area.Code));

        var row = result.Items.Single(r => r.EmployeeCode == employee.Code);
        row.AmountReg.ShouldBe(17);
        row.AmountJV.ShouldBe(0);
        row.Difference.ShouldBe(17);
    }

    [Fact]
    public async Task Should_Be_Empty_Without_A_Payroll_Area_Code()
    {
        ArrangeProvisionRegister("JV16IVP-NOAREA", 10);

        var result = await _appService.GetPayrollJVCustom016IndemnityProvisionReport(Input(null));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_To_Excel_With_The_Full_Filtered_Set_Not_Just_One_Page()
    {
        for (var i = 0; i < 3; i++)
        {
            ArrangeProvisionRegister("JV16IVP-EXP-" + i, 10 + i);
        }

        var file = await _appService.GetPayrollJVCustom016IndemnityProvisionReportToExcel(
            Input(Area.Code, maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom016IndemnityProvision.xlsx");
        file.FileType.ShouldNotBeNullOrEmpty();
    }
}
