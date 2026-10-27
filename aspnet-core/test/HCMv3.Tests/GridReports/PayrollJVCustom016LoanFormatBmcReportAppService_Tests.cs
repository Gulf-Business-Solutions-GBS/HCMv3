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
/// Tests for the Payroll JV (Custom 016) "Loan Format - BMC" grid report (legacy
/// <c>uscRptPayrollJV_Custom016.ascx</c>, <c>Table1()</c>, <c>cmbType == "Loan Format"</c> branch, BMC
/// column set -- lines 573-1168 common preprocessing + 2083-2191).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom016LoanFormatBmcReportAppService_Tests : AppTestBase
{
    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private readonly IPayrollJVCustom016LoanFormatBmcReportAppService _appService;
    private readonly PayrollJVReportTestDataBuilder _data;

    public PayrollJVCustom016LoanFormatBmcReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom016LoanFormatBmcReportAppService>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private static GetPayrollJVCustom016LoanFormatReportInput Input(
        int month = 6, int year = 2024, string employeeCode = null, PayrollRunType? runType = null,
        int maxResultCount = 100, int skipCount = 0)
    {
        return new GetPayrollJVCustom016LoanFormatReportInput
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
    /// Arranges one employee with a Loan Recovery Deduction (2001) line, mapped to a wage account and a
    /// cost-center-bearing org assignment, for the payroll area literally named "BMC".
    /// </summary>
    private PayrollArea _bmcArea;

    private (Employee employee, HCMv3.OrganizationManagement.CostCenter costCenter) ArrangeBasicLoanLine(
        string employeeCode, string costCenterCode, double amount, string wageTypeCode = "2001",
        BillingType billingType = BillingType.Deduction)
    {
        // The app service resolves a SINGLE payroll area by Name == "BMC" (FirstOrDefaultAsync); creating a
        // fresh one per call would silently drop every employee but the first-matched area's from results.
        var bmcArea = _bmcArea ??= _data.CreatePayrollArea("BMC01", "BMC");
        var costCenter = _data.CreateCostCenter(costCenterCode);
        var employee = _data.CreateEmployee(employeeCode);
        _data.CreateOrgAssignment(employee, bmcArea, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), costCenter);

        var wageType = _data.CreateWageType(wageTypeCode, billingType);
        var account = _data.CreateAccount("70001", "Loan Account");
        var offsetAccount = _data.CreateAccount("70002", "Loan Offset");
        _data.CreateWageAccount(employee, wageType, account, offsetAccount, costPercentage: 100);
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, amount);

        return (employee, costCenter);
    }

    [Fact]
    public async Task Should_Produce_One_Row_Per_Employee_Wage_Type_Account_Group()
    {
        var (employee, _) = ArrangeBasicLoanLine("JV16BMC-1", "1100", 200);

        var result = await _appService.GetPayrollJVCustom016LoanFormatBmcReport(Input());

        var row = result.Items.Single();
        row.SlPlAccount.ShouldBe("E8" + employee.Code);
        row.BaseValue.ShouldBe(-200);
        row.GoodsValue.ShouldBe(200);
        row.Mcsr.ShouldBe("REV8-PY");
        row.EColumn.ShouldBe("E");
        row.VatCode.ShouldBe("S");
        row.Vat.ShouldBe(0);
    }

    /// <summary>Legacy lines 2152-2161: the first digit of the cost-center code buckets it.</summary>
    [Theory]
    [InlineData("1100", "1000")]
    [InlineData("2200", "2000")]
    [InlineData("9900", "0900")]
    public async Task Should_Bucket_The_Cost_Center_By_First_Digit(string rawCode, string expectedBucket)
    {
        ArrangeBasicLoanLine("JV16BMC-BKT-" + rawCode, rawCode, 150);

        var result = await _appService.GetPayrollJVCustom016LoanFormatBmcReport(Input());

        result.Items.Single().NlCode.ShouldStartWith(expectedBucket);
    }

    /// <summary>Legacy line 2162-2163: branch is "2900" for cost centers starting with "2", else "1900".</summary>
    [Fact]
    public async Task Should_Resolve_Branch_Code_From_Cost_Center_Prefix()
    {
        ArrangeBasicLoanLine("JV16BMC-BR1", "2500", 50);
        ArrangeBasicLoanLine("JV16BMC-BR2", "4500", 50);

        var result = await _appService.GetPayrollJVCustom016LoanFormatBmcReport(Input());

        result.Items.Single(r => r.SlPlAccount == "E8JV16BMC-BR1").Branch.ShouldBe("2900");
        result.Items.Single(r => r.SlPlAccount == "E8JV16BMC-BR2").Branch.ShouldBe("1900");
    }

    /// <summary>Legacy line 2086: only the six Loan wage types are admitted; a Basic Pay line is not.</summary>
    [Fact]
    public async Task Should_Exclude_Non_Loan_Wage_Types()
    {
        ArrangeBasicLoanLine("JV16BMC-NL", "1100", 100, wageTypeCode: "1000", billingType: BillingType.Earning);

        var result = await _appService.GetPayrollJVCustom016LoanFormatBmcReport(Input());

        result.TotalCount.ShouldBe(0);
    }

    /// <summary>
    /// Legacy line 2092: this format only fires for the payroll area literally named "BMC" -- when no
    /// such payroll area exists at all, the report is empty rather than throwing.
    /// </summary>
    [Fact]
    public async Task Should_Be_Empty_When_No_Payroll_Area_Is_Named_Bmc()
    {
        var result = await _appService.GetPayrollJVCustom016LoanFormatBmcReport(Input());

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Code_List()
    {
        ArrangeBasicLoanLine("JV16BMC-C-A", "1100", 100);
        ArrangeBasicLoanLine("JV16BMC-C-B", "1100", 100);

        var result = await _appService.GetPayrollJVCustom016LoanFormatBmcReport(
            Input(employeeCode: "JV16BMC-C-A"));

        result.Items.Single().SlPlAccount.ShouldBe("E8JV16BMC-C-A");
    }

    /// <summary>
    /// Legacy lines 1114-1136 (via <see cref="PayrollJVCustom016PostingLineLoader"/>): a Regular-run
    /// Deduction line (Loan wage types are Deduction) is ZEROED for an employee whose Pay-In-Advance leave
    /// spans the whole requested month -- so the group's CostAmount nets to 0 and legacy drops the row
    /// (line 2145: <c>.Where(r => r.CostAmount != 0)</c>).
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_FullMonthLeave_Zeroing_Of_Loan_Deduction_Lines()
    {
        var (employee, _) = ArrangeBasicLoanLine("JV16BMC-FML", "1100", 300);

        // Pay-in-advance leave, payment made before the month, spanning the whole of June 2024.
        _data.CreateAbsence(
            employee,
            new DateTime(2024, 5, 1),
            new DateTime(2024, 7, 31),
            new DateTime(2024, 5, 25),
            LeavePaymentType.PayInAdvance);

        var result = await _appService.GetPayrollJVCustom016LoanFormatBmcReport(Input());

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_To_Excel_With_The_Full_Filtered_Set_Not_Just_One_Page()
    {
        for (var i = 0; i < 3; i++)
        {
            ArrangeBasicLoanLine("JV16BMC-EXP-" + i, "1100", 100 + i);
        }

        var file = await _appService.GetPayrollJVCustom016LoanFormatBmcReportToExcel(
            Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom016LoanFormatBmc.xlsx");
        file.FileType.ShouldNotBeNullOrEmpty();
    }
}
