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
/// Tests for the Payroll JV (Custom 016) "Detail" grid report (SuperAdmin-only, legacy line 250-256;
/// <c>uscRptPayrollJV_Custom016.ascx</c>, <c>Table1()</c>, BMC column set, <c>IsDetail == true</c> --
/// lines 573-1168 common preprocessing + 1176-1445 shared summary + 1527-1572 detail rows).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom016DetailReportAppService_Tests : AppTestBase
{
    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private readonly IPayrollJVCustom016DetailReportAppService _appService;
    private readonly PayrollJVReportTestDataBuilder _data;
    private PayrollArea _bmcArea;

    public PayrollJVCustom016DetailReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom016DetailReportAppService>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private static GetPayrollJVCustom016MainFormatReportInput Input(
        int month = 6, int year = 2024, int maxResultCount = 100, int skipCount = 0)
    {
        return new GetPayrollJVCustom016MainFormatReportInput
        {
            Month = month,
            Year = year,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    private Employee ArrangeLine(
        string employeeCode, string costCenterCode, string wageTypeCode, double amount,
        BillingType billingType, string accountCode, string offsetAccountCode)
    {
        var bmcArea = _bmcArea ??= _data.CreatePayrollArea("BMC01", "BMC");
        var costCenter = _data.CreateCostCenter(costCenterCode);
        var employee = _data.CreateEmployee(employeeCode);
        _data.CreateOrgAssignment(employee, bmcArea, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), costCenter);

        var wageType = _data.CreateWageType(wageTypeCode, billingType);
        var account = _data.CreateAccount(accountCode, "Account " + accountCode);
        var offsetAccount = _data.CreateAccount(offsetAccountCode, "Offset " + offsetAccountCode);
        _data.CreateWageAccount(employee, wageType, account, offsetAccount, costPercentage: 100);
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, amount);

        return employee;
    }

    /// <summary>
    /// Legacy lines 1527-1572: unlike Main Format, "Detail" does NOT group by (CostCenter, Account,
    /// OffsetAccount) -- two employees on the same account each get their own row, and EmpCode/WageType/
    /// CostCenter are populated (blank on Main Format).
    /// </summary>
    [Fact]
    public async Task Should_Produce_One_Row_Per_Employee_Instead_Of_Grouping()
    {
        var empA = ArrangeLine("JV16DET-A", "1100", "2000", 100, BillingType.Earning, "95001", "95002");
        var empB = ArrangeLine("JV16DET-B", "1100", "2000", 150, BillingType.Earning, "95001", "95002");

        var result = await _appService.GetPayrollJVCustom016DetailReport(Input());

        result.Items.Count(r => r.Account.Contains("95001")).ShouldBe(2);
        var rowA = result.Items.Single(r => r.EmployeeCode == empA.Code);
        rowA.WageTypeName.ShouldBe("WT 2000");
        rowA.CostCenterCode.ShouldBe("1100");
        rowA.Amount.ShouldBe(100);
        var rowB = result.Items.Single(r => r.EmployeeCode == empB.Code);
        rowB.Amount.ShouldBe(150);
    }

    /// <summary>Legacy line 1531: only Net Pay (9000) is excluded from the detail rows.</summary>
    [Fact]
    public async Task Should_Exclude_Only_Net_Pay_From_Detail_Rows()
    {
        ArrangeLine("JV16DET-NP", "1200", "9000", 500, BillingType.Earning, "95003", "95004");

        var result = await _appService.GetPayrollJVCustom016DetailReport(Input());

        result.Items.ShouldNotContain(r => r.EmployeeCode == "JV16DET-NP" && r.Amount == 500);
    }

    [Fact]
    public async Task Should_Be_Empty_When_No_Payroll_Area_Is_Named_Bmc()
    {
        var result = await _appService.GetPayrollJVCustom016DetailReport(Input());

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_To_Excel_With_The_Full_Filtered_Set_Not_Just_One_Page()
    {
        for (var i = 0; i < 3; i++)
        {
            ArrangeLine("JV16DET-EXP-" + i, "1300", "2000", 100 + i, BillingType.Earning, "9600" + i, "9700" + i);
        }

        var file = await _appService.GetPayrollJVCustom016DetailReportToExcel(Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom016Detail.xlsx");
        file.FileType.ShouldNotBeNullOrEmpty();
    }
}
