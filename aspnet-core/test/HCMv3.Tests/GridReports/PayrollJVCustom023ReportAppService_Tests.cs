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
/// Tests for the Payroll JV (Custom 023) grid report (legacy <c>uscRptPayrollJV_Custom023.ascx</c>,
/// <c>Table1()</c> lines 557-1176).
///
/// The report is a journal voucher driven by the employee's <c>EmployeeWageAccount</c> mapping, with
/// three mutually exclusive output formats behind the legacy <c>cmbType</c> combo. The tests pin the
/// per-format column set, the debit/credit sign rules, the balancing line and the two legacy accounting
/// defects that are reproduced on purpose.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom023ReportAppService_Tests : AppTestBase
{
    private readonly IPayrollJVCustom023ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly PayrollJVReportTestDataBuilder _data;

    public PayrollJVCustom023ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom023ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private static GetPayrollJVCustom023ReportInput Input(
        int month = 6,
        int year = 2024,
        PayrollJVCustom023Format format = PayrollJVCustom023Format.MainFormat,
        string employeeCode = null,
        PayrollRunType? runType = null,
        int? payrollAreaId = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollJVCustom023ReportInput
        {
            Month = month,
            Year = year,
            Format = format,
            EmployeeCode = employeeCode,
            PayrollRunType = runType,
            PayrollAreaId = payrollAreaId,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    /// <summary>
    /// Legacy <c>GetTable()</c> builds the period from month/year and <c>Table1()</c> matches
    /// <c>ForPeriod == EndDate</c> exactly, so only a register stamped on the LAST day of the requested
    /// month contributes. Mid-month and next-month registers must not leak in.
    /// </summary>
    [Fact]
    public async Task Should_Only_Aggregate_Registers_Stamped_On_The_Requested_Month_End()
    {
        var wageType = _data.CreateWageType("1000");
        var account = _data.CreateAccount("60001", "Salary Expense");

        var inMonth = _data.CreateEmployee("JV23-EOM");
        _data.CreateWageAccount(inMonth, wageType, account);
        _data.CreateDetail(_data.CreateRegister(inMonth, JuneEnd), wageType, 100);

        var midMonth = _data.CreateEmployee("JV23-MID");
        _data.CreateWageAccount(midMonth, wageType, account);
        _data.CreateDetail(_data.CreateRegister(midMonth, new DateTime(2024, 6, 15)), wageType, 500);

        var nextMonth = _data.CreateEmployee("JV23-JUL");
        _data.CreateWageAccount(nextMonth, wageType, account);
        _data.CreateDetail(_data.CreateRegister(nextMonth, new DateTime(2024, 7, 31)), wageType, 700);

        var result = await _appService.GetPayrollJVCustom023Report(Input());

        result.Items.Single(i => i.GLCode == "60001").Debit.ShouldBe(100);
    }

    /// <summary>Legacy line 564: <c>ForPeriod == InPeriod</c> excludes retro runs paid in a later month.</summary>
    [Fact]
    public async Task Should_Exclude_Retro_Runs_Where_InPeriod_Differs_From_ForPeriod()
    {
        var wageType = _data.CreateWageType("1000");
        var account = _data.CreateAccount("60002", "Salary Expense");

        var inPeriod = _data.CreateEmployee("JV23-IN");
        _data.CreateWageAccount(inPeriod, wageType, account);
        _data.CreateDetail(_data.CreateRegister(inPeriod, JuneEnd), wageType, 100);

        var retro = _data.CreateEmployee("JV23-RETRO");
        _data.CreateWageAccount(retro, wageType, account);
        _data.CreateDetail(
            _data.CreateRegister(retro, JuneEnd, new DateTime(2024, 7, 31)), wageType, 900);

        var result = await _appService.GetPayrollJVCustom023Report(Input());

        result.Items.Single(i => i.GLCode == "60002").Debit.ShouldBe(100);
    }

    /// <summary>Legacy line 566: only wage types flagged <c>IncludeInFIPosting</c> reach the voucher.</summary>
    [Fact]
    public async Task Should_Exclude_Wage_Types_Not_Flagged_For_FI_Posting()
    {
        var employee = _data.CreateEmployee("JV23-FI");
        var register = _data.CreateRegister(employee, JuneEnd);
        var account = _data.CreateAccount("60003", "Salary Expense");

        var posted = _data.CreateWageType("1000");
        var notPosted = _data.CreateWageType("1001", includeInFiPosting: false);
        _data.CreateWageAccount(employee, posted, account);
        _data.CreateWageAccount(employee, notPosted, account);

        _data.CreateDetail(register, posted, 100);
        _data.CreateDetail(register, notPosted, 999);

        var result = await _appService.GetPayrollJVCustom023Report(Input());

        result.Items.Single(i => i.GLCode == "60003").Debit.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Code_List()
    {
        var wageType = _data.CreateWageType("1000");

        foreach (var (code, glCode, amount) in new[]
                 {
                     ("JV23-C-A", "61001", 10d),
                     ("JV23-C-B", "61002", 20d),
                     ("JV23-C-C", "61003", 30d)
                 })
        {
            var employee = _data.CreateEmployee(code);
            _data.CreateWageAccount(employee, wageType, _data.CreateAccount(glCode, glCode));
            _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, amount);
        }

        var result = await _appService.GetPayrollJVCustom023Report(
            Input(employeeCode: "JV23-C-A , JV23-C-C"));

        var glCodes = result.Items.Select(i => i.GLCode).ToList();
        glCodes.ShouldContain("61001");
        glCodes.ShouldNotContain("61002");
        glCodes.ShouldContain("61003");
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Run_Type_When_Supplied()
    {
        var wageType = _data.CreateWageType("1000");

        var regular = _data.CreateEmployee("JV23-RT-REG");
        _data.CreateWageAccount(regular, wageType, _data.CreateAccount("62001", "Regular"));
        _data.CreateDetail(_data.CreateRegister(regular, JuneEnd), wageType, 100);

        var offCycle = _data.CreateEmployee("JV23-RT-OFF");
        _data.CreateWageAccount(offCycle, wageType, _data.CreateAccount("62002", "OffCycle"));
        _data.CreateDetail(
            _data.CreateRegister(offCycle, JuneEnd, runType: PayrollRunType.OffCycle), wageType, 250);

        var result = await _appService.GetPayrollJVCustom023Report(
            Input(runType: PayrollRunType.OffCycle));

        result.Items.Select(i => i.GLCode).ShouldNotContain("62001");
        result.Items.Single(i => i.GLCode == "62002").Debit.ShouldBe(250);
    }

    /// <summary>
    /// Legacy lines 585-596: the payroll area is matched through the employee's org assignment covering
    /// the month end -- unlike Custom 018, where the same combo filters nothing.
    /// </summary>
    [Fact]
    public async Task Should_Filter_By_Payroll_Area_Through_The_Org_Assignment()
    {
        // Not about Net Pay: a wage type that does not impact net pay keeps the legacy balancing line
        // (lines 817-843, pinned by the Salary Due to be Adjusted tests) out of this fixture.
        var wageType = _data.CreateWageType("1000", dontImpactNetPay: true);
        var inArea = _data.CreatePayrollArea("PA-IN-23", "In Area");
        var outArea = _data.CreatePayrollArea("PA-OUT-23", "Out Area");

        var inside = _data.CreateEmployee("JV23-PA-IN");
        _data.CreateOrgAssignment(inside, inArea, new DateTime(2024, 1, 1), new DateTime(2099, 12, 31));
        _data.CreateWageAccount(inside, wageType, _data.CreateAccount("63001", "In"));
        _data.CreateDetail(_data.CreateRegister(inside, JuneEnd), wageType, 100);

        var outside = _data.CreateEmployee("JV23-PA-OUT");
        _data.CreateOrgAssignment(outside, outArea, new DateTime(2024, 1, 1), new DateTime(2099, 12, 31));
        _data.CreateWageAccount(outside, wageType, _data.CreateAccount("63002", "Out"));
        _data.CreateDetail(_data.CreateRegister(outside, JuneEnd), wageType, 900);

        var result = await _appService.GetPayrollJVCustom023Report(Input(payrollAreaId: inArea.Id));

        result.Items.Single().GLCode.ShouldBe("63001");
    }

    /// <summary>
    /// Legacy lines 711-768: earnings become debits and deductions become credits, both grouped by
    /// account and cost elements. The Amount column is the raw sum for debits and the NEGATED sum for
    /// credits (lines 727/758).
    /// </summary>
    [Fact]
    public async Task Should_Post_Earnings_As_Debits_And_Deductions_As_Credits()
    {
        var employee = _data.CreateEmployee("JV23-DC");
        var register = _data.CreateRegister(employee, JuneEnd);

        var earning = _data.CreateWageType("1000");
        var deduction = _data.CreateWageType("2001", BillingType.Deduction);

        _data.CreateWageAccount(employee, earning, _data.CreateAccount("64001", "Salary Expense"));
        _data.CreateWageAccount(employee, deduction, _data.CreateAccount("64002", "Loan Recovery"));

        _data.CreateDetail(register, earning, 1000);
        _data.CreateDetail(register, deduction, 150);

        var result = await _appService.GetPayrollJVCustom023Report(Input());

        var debitRow = result.Items.Single(i => i.GLCode == "64001");
        debitRow.Debit.ShouldBe(1000);
        debitRow.Credit.ShouldBe(0);
        debitRow.Amount.ShouldBe(1000);
        debitRow.Remark.ShouldBe("Salary Expense");

        var creditRow = result.Items.Single(i => i.GLCode == "64002");
        creditRow.Debit.ShouldBe(0);
        creditRow.Credit.ShouldBe(150);
        creditRow.Amount.ShouldBe(-150);
    }

    /// <summary>
    /// Legacy lines 771-815: Net Pay (wage type 9000) is excluded from the debit block and posted on its
    /// own as a CREDIT, even though its billing type is Earning.
    /// </summary>
    [Fact]
    public async Task Should_Post_Net_Pay_As_A_Credit_Separate_From_The_Earning_Debits()
    {
        var employee = _data.CreateEmployee("JV23-NET");
        var register = _data.CreateRegister(employee, JuneEnd);

        var basic = _data.CreateWageType("1000");
        var netPay = _data.CreateWageType("9000");

        _data.CreateWageAccount(employee, basic, _data.CreateAccount("65001", "Salary Expense"));
        _data.CreateWageAccount(employee, netPay, _data.CreateAccount("65002", "Salary Payable"));

        _data.CreateDetail(register, basic, 1000);
        _data.CreateDetail(register, netPay, 1000);

        var result = await _appService.GetPayrollJVCustom023Report(Input());

        result.Items.Single(i => i.GLCode == "65001").Debit.ShouldBe(1000);

        var netRow = result.Items.Single(i => i.GLCode == "65002");
        netRow.Debit.ShouldBe(0);
        netRow.Credit.ShouldBe(1000);
        netRow.Amount.ShouldBe(-1000);
    }

    /// <summary>
    /// Legacy lines 817-843: when Net Pay does not equal earnings minus deductions, the difference is
    /// booked on a final line with NO GL account and the fixed caption "Salary Due to be Adjusted on
    /// Leave Resumption". When the two agree, no such line is emitted.
    /// </summary>
    [Fact]
    public async Task Should_Add_The_Salary_Due_To_Be_Adjusted_Line_Only_When_Net_Pay_Does_Not_Reconcile()
    {
        var employee = _data.CreateEmployee("JV23-ADJ");
        var register = _data.CreateRegister(employee, JuneEnd);

        var basic = _data.CreateWageType("1000");
        var netPay = _data.CreateWageType("9000");

        _data.CreateWageAccount(employee, basic, _data.CreateAccount("66001", "Salary Expense"));
        _data.CreateWageAccount(employee, netPay, _data.CreateAccount("66002", "Salary Payable"));

        // Net Pay 800 against earnings of 1000 -- a 200 shortfall.
        _data.CreateDetail(register, basic, 1000);
        _data.CreateDetail(register, netPay, 800);

        var result = await _appService.GetPayrollJVCustom023Report(Input());

        var adjustment = result.Items.Single(i => i.Remark == "Salary Due to be Adjusted on Leave Resumption");
        adjustment.GLCode.ShouldBe(string.Empty);
        adjustment.Amount.ShouldBe(-200);
        adjustment.Credit.ShouldBe(200);
        adjustment.Debit.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Add_The_Adjustment_Line_When_Net_Pay_Reconciles()
    {
        var employee = _data.CreateEmployee("JV23-NOADJ");
        var register = _data.CreateRegister(employee, JuneEnd);

        var basic = _data.CreateWageType("1000");
        var netPay = _data.CreateWageType("9000");

        _data.CreateWageAccount(employee, basic, _data.CreateAccount("67001", "Salary Expense"));
        _data.CreateWageAccount(employee, netPay, _data.CreateAccount("67002", "Salary Payable"));

        _data.CreateDetail(register, basic, 1000);
        _data.CreateDetail(register, netPay, 1000);

        var result = await _appService.GetPayrollJVCustom023Report(Input());

        result.Items.ShouldNotContain(i => i.Remark == "Salary Due to be Adjusted on Leave Resumption");
    }

    /// <summary>
    /// Legacy lines 936-1008: the "Missing Accounts" format is an EXCEPTION report -- it lists the salary
    /// lines whose employee/wage type has no <c>EmployeeWageAccount</c> row for the month, and nothing
    /// else. Its column set is completely different from the Main Format's.
    /// </summary>
    [Fact]
    public async Task Should_List_Only_Unmapped_Wage_Types_In_The_Missing_Accounts_Format()
    {
        var employee = _data.CreateEmployee("JV23-MISS");
        var register = _data.CreateRegister(employee, JuneEnd);

        var mapped = _data.CreateWageType("1000");
        var unmapped = _data.CreateWageType("1002");
        _data.CreateWageAccount(employee, mapped, _data.CreateAccount("68001", "Salary Expense"));

        _data.CreateDetail(register, mapped, 1000);
        _data.CreateDetail(register, unmapped, 55);

        var result = await _appService.GetPayrollJVCustom023Report(
            Input(format: PayrollJVCustom023Format.MissingAccounts));

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe("JV23-MISS");
        row.WageTypeName.ShouldBe("WT 1002");
        row.Amount.ShouldBe(55);
        row.ForPeriod.ShouldBe(JuneEnd);
        row.AccountInfoCode.ShouldBe(string.Empty);

        result.Columns.Select(c => c.Field).ShouldBe(new[]
        {
            "lineNumber", "employeeCode", "forPeriod", "wageTypeName", "accountInfoCode",
            "accountInfoName", "offsetAccountInfoCode", "offsetAccountInfoName", "project",
            "lineOfBusiness", "vehicleMachinery", "departments", "costPercentage", "amount"
        });
    }

    /// <summary>
    /// Legacy lines 1011-1167: the same postings broken down per employee and wage type. Control Acct is
    /// blanked on this branch (legacy lines 1063/1101/1152 write <c>""</c>).
    /// </summary>
    [Fact]
    public async Task Should_Break_Postings_Down_Per_Employee_And_Wage_Type()
    {
        var account = _data.CreateAccount("69001", "Salary Expense");
        var wageType = _data.CreateWageType("1000");

        var first = _data.CreateEmployee("JV23-EW-A");
        _data.CreateWageAccount(first, wageType, account);
        _data.CreateDetail(_data.CreateRegister(first, JuneEnd), wageType, 100);

        var second = _data.CreateEmployee("JV23-EW-B");
        _data.CreateWageAccount(second, wageType, account);
        _data.CreateDetail(_data.CreateRegister(second, JuneEnd), wageType, 200);

        // Main Format collapses both employees onto the one account...
        var main = await _appService.GetPayrollJVCustom023Report(Input());
        main.Items.Single(i => i.GLCode == "69001").Debit.ShouldBe(300);

        // ...while EmployeeWise BreakDown keeps them apart.
        var breakdown = await _appService.GetPayrollJVCustom023Report(
            Input(format: PayrollJVCustom023Format.EmployeeWiseBreakDown));

        breakdown.Items.Single(i => i.EmployeeCode == "JV23-EW-A").Debit.ShouldBe(100);
        breakdown.Items.Single(i => i.EmployeeCode == "JV23-EW-B").Debit.ShouldBe(200);
        breakdown.Items.ShouldAllBe(i => i.ControlAcct == string.Empty);
    }

    /// <summary>The three formats each declare their own legacy column list.</summary>
    [Fact]
    public async Task Should_Return_The_Legacy_Column_Set_Of_The_Chosen_Format()
    {
        var main = await _appService.GetPayrollJVCustom023Report(Input());
        main.Columns.Select(c => c.Field).ShouldBe(new[]
        {
            "lineNumber", "remark", "glCode", "debit", "credit", "amount", "glDesc"
        });

        var breakdown = await _appService.GetPayrollJVCustom023Report(
            Input(format: PayrollJVCustom023Format.EmployeeWiseBreakDown));
        breakdown.Columns.Select(c => c.Field).ShouldBe(new[]
        {
            "lineNumber", "employeeCode", "wageTypeName", "glCode", "glName", "controlAcct", "debit",
            "credit", "remarksTemplate"
        });
    }

    /// <summary>
    /// ACCOUNTING DEFECT PINNED, NOT FIXED. Legacy's left join (lines 652-682) selects the RAW amount
    /// rather than <c>amount * CostPercentage / 100</c>, so an employee/wage type split across two cost
    /// centres emits the salary line twice AT FULL VALUE -- the voucher is inflated and the split
    /// percentages are ignored. The sibling Custom 017 report DOES apply the percentage (017 line 795),
    /// which is what makes this a defect rather than a design choice. Needs a customer decision.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_Defect_Where_Cost_Splits_Multiply_The_Amount()
    {
        var employee = _data.CreateEmployee("JV23-SPLIT");
        var wageType = _data.CreateWageType("1000");
        var account = _data.CreateAccount("70001", "Salary Expense");

        var costCentreA = _data.CreateCostElement("CC-A-23");
        var costCentreB = _data.CreateCostElement("CC-B-23");

        // A 60/40 split across two cost centres.
        _data.CreateWageAccount(employee, wageType, account, costElement1: costCentreA, costPercentage: 60);
        _data.CreateWageAccount(employee, wageType, account, costElement1: costCentreB, costPercentage: 40);

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 1000);

        var result = await _appService.GetPayrollJVCustom023Report(Input());

        // Correct behaviour would be 600 + 400 = 1000. Legacy posts 1000 + 1000 = 2000.
        var rows = result.Items.Where(i => i.GLCode == "70001").ToList();
        rows.Count.ShouldBe(2);
        rows.ShouldAllBe(r => r.Debit == 1000);
        rows.Sum(r => r.Debit).ShouldBe(2000);
    }

    [Fact]
    public async Task Should_Number_Rows_Continuously_And_Page_Them()
    {
        // Not about Net Pay: a wage type that does not impact net pay keeps the legacy balancing line
        // (lines 817-843, pinned by the Salary Due to be Adjusted tests) out of this fixture.
        var wageType = _data.CreateWageType("1000", dontImpactNetPay: true);

        foreach (var (code, glCode) in new[]
                 {
                     ("JV23-PG-A", "71001"), ("JV23-PG-B", "71002"), ("JV23-PG-C", "71003")
                 })
        {
            var employee = _data.CreateEmployee(code);
            _data.CreateWageAccount(employee, wageType, _data.CreateAccount(glCode, glCode));
            _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);
        }

        var secondPage = await _appService.GetPayrollJVCustom023Report(
            Input(maxResultCount: 1, skipCount: 1));

        secondPage.TotalCount.ShouldBe(3);
        secondPage.Items.Single().LineNumber.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        // Not about Net Pay: a wage type that does not impact net pay keeps the legacy balancing line
        // (lines 817-843, pinned by the Salary Due to be Adjusted tests) out of this fixture.
        var wageType = _data.CreateWageType("1000", dontImpactNetPay: true);

        foreach (var (code, glCode) in new[]
                 {
                     ("JV23-EX-A", "72001"), ("JV23-EX-B", "72002"), ("JV23-EX-C", "72003")
                 })
        {
            var employee = _data.CreateEmployee(code);
            _data.CreateWageAccount(employee, wageType, _data.CreateAccount(glCode, glCode));
            _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);
        }

        // A different month -- must not reach the export.
        var other = _data.CreateEmployee("JV23-EX-OUT");
        _data.CreateWageAccount(other, wageType, _data.CreateAccount("72999", "Out"));
        _data.CreateDetail(_data.CreateRegister(other, new DateTime(2024, 7, 31)), wageType, 100);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetPayrollJVCustom023ReportToExcel(Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom023.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
    }

    /// <summary>
    /// Every exported header of EVERY format must be a RESOLVED caption. A localization key with no XML
    /// entry renders as "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler, and grep
    /// alone has proved unreliable at catching it.
    /// </summary>
    [Theory]
    [InlineData(PayrollJVCustom023Format.MainFormat)]
    [InlineData(PayrollJVCustom023Format.MissingAccounts)]
    [InlineData(PayrollJVCustom023Format.EmployeeWiseBreakDown)]
    public async Task Should_Resolve_Every_Exported_Column_Header(PayrollJVCustom023Format format)
    {
        var employee = _data.CreateEmployee("JV23-HDR-" + (int)format);
        var wageType = _data.CreateWageType("1000");
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);

        var file = await _appService.GetPayrollJVCustom023ReportToExcel(Input(format: format));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
