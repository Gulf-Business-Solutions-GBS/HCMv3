using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Payroll JV (Custom 045) grid report (legacy <c>uscRptPayrollJV_Custom045.ascx</c>,
/// <c>Table1()</c> lines 557-1145).
///
/// Three sheets behind one screen. The tests cover the shared preamble (period boundary, run type,
/// payroll area, employee code), one behaviour per sheet, the export contract -- and PIN the five legacy
/// accounting defects the port reproduces on purpose.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom045ReportAppService_Tests : AppTestBase
{
    private readonly IPayrollJVCustom045ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly PayrollJVReportTestDataBuilder _data;

    public PayrollJVCustom045ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom045ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private static GetPayrollJVCustom045ReportInput Input(
        PayrollJVCustom045ReportType reportType = PayrollJVCustom045ReportType.SalaryJV,
        int month = 6,
        int year = 2024,
        int? payrollAreaId = null,
        PayrollRunType? runType = null,
        string employeeCode = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollJVCustom045ReportInput
        {
            ReportType = reportType,
            Month = month,
            Year = year,
            PayrollAreaId = payrollAreaId,
            PayrollRunType = runType,
            EmployeeCode = employeeCode,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Serve_The_Column_List_Of_The_Selected_Report_Type()
    {
        var salaryJv = await _appService.GetPayrollJVCustom045Report(Input());
        var missing = await _appService.GetPayrollJVCustom045Report(
            Input(PayrollJVCustom045ReportType.MissingAccounts));

        salaryJv.Columns.Select(c => c.Name).ShouldContain("lines.debit.value.amount");
        missing.Columns.Select(c => c.Name).ShouldContain("OffsetAccountInfo Code");
        salaryJv.Columns.Count.ShouldNotBe(missing.Columns.Count);
    }

    [Fact]
    public async Task Should_Emit_A_Debit_Line_Per_Employee_Account_Combination()
    {
        var employee = _data.CreateEmployee("JV45-A");
        var basic = _data.CreateWageType("1000");
        var account = _data.CreateAccount("5100", "Basic Salary Expense");
        _data.CreateWageAccount(employee, basic, account);

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 500);

        var result = await _appService.GetPayrollJVCustom045Report(Input());

        result.TotalCount.ShouldBe(1);

        var row = result.Items.Single();
        row.Values["lines.account"].ShouldBe("5100");
        row.Values["lines.subsidiary#type"].ShouldBe(string.Empty);
        row.Amounts["lines.debit.localAmount"].ShouldBe(500);
        row.Amounts["lines.credit.localAmount"].ShouldBe(0);
        // Legacy lines 899-902: the entity-dimension CODE cell carries the literal "Employee" and the NAME
        // cell carries the employee's code.
        row.Values["lines.entityDimension#code"].ShouldBe("Employee");
        row.Values["lines.entityDimension@name"].ShouldBe("JV45-A");
    }

    [Fact]
    public async Task Should_Resolve_Period_To_The_Last_Day_Of_The_Month_And_Exclude_The_Neighbouring_Months()
    {
        var basic = _data.CreateWageType("1000");

        var inside = _data.CreateEmployee("JV45-IN");
        _data.CreateDetail(_data.CreateRegister(inside, JuneEnd), basic, 100);

        var before = _data.CreateEmployee("JV45-BEFORE");
        _data.CreateDetail(_data.CreateRegister(before, new DateTime(2024, 5, 31)), basic, 100);

        var after = _data.CreateEmployee("JV45-AFTER");
        _data.CreateDetail(_data.CreateRegister(after, new DateTime(2024, 7, 31)), basic, 100);

        var retro = _data.CreateEmployee("JV45-RETRO");
        _data.CreateDetail(_data.CreateRegister(retro, JuneEnd, new DateTime(2024, 7, 31)), basic, 100);

        var result = await _appService.GetPayrollJVCustom045Report(Input());

        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Exclude_WageTypes_Not_Flagged_For_FI_Posting()
    {
        var employee = _data.CreateEmployee("JV45-FI");
        var posted = _data.CreateWageType("1000");
        var notPosted = _data.CreateWageType("1002", includeInFiPosting: false);

        var register = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(register, posted, 100);
        _data.CreateDetail(register, notPosted, 999);

        var result = await _appService.GetPayrollJVCustom045Report(Input());

        result.TotalCount.ShouldBe(1);
        result.Items.Single().Amounts["lines.debit.localAmount"].ShouldBe(100);
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        var basic = _data.CreateWageType("1000");

        foreach (var code in new[] { "JV45-C-A", "JV45-C-B", "JV45-C-C" })
        {
            _data.CreateDetail(_data.CreateRegister(_data.CreateEmployee(code), JuneEnd), basic, 100);
        }

        var result = await _appService.GetPayrollJVCustom045Report(
            Input(employeeCode: "JV45-C-A , JV45-C-C"));

        result.TotalCount.ShouldBe(2);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. The Credit Entry loop writes <c>lines.debit.value.amount = CostAmount</c> and
    /// <c>lines.credit.value.amount = 0</c> -- identical to the Debit loop (lines 947-948 vs 875-876). Only
    /// the LOCAL pair is swapped, so an importer reading the value pair books every deduction as a debit.
    /// </summary>
    [Fact]
    public async Task Should_Write_Credit_Amounts_Into_The_Debit_Value_Column_Reproducing_Legacy_Defect()
    {
        var employee = _data.CreateEmployee("JV45-CR");
        var deduction = _data.CreateWageType("2001", BillingType.Deduction);
        _data.CreateWageAccount(employee, deduction, _data.CreateAccount("2200", "Deductions Payable"));

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), deduction, 120);

        var row = (await _appService.GetPayrollJVCustom045Report(Input())).Items.Single();

        row.Values["lines.subsidiary#type"].ShouldBe("Employee");
        // The defect: the amount lands on the DEBIT value column even though this is a credit line.
        row.Amounts["lines.debit.value.amount"].ShouldBe(120);
        row.Amounts["lines.credit.value.amount"].ShouldBe(0);
        // The local pair is the one that is correct.
        row.Amounts["lines.credit.localAmount"].ShouldBe(120);
        row.Amounts["lines.debit.localAmount"].ShouldBe(0);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. <c>(x != null) ? x.NameEn : "" + " " + voucherDate</c> binds the concatenation
    /// to the FALSE branch only (lines 885, 957), so a mapped line loses its period from the narration.
    /// </summary>
    [Fact]
    public async Task Should_Drop_The_Period_From_The_Narration_On_Mapped_Lines_Reproducing_Legacy_Defect()
    {
        var mapped = _data.CreateEmployee("JV45-N-MAPPED");
        var basic = _data.CreateWageType("1000");
        _data.CreateWageAccount(mapped, basic, _data.CreateAccount("5100", "Basic Salary Expense"));
        _data.CreateDetail(_data.CreateRegister(mapped, JuneEnd), basic, 100);

        var unmapped = _data.CreateEmployee("JV45-N-UNMAPPED");
        _data.CreateDetail(_data.CreateRegister(unmapped, JuneEnd), basic, 200);

        var rows = (await _appService.GetPayrollJVCustom045Report(Input())).Items.ToList();

        var mappedRow = rows.Single(r => r.Values["lines.account"] == "5100");
        var unmappedRow = rows.Single(r => r.Values["lines.account"] == string.Empty);

        // Mapped: bare account name, no period.
        mappedRow.Values["lines.narration"].ShouldBe("Basic Salary Expense");
        // Unmapped: a leading space then the period, and no account name.
        unmappedRow.Values["lines.narration"].ShouldBe(" Jun 2024");
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. The dimension CODE columns are filled from <c>CostElement.NameEn</c> and an
    /// unmapped dimension becomes the literal "PUBLIC" (lines 887-895).
    /// </summary>
    [Fact]
    public async Task Should_Fill_Dimension_Codes_With_Names_And_Default_To_PUBLIC_Reproducing_Legacy_Defect()
    {
        var employee = _data.CreateEmployee("JV45-DIM");
        var basic = _data.CreateWageType("1000");
        // CostElement1 is Branch on THIS control (lines 846-848); Sector and Department are left unmapped.
        _data.CreateWageAccount(
            employee,
            basic,
            _data.CreateAccount("5100", "Basic"),
            costElement1: _data.CreateCostElement("BR1"));

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 100);

        var row = (await _appService.GetPayrollJVCustom045Report(Input())).Items.Single();

        row.Values["lines.branch"].ShouldBe("CE BR1");
        row.Values["lines.branch@name"].ShouldBe("CE BR1");
        row.Values["lines.sector"].ShouldBe("PUBLIC");
        row.Values["lines.department"].ShouldBe("PUBLIC");
        row.Values["lines.analysisSet"].ShouldBe("PUBLIC");
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. EmployeeWise BreakDown writes <c>CostAmount >= 0 ? CostAmount : 0</c> for both
    /// amount columns (lines 1130-1131) while still emitting the row and its Dr/Cr marker.
    /// </summary>
    [Fact]
    public async Task Should_Zero_Negative_Amounts_On_BreakDown_Reproducing_Legacy_Defect()
    {
        var employee = _data.CreateEmployee("JV45-BD-NEG");
        var deduction = _data.CreateWageType("2001", BillingType.Deduction);
        _data.CreateWageAccount(employee, deduction, _data.CreateAccount("2200", "Deductions Payable"));

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), deduction, -75);

        var row = (await _appService.GetPayrollJVCustom045Report(
            Input(PayrollJVCustom045ReportType.EmployeeWiseBreakDown))).Items.Single();

        row.Amounts["Fc amount"].ShouldBe(0);
        row.Amounts["Net amount"].ShouldBe(0);
        row.Values["Dr/Cr"].ShouldBe("C");
        // Legacy's own typo in the caption, kept verbatim so the customer's reconciliation still matches.
        row.Values.ShouldContainKey("Anl2");
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. Missing Accounts lists exactly the lines whose left join found NO wage
    /// account, so its Division/Department/Analysis/CostPercentage columns can never be populated.
    /// </summary>
    [Fact]
    public async Task Should_Leave_CostCentre_Columns_Blank_On_MissingAccounts_Reproducing_Legacy_Defect()
    {
        var employee = _data.CreateEmployee("JV45-MA");
        var basic = _data.CreateWageType("1000");
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 100);

        var row = (await _appService.GetPayrollJVCustom045Report(
            Input(PayrollJVCustom045ReportType.MissingAccounts))).Items.Single();

        row.Values["EmpCode"].ShouldBe("JV45-MA");
        row.Values["ForPeriod"].ShouldBe("2024-06-30");
        row.Values["Division"].ShouldBe(string.Empty);
        row.Values["Department"].ShouldBe(string.Empty);
        row.Values["Analysis"].ShouldBe(string.Empty);
        row.Amounts["CostPercentage"].ShouldBe(0);
        row.Amounts["Amount"].ShouldBe(100);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var basic = _data.CreateWageType("1000");
        var account = _data.CreateAccount("5100", "Basic Salary Expense");

        foreach (var code in new[] { "JV45-EXP-A", "JV45-EXP-B", "JV45-EXP-C" })
        {
            var employee = _data.CreateEmployee(code);
            _data.CreateWageAccount(employee, basic, account);
            _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 100);
        }

        // A different month -- must not reach the export.
        var other = _data.CreateEmployee("JV45-EXP-OUT");
        _data.CreateDetail(_data.CreateRegister(other, new DateTime(2024, 7, 31)), basic, 900);

        // Page size of 1: the export must ignore it and still return every filtered row.
        var file = await _appService.GetPayrollJVCustom045ReportToExcel(Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom045.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);

        // Every header must be real text -- ABP renders an unknown localization key as "[Key]", which in an
        // ERP import file would silently break the import.
        var unresolved = exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ToList();
        unresolved.ShouldBeEmpty();

        exported.First().Keys.ShouldContain("lines.debit.value.amount");
        exported.First().Keys.ShouldContain("lines.entityDimension#code");
    }
}
