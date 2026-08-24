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
/// Tests for the Payroll JV (Custom 030) grid report (legacy <c>uscRptPayrollJV_Custom030.ascx</c>,
/// <c>Table1()</c> lines 558-1815).
///
/// Five sheets behind one screen, and the only member of the family whose voucher is a two-column PIVOT:
/// "Ops" (CostElement1.Code == "1") and "Admin" ("2"), each pro-rated by <c>CostPercentage</c>. The tests
/// cover that pivot, the two balance sheets, the export contract -- and PIN the legacy accounting defects
/// the port reproduces on purpose.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom030ReportAppService_Tests : AppTestBase
{
    private readonly IPayrollJVCustom030ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly PayrollJVReportTestDataBuilder _data;

    public PayrollJVCustom030ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom030ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private static readonly DateTime JuneEnd = new(2024, 6, 30);
    private static readonly DateTime MayEnd = new(2024, 5, 31);

    private static GetPayrollJVCustom030ReportInput Input(
        PayrollJVCustom030ReportType reportType = PayrollJVCustom030ReportType.SalaryJV,
        int month = 6,
        int year = 2024,
        int? payrollAreaId = null,
        PayrollRunType? runType = null,
        string employeeCode = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollJVCustom030ReportInput
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
    public async Task Should_Pivot_Amounts_Into_Ops_And_Admin_Prorated_By_CostPercentage()
    {
        var employee = _data.CreateEmployee("JV30-PIVOT");
        var basic = _data.CreateWageType("1000");
        var account = _data.CreateAccount("5100", "Basic Salary Expense");

        var ops = _data.CreateCostElement("1");
        var admin = _data.CreateCostElement("2");

        // Two mappings for the same (employee, wage type): 60% Ops, 40% Admin -- the shape this customer's
        // costing uses.
        _data.CreateWageAccount(employee, basic, account, costElement1: ops, costPercentage: 60);
        _data.CreateWageAccount(employee, basic, account, costElement1: admin, costPercentage: 40);

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 1000);

        var rows = (await _appService.GetPayrollJVCustom030Report(Input())).Items.ToList();

        var wageTypeRow = rows.First();
        wageTypeRow.Values["DrCr"].ShouldBe("+Dr/+Cr");
        wageTypeRow.Amounts["Ops"].ShouldBe(600);
        wageTypeRow.Amounts["Admin"].ShouldBe(400);
        wageTypeRow.Amounts["Lowwo"].ShouldBe(1000);
        wageTypeRow.Values["Ops Account Description"].ShouldBe("Basic Salary Expense");
    }

    [Fact]
    public async Task Should_Resolve_Period_To_The_Last_Day_Of_The_Month_And_Exclude_The_Neighbouring_Months()
    {
        var basic = _data.CreateWageType("1000");
        var ops = _data.CreateCostElement("1");
        var account = _data.CreateAccount("5100", "Basic");

        var inside = _data.CreateEmployee("JV30-IN");
        _data.CreateWageAccount(inside, basic, account, costElement1: ops);
        _data.CreateDetail(_data.CreateRegister(inside, JuneEnd), basic, 100);

        var before = _data.CreateEmployee("JV30-BEFORE");
        _data.CreateWageAccount(before, basic, account, costElement1: ops);
        _data.CreateDetail(_data.CreateRegister(before, MayEnd), basic, 100);

        var after = _data.CreateEmployee("JV30-AFTER");
        _data.CreateWageAccount(after, basic, account, costElement1: ops);
        _data.CreateDetail(_data.CreateRegister(after, new DateTime(2024, 7, 31)), basic, 100);

        var retro = _data.CreateEmployee("JV30-RETRO");
        _data.CreateWageAccount(retro, basic, account, costElement1: ops);
        _data.CreateDetail(_data.CreateRegister(retro, JuneEnd, new DateTime(2024, 7, 31)), basic, 100);

        var rows = (await _appService.GetPayrollJVCustom030Report(Input())).Items.ToList();

        // The single June wage-type row plus the Net Pay row.
        rows.Count.ShouldBe(2);
        rows.First().Amounts["Lowwo"].ShouldBe(100);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. The Net Pay row drops every employee whose net came out negative (line 986,
    /// <c>.Where(r => r.Net >= 0)</c>), so it does not equal the earnings less the deductions above it.
    /// </summary>
    [Fact]
    public async Task Should_Drop_Employees_With_A_Negative_Net_From_The_NetPay_Row_Reproducing_Legacy_Defect()
    {
        var basic = _data.CreateWageType("1000");
        var recovery = _data.CreateWageType("2020", BillingType.Deduction);
        var ops = _data.CreateCostElement("1");
        var account = _data.CreateAccount("5100", "Basic");

        // Positive net: 300 earned, 100 deducted.
        var positive = _data.CreateEmployee("JV30-NET-POS");
        _data.CreateWageAccount(positive, basic, account, costElement1: ops);
        _data.CreateWageAccount(positive, recovery, account, costElement1: ops);
        var positiveRegister = _data.CreateRegister(positive, JuneEnd);
        _data.CreateDetail(positiveRegister, basic, 300);
        _data.CreateDetail(positiveRegister, recovery, 100);

        // Negative net: 100 earned, 400 recovered -- legacy discards this employee entirely.
        var negative = _data.CreateEmployee("JV30-NET-NEG");
        _data.CreateWageAccount(negative, basic, account, costElement1: ops);
        _data.CreateWageAccount(negative, recovery, account, costElement1: ops);
        var negativeRegister = _data.CreateRegister(negative, JuneEnd);
        _data.CreateDetail(negativeRegister, basic, 100);
        _data.CreateDetail(negativeRegister, recovery, 400);

        var rows = (await _appService.GetPayrollJVCustom030Report(Input())).Items.ToList();
        var netPayRow = rows.Single(r => r.Values["WageType"] == "Net Pay");

        // Both employees together net 300 - 100 + 100 - 400 = -100. Legacy reports only the positive one.
        netPayRow.Amounts["Ops"].ShouldBe(200);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. Employees whose lines touch BOTH cost centres are bucketed "Lowvo" (lines
    /// 966-969) and only the "Ops" and "Admin" buckets are summed into the Net Pay row (lines 991-992), so
    /// their net pay is computed and then discarded.
    /// </summary>
    [Fact]
    public async Task Should_Drop_The_Lowvo_Bucket_From_The_NetPay_Row_Reproducing_Legacy_Defect()
    {
        var employee = _data.CreateEmployee("JV30-LOWVO");
        var basic = _data.CreateWageType("1000");
        var account = _data.CreateAccount("5100", "Basic");

        // Split across BOTH cost centres, so this employee falls in the "Lowvo" bucket.
        _data.CreateWageAccount(employee, basic, account, costElement1: _data.CreateCostElement("1"), costPercentage: 50);
        _data.CreateWageAccount(employee, basic, account, costElement1: _data.CreateCostElement("2"), costPercentage: 50);

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 1000);

        var rows = (await _appService.GetPayrollJVCustom030Report(Input())).Items.ToList();

        // The wage-type row still shows the money...
        rows.First().Amounts["Lowwo"].ShouldBe(1000);

        // ...but the Net Pay row is zero, because neither the Ops nor the Admin bucket claimed it.
        var netPayRow = rows.Single(r => r.Values["WageType"] == "Net Pay");
        netPayRow.Amounts["Ops"].ShouldBe(0);
        netPayRow.Amounts["Admin"].ShouldBe(0);
        netPayRow.Amounts["Lowwo"].ShouldBe(0);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. EmployeeWise BreakDown writes <c>CostAmount >= 0 ? CostAmount : 0</c>
    /// (line 1136) while still emitting the row.
    /// </summary>
    [Fact]
    public async Task Should_Zero_Negative_Amounts_On_BreakDown_Reproducing_Legacy_Defect()
    {
        var employee = _data.CreateEmployee("JV30-BD-NEG");
        var deduction = _data.CreateWageType("2001", BillingType.Deduction);
        _data.CreateWageAccount(
            employee,
            deduction,
            _data.CreateAccount("2200", "Deductions Payable"),
            costElement1: _data.CreateCostElement("1"));

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), deduction, -75);

        var row = (await _appService.GetPayrollJVCustom030Report(
            Input(PayrollJVCustom030ReportType.EmployeeWiseBreakDown))).Items.Single();

        row.Values["EmployeeCode"].ShouldBe("JV30-BD-NEG");
        row.Amounts["Amount"].ShouldBe(0);
    }

    [Fact]
    public async Task Should_List_Unmapped_Lines_On_MissingAccounts()
    {
        var unmapped = _data.CreateEmployee("JV30-MA");
        var basic = _data.CreateWageType("1000");
        _data.CreateDetail(_data.CreateRegister(unmapped, JuneEnd), basic, 200);

        var result = await _appService.GetPayrollJVCustom030Report(
            Input(PayrollJVCustom030ReportType.MissingAccounts));

        result.TotalCount.ShouldBe(1);

        var row = result.Items.Single();
        row.Values["EmpCode"].ShouldBe("JV30-MA");
        row.Values["ForPeriod"].ShouldBe("2024-06-30");
        row.Amounts["Amount"].ShouldBe(200);
    }

    [Fact]
    public async Task Should_Build_The_Indemnity_Balance_Movement_From_WageTypes_3000_And_3003()
    {
        var employee = _data.CreateEmployee("JV30-IND");
        var charge = _data.CreateWageType("3000", BillingType.Provision);
        var closing = _data.CreateWageType("3003", BillingType.Provision);
        var ops = _data.CreateCostElement("1");
        var account = _data.CreateAccount("2300", "Indemnity Provision");

        _data.CreateWageAccount(employee, charge, account, costElement1: ops);
        _data.CreateWageAccount(employee, closing, account, costElement1: ops);

        // Last month closed at 900.
        _data.CreateDetail(_data.CreateRegister(employee, MayEnd), closing, 900);

        // This month charges 100 and closes at 1000 -- a clean movement, so Difference is zero.
        var juneRegister = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(juneRegister, charge, 100);
        _data.CreateDetail(juneRegister, closing, 1000);

        var rows = (await _appService.GetPayrollJVCustom030Report(
            Input(PayrollJVCustom030ReportType.IndemnityJV))).Items.ToList();

        rows.Count.ShouldBe(4);
        rows[0].Values["Description"].ShouldBe("Previous Opening Balance");
        rows[0].Amounts["Ops"].ShouldBe(900);
        rows[1].Values["Description"].ShouldBe("Current Month Balance");
        rows[1].Amounts["Ops"].ShouldBe(100);
        rows[2].Values["Description"].ShouldBe("Closing Balance");
        rows[2].Amounts["Ops"].ShouldBe(1000);
        rows[3].Values["Description"].ShouldBe("Difference");
        rows[3].Amounts["Ops"].ShouldBe(0);
    }

    [Fact]
    public async Task Should_Build_The_AnnualLeave_Balance_Movement_With_A_Paid_Line()
    {
        var employee = _data.CreateEmployee("JV30-AL");
        var charge = _data.CreateWageType("3001", BillingType.Provision);
        var closing = _data.CreateWageType("3004", BillingType.Provision);
        var paid = _data.CreateWageType("1003");
        var ops = _data.CreateCostElement("1");
        var account = _data.CreateAccount("2400", "Leave Provision");

        _data.CreateWageAccount(employee, charge, account, costElement1: ops);
        _data.CreateWageAccount(employee, closing, account, costElement1: ops);
        _data.CreateWageAccount(employee, paid, account, costElement1: ops);

        _data.CreateDetail(_data.CreateRegister(employee, MayEnd), closing, 500);

        var juneRegister = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(juneRegister, charge, 200);
        _data.CreateDetail(juneRegister, paid, 50);
        _data.CreateDetail(juneRegister, closing, 650);

        var rows = (await _appService.GetPayrollJVCustom030Report(
            Input(PayrollJVCustom030ReportType.AnnualLeaveJV))).Items.ToList();

        rows.Count.ShouldBe(5);
        rows[0].Amounts["Ops"].ShouldBe(500);
        rows[1].Amounts["Ops"].ShouldBe(200);
        rows[2].Values["Description"].ShouldBe("Less (QB Payroll Sheet)");
        rows[2].Amounts["Ops"].ShouldBe(50);
        rows[3].Amounts["Ops"].ShouldBe(650);
        // 500 + 200 - 650 - 50 == 0.
        rows[4].Values["Description"].ShouldBe("Difference");
        rows[4].Amounts["Ops"].ShouldBe(0);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. The two balance sheets re-query from scratch with only a period and a
    /// billing-type predicate (lines 1157-1163, 1431-1438), so Payroll Area, Run Type and Employee Code
    /// have no effect on them at all.
    /// </summary>
    [Fact]
    public async Task Should_Ignore_The_Filter_Panel_On_The_Balance_Sheets_Reproducing_Legacy_Defect()
    {
        var employee = _data.CreateEmployee("JV30-IND-FILTERED");
        var charge = _data.CreateWageType("3000", BillingType.Provision);
        _data.CreateWageAccount(
            employee,
            charge,
            _data.CreateAccount("2300", "Indemnity Provision"),
            costElement1: _data.CreateCostElement("1"));

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), charge, 100);

        // Filtering to a completely different employee code must NOT change the movement.
        var rows = (await _appService.GetPayrollJVCustom030Report(
            Input(PayrollJVCustom030ReportType.IndemnityJV, employeeCode: "SOMEONE-ELSE"))).Items.ToList();

        rows.Single(r => r.Values["Description"] == "Current Month Balance").Amounts["Ops"].ShouldBe(100);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var basic = _data.CreateWageType("1000");
        var allowance = _data.CreateWageType("1001");
        var account = _data.CreateAccount("5100", "Basic Salary Expense");
        var ops = _data.CreateCostElement("1");

        var employee = _data.CreateEmployee("JV30-EXP");
        _data.CreateWageAccount(employee, basic, account, costElement1: ops);
        _data.CreateWageAccount(employee, allowance, account, costElement1: ops);

        var register = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(register, basic, 100);
        _data.CreateDetail(register, allowance, 50);

        // Page size of 1: the export must ignore it and still return every row (two wage types plus the Net
        // Pay row).
        var file = await _appService.GetPayrollJVCustom030ReportToExcel(Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom030.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);

        // Every header must be real text -- ABP renders an unknown localization key as "[Key]", which the
        // customer's reconciliation against the legacy file would not survive.
        var unresolved = exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ToList();
        unresolved.ShouldBeEmpty();

        exported.First().Keys.ShouldContain("Ops Account Description");
        // Legacy's own misspelling of "Lowvo", kept verbatim in the sheet header.
        exported.First().Keys.ShouldContain("Lowwo");
    }
}
