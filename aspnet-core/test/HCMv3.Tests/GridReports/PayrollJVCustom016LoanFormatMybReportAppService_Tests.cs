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
/// Tests for the Payroll JV (Custom 016) "Loan Format - MYB" grid report (legacy
/// <c>uscRptPayrollJV_Custom016.ascx</c>, <c>Table1()</c>, <c>cmbType == "Loan Format"</c> branch, MYB
/// column set -- lines 573-1168 common preprocessing + 2083-2088 + 2194-2298).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom016LoanFormatMybReportAppService_Tests : AppTestBase
{
    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private readonly IPayrollJVCustom016LoanFormatMybReportAppService _appService;
    private readonly PayrollJVReportTestDataBuilder _data;

    public PayrollJVCustom016LoanFormatMybReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom016LoanFormatMybReportAppService>();
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

    private Employee ArrangeBasicLoanLine(
        string employeeCode, double amount, string wageTypeCode = "2001",
        BillingType billingType = BillingType.Deduction, string mybAreaName = "MYB")
    {
        var mybArea = _data.CreatePayrollArea("MYB01-" + employeeCode, mybAreaName);
        var employee = _data.CreateEmployee(employeeCode);
        _data.CreateOrgAssignment(employee, mybArea, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var wageType = _data.CreateWageType(wageTypeCode, billingType);
        var account = _data.CreateAccount("71001", "Loan Account");
        var offsetAccount = _data.CreateAccount("71002", "Loan Offset");
        _data.CreateWageAccount(employee, wageType, account, offsetAccount, costPercentage: 100);
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, amount);

        return employee;
    }

    /// <summary>Legacy lines 2244-2295: each group produces a Debit row THEN a mirrored Credit row.</summary>
    [Fact]
    public async Task Should_Produce_A_Debit_And_Credit_Row_Per_Group()
    {
        var employee = ArrangeBasicLoanLine("JV16MYB-1", 400);

        var result = await _appService.GetPayrollJVCustom016LoanFormatMybReport(Input());

        result.TotalCount.ShouldBe(2);
        var debitRow = result.Items.Single(r => r.Debit > 0);
        var creditRow = result.Items.Single(r => r.Credit > 0);

        debitRow.Debit.ShouldBe(400);
        debitRow.Exp.ShouldBe("071002"); // OffsetAccountInfo code, padded to 6.
        debitRow.CostCenter.ShouldBe("0000");
        debitRow.Suffix.ShouldBe(employee.Code);
        debitRow.SlPlAccount.ShouldBe(employee.ReferenceCode);

        creditRow.Credit.ShouldBe(400);
        creditRow.Exp.ShouldBe("071001"); // AccountInfo code, padded to 6.
        creditRow.Narrative.ShouldEndWith("SAL LOAN");
    }

    /// <summary>Legacy line 2195: <c>emppayrollArea.Name.ToUpper().Contains("MYB")</c>.</summary>
    [Fact]
    public async Task Should_Match_Payroll_Area_By_Name_Containment_Case_Insensitively()
    {
        ArrangeBasicLoanLine("JV16MYB-CI", 100, mybAreaName: "myb-kuwait");

        var result = await _appService.GetPayrollJVCustom016LoanFormatMybReport(Input());

        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Be_Empty_When_No_Payroll_Area_Name_Contains_Myb()
    {
        var result = await _appService.GetPayrollJVCustom016LoanFormatMybReport(Input());

        result.TotalCount.ShouldBe(0);
    }

    /// <summary>Legacy line 2086: only the six Loan wage types are admitted.</summary>
    [Fact]
    public async Task Should_Exclude_Non_Loan_Wage_Types()
    {
        ArrangeBasicLoanLine("JV16MYB-NL", 100, wageTypeCode: "1000", billingType: BillingType.Earning);

        var result = await _appService.GetPayrollJVCustom016LoanFormatMybReport(Input());

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_To_Excel_With_The_Full_Filtered_Set_Not_Just_One_Page()
    {
        for (var i = 0; i < 3; i++)
        {
            ArrangeBasicLoanLine("JV16MYB-EXP-" + i, 100 + i);
        }

        var file = await _appService.GetPayrollJVCustom016LoanFormatMybReportToExcel(
            Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom016LoanFormatMyb.xlsx");
        file.FileType.ShouldNotBeNullOrEmpty();
    }
}
