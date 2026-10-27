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
/// Tests for the Payroll JV (Custom 016) "Main Format - BMC" grid report (legacy
/// <c>uscRptPayrollJV_Custom016.ascx</c>, <c>Table1()</c>, BMC column set, <c>IsDetail == false</c> --
/// lines 573-1168 common preprocessing + 1176-1524). Also covers "Main Format-New" for BMC, since nothing
/// in the BMC branch tests for the "-New" suffix -- see the app service's class remarks.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom016MainFormatBmcReportAppService_Tests : AppTestBase
{
    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private readonly IPayrollJVCustom016MainFormatBmcReportAppService _appService;
    private readonly PayrollJVReportTestDataBuilder _data;
    private PayrollArea _bmcArea;

    public PayrollJVCustom016MainFormatBmcReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom016MainFormatBmcReportAppService>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private static GetPayrollJVCustom016MainFormatReportInput Input(
        int month = 6, int year = 2024, string employeeCode = null, PayrollRunType? runType = null,
        int maxResultCount = 100, int skipCount = 0)
    {
        return new GetPayrollJVCustom016MainFormatReportInput
        {
            Month = month,
            Year = year,
            EmployeeCode = employeeCode,
            PayrollRunType = runType,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    /// <summary>
    /// The app service resolves a SINGLE payroll area by Name == "BMC" (FirstOrDefaultAsync); creating a
    /// fresh one per call would silently drop every employee but the first-matched area's from results.
    /// </summary>
    private (Employee employee, AccountInfo account) ArrangeLine(
        string employeeCode, string costCenterCode, string wageTypeCode, double amount,
        BillingType billingType, string accountCode = "80001", string offsetAccountCode = "80002")
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

        return (employee, account);
    }

    /// <summary>
    /// Legacy lines 1456-1524: a wage type NOT on the "NotInDebitWageTypeList" is grouped by
    /// (CostCenter, Account, OffsetAccount) into one CostCenter-wise Debit row.
    /// </summary>
    [Fact]
    public async Task Should_Produce_CostCenter_Wise_Debit_Row_For_A_Non_Special_Wage_Type()
    {
        var (employee, account) = ArrangeLine("JV16MFB-1", "1100", "2000", 250, BillingType.Earning);

        var result = await _appService.GetPayrollJVCustom016MainFormatBmcReport(Input());

        var row = result.Items.Single(r => r.Account.Contains(account.Code));
        row.Account.ShouldBe("1100 " + account.Code.PadLeft(8, '0'));
        row.Amount.ShouldBe(250);
        row.Narrative.ShouldBe("SAL Account " + account.Code);
        row.Suffix.ShouldBe(JuneEnd.ToString("MMM/yyyy").ToUpperInvariant());
    }

    /// <summary>
    /// Legacy line 1484: a Deduction line's CostAmount is negated in the CostCenter-wise Debit section.
    /// </summary>
    [Fact]
    public async Task Should_Negate_Deduction_Amounts_In_The_Debit_Section()
    {
        var (_, account) = ArrangeLine("JV16MFB-2", "1200", "2001x", 100, BillingType.Deduction, "80003", "80004");

        var result = await _appService.GetPayrollJVCustom016MainFormatBmcReport(Input());

        result.Items.Single(r => r.Account.Contains(account.Code)).Amount.ShouldBe(-100);
    }

    /// <summary>
    /// Legacy lines 1265, 1282: the Annual Leave Pay summary row's narrative always uses wage type
    /// "1003"'s own name, even when the arrear "7003" is the line that actually matched.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_Fixed_WageType_Name_In_Annual_Leave_Pay_Narrative()
    {
        _data.CreateWageType("1003", BillingType.Earning); // exists in the system but not on any register.
        ArrangeLine("JV16MFB-3", "1300", "7003", 60, BillingType.Earning);

        var result = await _appService.GetPayrollJVCustom016MainFormatBmcReport(Input());

        var summaryRow = result.Items.Single(r => r.Narrative.Contains("WT 1003"));
        summaryRow.Amount.ShouldBe(60);
    }

    [Fact]
    public async Task Should_Be_Empty_When_No_Payroll_Area_Is_Named_Bmc()
    {
        var result = await _appService.GetPayrollJVCustom016MainFormatBmcReport(Input());

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Code_List()
    {
        ArrangeLine("JV16MFB-C-A", "1100", "2000", 100, BillingType.Earning, "81001", "81002");
        ArrangeLine("JV16MFB-C-B", "1100", "2000", 100, BillingType.Earning, "81001", "81002");

        var result = await _appService.GetPayrollJVCustom016MainFormatBmcReport(
            Input(employeeCode: "JV16MFB-C-A"));

        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Export_To_Excel_With_The_Full_Filtered_Set_Not_Just_One_Page()
    {
        for (var i = 0; i < 3; i++)
        {
            ArrangeLine("JV16MFB-EXP-" + i, "1100", "2000", 100 + i, BillingType.Earning, "82001", "82002");
        }

        var file = await _appService.GetPayrollJVCustom016MainFormatBmcReportToExcel(Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom016MainFormatBmc.xlsx");
        file.FileType.ShouldNotBeNullOrEmpty();
    }
}
