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
/// Tests for the Payroll JV (Custom 019) grid report (legacy <c>uscRptPayrollJV_Custom019.ascx</c>,
/// <c>Table1()</c> lines 558-1296).
///
/// Custom 019 is Custom 023's sibling -- the same wage-account-driven voucher -- plus three
/// customer-specific behaviours the tests concentrate on: the LIVE "Provision Format" branch, the
/// Sub-Contractors payroll-area toggle, and the synthetic "Last Month Recovery Ded" line generated for
/// employees whose Net Pay came out as zero. The two legacy accounting defects that are reproduced on
/// purpose are pinned here too.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom019ReportAppService_Tests : AppTestBase
{
    private readonly IPayrollJVCustom019ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly PayrollJVReportTestDataBuilder _data;

    public PayrollJVCustom019ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom019ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private static GetPayrollJVCustom019ReportInput Input(
        int month = 6,
        int year = 2024,
        PayrollJVCustom019Format format = PayrollJVCustom019Format.MainFormat,
        string employeeCode = null,
        PayrollRunType? runType = null,
        PayrollJVCustom019PayrollAreaScope? payrollAreaScope = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollJVCustom019ReportInput
        {
            Month = month,
            Year = year,
            Format = format,
            EmployeeCode = employeeCode,
            PayrollRunType = runType,
            PayrollAreaScope = payrollAreaScope,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    /// <summary>
    /// Legacy <c>GetTable()</c> builds the period from month/year and <c>Table1()</c> matches
    /// <c>ForPeriod == EndDate</c> exactly, so only a register stamped on the LAST day of the requested
    /// month contributes.
    /// </summary>
    [Fact]
    public async Task Should_Only_Aggregate_Registers_Stamped_On_The_Requested_Month_End()
    {
        var wageType = _data.CreateWageType("1000");
        var account = _data.CreateAccount("80001", "Salary Expense");

        var inMonth = _data.CreateEmployee("JV19-EOM");
        _data.CreateWageAccount(inMonth, wageType, account);
        _data.CreateDetail(_data.CreateRegister(inMonth, JuneEnd), wageType, 100);

        var midMonth = _data.CreateEmployee("JV19-MID");
        _data.CreateWageAccount(midMonth, wageType, account);
        _data.CreateDetail(_data.CreateRegister(midMonth, new DateTime(2024, 6, 15)), wageType, 500);

        var nextMonth = _data.CreateEmployee("JV19-JUL");
        _data.CreateWageAccount(nextMonth, wageType, account);
        _data.CreateDetail(_data.CreateRegister(nextMonth, new DateTime(2024, 7, 31)), wageType, 700);

        var result = await _appService.GetPayrollJVCustom019Report(Input());

        result.Items.Single(i => i.GLCode == "80001").Debit.ShouldBe(100);
    }

    /// <summary>Legacy line 570: <c>ForPeriod == InPeriod</c> excludes retro runs paid in a later month.</summary>
    [Fact]
    public async Task Should_Exclude_Retro_Runs_Where_InPeriod_Differs_From_ForPeriod()
    {
        var wageType = _data.CreateWageType("1000");
        var account = _data.CreateAccount("80002", "Salary Expense");

        var inPeriod = _data.CreateEmployee("JV19-IN");
        _data.CreateWageAccount(inPeriod, wageType, account);
        _data.CreateDetail(_data.CreateRegister(inPeriod, JuneEnd), wageType, 100);

        var retro = _data.CreateEmployee("JV19-RETRO");
        _data.CreateWageAccount(retro, wageType, account);
        _data.CreateDetail(_data.CreateRegister(retro, JuneEnd, new DateTime(2024, 7, 31)), wageType, 900);

        var result = await _appService.GetPayrollJVCustom019Report(Input());

        result.Items.Single(i => i.GLCode == "80002").Debit.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Code_List()
    {
        var wageType = _data.CreateWageType("1000");

        foreach (var (code, glCode) in new[]
                 {
                     ("JV19-C-A", "81001"), ("JV19-C-B", "81002"), ("JV19-C-C", "81003")
                 })
        {
            var employee = _data.CreateEmployee(code);
            _data.CreateWageAccount(employee, wageType, _data.CreateAccount(glCode, glCode));
            _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);
        }

        var result = await _appService.GetPayrollJVCustom019Report(
            Input(employeeCode: "JV19-C-A , JV19-C-C"));

        var glCodes = result.Items.Select(i => i.GLCode).ToList();
        glCodes.ShouldContain("81001");
        glCodes.ShouldNotContain("81002");
        glCodes.ShouldContain("81003");
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Run_Type_When_Supplied()
    {
        var wageType = _data.CreateWageType("1000");

        var regular = _data.CreateEmployee("JV19-RT-REG");
        _data.CreateWageAccount(regular, wageType, _data.CreateAccount("82001", "Regular"));
        _data.CreateDetail(_data.CreateRegister(regular, JuneEnd), wageType, 100);

        var offCycle = _data.CreateEmployee("JV19-RT-OFF");
        _data.CreateWageAccount(offCycle, wageType, _data.CreateAccount("82002", "OffCycle"));
        _data.CreateDetail(
            _data.CreateRegister(offCycle, JuneEnd, runType: PayrollRunType.OffCycle), wageType, 250);

        var result = await _appService.GetPayrollJVCustom019Report(
            Input(runType: PayrollRunType.OffCycle));

        result.Items.Select(i => i.GLCode).ShouldNotContain("82001");
        result.Items.Single(i => i.GLCode == "82002").Debit.ShouldBe(250);
    }

    /// <summary>
    /// Legacy lines 590-614: the Payroll Area combo is NOT a payroll-area picker. Code "0" means every
    /// area whose NAME is not "Sub-Contractors" and code "1" means only that one -- a hard-coded name
    /// test unique to this customer.
    /// </summary>
    [Fact]
    public async Task Should_Treat_The_Payroll_Area_Combo_As_A_Sub_Contractors_Toggle()
    {
        var wageType = _data.CreateWageType("1000");
        var subContractors = _data.CreatePayrollArea("PA-SUB-19", "Sub-Contractors");
        var staff = _data.CreatePayrollArea("PA-STAFF-19", "Head Office");

        var subEmployee = _data.CreateEmployee("JV19-SUB");
        _data.CreateOrgAssignment(subEmployee, subContractors, new DateTime(2024, 1, 1), new DateTime(2099, 12, 31));
        _data.CreateWageAccount(subEmployee, wageType, _data.CreateAccount("83001", "Sub"));
        _data.CreateDetail(_data.CreateRegister(subEmployee, JuneEnd), wageType, 100);

        var staffEmployee = _data.CreateEmployee("JV19-STAFF");
        _data.CreateOrgAssignment(staffEmployee, staff, new DateTime(2024, 1, 1), new DateTime(2099, 12, 31));
        _data.CreateWageAccount(staffEmployee, wageType, _data.CreateAccount("83002", "Staff"));
        _data.CreateDetail(_data.CreateRegister(staffEmployee, JuneEnd), wageType, 200);

        var subOnly = await _appService.GetPayrollJVCustom019Report(
            Input(payrollAreaScope: PayrollJVCustom019PayrollAreaScope.SubContractorsOnly));
        subOnly.Items.Select(i => i.GLCode).ShouldBe(new[] { "83001" });

        var everythingElse = await _appService.GetPayrollJVCustom019Report(
            Input(payrollAreaScope: PayrollJVCustom019PayrollAreaScope.AllExceptSubContractors));
        everythingElse.Items.Select(i => i.GLCode).ShouldBe(new[] { "83002" });
    }

    /// <summary>
    /// Legacy lines 562-566: unlike Custom 023, provision wage types 3001 and 3000 are admitted
    /// alongside Earning/Deduction so the Provision Format branch has something to post.
    /// </summary>
    [Fact]
    public async Task Should_Post_Provisions_As_An_Account_And_Offset_Account_Pair()
    {
        var employee = _data.CreateEmployee("JV19-PROV");
        var register = _data.CreateRegister(employee, JuneEnd);

        var leaveProvision = _data.CreateWageType("3001", BillingType.Provision);
        var expenseAccount = _data.CreateAccount("84001", "Leave Provision Expense");
        var liabilityAccount = _data.CreateAccount("84002", "Leave Provision Liability");

        _data.CreateWageAccount(employee, leaveProvision, expenseAccount, offsetAccount: liabilityAccount);
        _data.CreateDetail(register, leaveProvision, 300);

        var result = await _appService.GetPayrollJVCustom019Report(
            Input(format: PayrollJVCustom019Format.ProvisionFormat));

        result.Items.Count.ShouldBe(2);

        var debit = result.Items.Single(i => i.GLCode == "84001");
        debit.Debit.ShouldBe(300);
        debit.Credit.ShouldBe(0);
        // Provision Format is the ONLY branch on which legacy fills Control Acct (lines 1005/1024).
        debit.ControlAcct.ShouldBe("84001");

        var credit = result.Items.Single(i => i.GLCode == "84002");
        credit.Debit.ShouldBe(0);
        credit.Credit.ShouldBe(300);
        credit.GLName.ShouldBe("Leave Provision Liability");
        credit.ControlAcct.ShouldBe("84002");
    }

    /// <summary>
    /// Legacy lines 644-706. When an employee's Net Pay line is exactly 0, legacy sums every
    /// net-pay-impacting Earning/Deduction line of that register (deductions negative) and books the
    /// total against wage type 2011 "Last Month Recovery Ded" -- an extra posting that exists in no
    /// other JV variant.
    /// </summary>
    [Fact]
    public async Task Should_Generate_A_Last_Month_Recovery_Line_For_Registers_Whose_Net_Pay_Is_Zero()
    {
        var employee = _data.CreateEmployee("JV19-ZERO");
        var register = _data.CreateRegister(employee, JuneEnd);

        var basic = _data.CreateWageType("1000");
        var deduction = _data.CreateWageType("2005", BillingType.Deduction);
        var netPay = _data.CreateWageType("9000");
        var recovery = _data.CreateWageType("2011", BillingType.Deduction);

        _data.CreateWageAccount(employee, basic, _data.CreateAccount("85001", "Salary Expense"));
        _data.CreateWageAccount(employee, deduction, _data.CreateAccount("85002", "Deduction"));
        _data.CreateWageAccount(employee, netPay, _data.CreateAccount("85003", "Salary Payable"));
        _data.CreateWageAccount(employee, recovery, _data.CreateAccount("85004", "Last Month Recovery"));

        _data.CreateDetail(register, basic, 1000);
        _data.CreateDetail(register, deduction, 1000);
        // Net Pay is exactly zero -- the trigger.
        _data.CreateDetail(register, netPay, 0);

        var result = await _appService.GetPayrollJVCustom019Report(
            Input(format: PayrollJVCustom019Format.EmployeeWiseBreakDown));

        // Earnings 1000 minus deductions 1000 = 0, so the synthetic line nets to zero and legacy's
        // "amount != 0" guard drops it; the ordinary postings are still there.
        result.Items.Select(i => i.GLCode).ShouldContain("85001");
        result.Items.Select(i => i.GLCode).ShouldNotContain("85004");
    }

    /// <summary>
    /// The same trigger, but with a non-zero recovery total so the synthetic line actually surfaces:
    /// legacy re-reads the register WITHOUT the <c>IncludeInFIPosting</c> restriction the main query
    /// applies (line 658), so a non-posting wage type still feeds the recovery total.
    /// </summary>
    [Fact]
    public async Task Should_Include_Non_FI_Posting_Wage_Types_In_The_Last_Month_Recovery_Total()
    {
        var employee = _data.CreateEmployee("JV19-ZERO2");
        var register = _data.CreateRegister(employee, JuneEnd);

        var netPay = _data.CreateWageType("9000");
        // Deliberately NOT flagged for FI posting -- excluded from the main query, included in the
        // recovery total.
        var nonPosting = _data.CreateWageType("1500", includeInFiPosting: false);
        var recovery = _data.CreateWageType("2011", BillingType.Deduction);

        _data.CreateWageAccount(employee, netPay, _data.CreateAccount("86001", "Salary Payable"));
        _data.CreateWageAccount(employee, recovery, _data.CreateAccount("86002", "Last Month Recovery"));

        _data.CreateDetail(register, nonPosting, 450);
        _data.CreateDetail(register, netPay, 0);

        var result = await _appService.GetPayrollJVCustom019Report(
            Input(format: PayrollJVCustom019Format.EmployeeWiseBreakDown));

        // Wage type 2011 is a Deduction, so 450 lands in the Credit column of the recovery account.
        var recoveryRow = result.Items.Single(i => i.GLCode == "86002");
        recoveryRow.Credit.ShouldBe(450);
        recoveryRow.EmployeeCode.ShouldBe("JV19-ZERO2");

        // The non-posting wage type itself never appears as a posting.
        result.Items.ShouldNotContain(i => i.WageTypeName == "WT 1500");
    }

    /// <summary>
    /// ACCOUNTING DEFECT PINNED, NOT FIXED. All four formats declare a "Vehicle/Machinery" column and
    /// group by <c>CostElement2</c>, but legacy only WRITES it on the "Missing Accounts" branch
    /// (line 1098). On the three POSTING branches it is always blank -- a cost dimension silently
    /// dropped from a journal voucher that goes to finance. Needs a customer decision.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_Defect_Where_Vehicle_Machinery_Is_Blank_On_Posting_Formats()
    {
        var employee = _data.CreateEmployee("JV19-VEH");
        var wageType = _data.CreateWageType("1000");
        var unmapped = _data.CreateWageType("1002");
        var account = _data.CreateAccount("87001", "Salary Expense");

        var vehicle = _data.CreateCostElement("VEH-19");
        var department = _data.CreateCostElement("DEPT-19");

        _data.CreateWageAccount(employee, wageType, account,
            costElement2: vehicle, costElement3: department);

        var register = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(register, wageType, 1000);
        _data.CreateDetail(register, unmapped, 25);

        // Main Format: Departments comes through, Vehicle/Machinery does NOT.
        var main = await _appService.GetPayrollJVCustom019Report(Input());
        var posting = main.Items.Single(i => i.GLCode == "87001");
        posting.Departments.ShouldBe("DEPT-19");
        posting.VehicleMachinery.ShouldBe(string.Empty);

        // Missing Accounts: the only branch where legacy fills it (though the unmapped line has no cost
        // elements at all, so it is empty here too -- the column exists and is written).
        var missing = await _appService.GetPayrollJVCustom019Report(
            Input(format: PayrollJVCustom019Format.MissingAccounts));
        missing.Items.Single().WageTypeName.ShouldBe("WT 1002");
    }

    /// <summary>
    /// ACCOUNTING DEFECT PINNED, NOT FIXED -- shared with Custom 023: cost percentages are ignored and a
    /// cost split emits the salary line once per split AT FULL VALUE. See
    /// <c>PayrollJVPostingLineLoader</c>.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_Defect_Where_Cost_Splits_Multiply_The_Amount()
    {
        var employee = _data.CreateEmployee("JV19-SPLIT");
        var wageType = _data.CreateWageType("1000");
        var account = _data.CreateAccount("88001", "Salary Expense");

        _data.CreateWageAccount(employee, wageType, account,
            costElement1: _data.CreateCostElement("CC-A-19"), costPercentage: 60);
        _data.CreateWageAccount(employee, wageType, account,
            costElement1: _data.CreateCostElement("CC-B-19"), costPercentage: 40);

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 1000);

        var result = await _appService.GetPayrollJVCustom019Report(Input());

        // Correct behaviour would be 600 + 400 = 1000. Legacy posts 1000 + 1000 = 2000.
        var rows = result.Items.Where(i => i.GLCode == "88001").ToList();
        rows.Count.ShouldBe(2);
        rows.Sum(r => r.Debit).ShouldBe(2000);
    }

    /// <summary>The four formats each declare their own legacy column list.</summary>
    [Fact]
    public async Task Should_Return_The_Legacy_Column_Set_Of_The_Chosen_Format()
    {
        var main = await _appService.GetPayrollJVCustom019Report(Input());
        main.Columns.Select(c => c.Field).ShouldBe(new[]
        {
            "lineNumber", "glCode", "glName", "controlAcct", "debit", "credit", "remarksTemplate",
            "project", "lineOfBusiness", "vehicleMachinery", "departments"
        });

        // Provision Format declares the SAME list as Main Format (legacy lines 953-969).
        var provision = await _appService.GetPayrollJVCustom019Report(
            Input(format: PayrollJVCustom019Format.ProvisionFormat));
        provision.Columns.Select(c => c.Field).ShouldBe(main.Columns.Select(c => c.Field));

        var missing = await _appService.GetPayrollJVCustom019Report(
            Input(format: PayrollJVCustom019Format.MissingAccounts));
        missing.Columns.Select(c => c.Field).ShouldBe(new[]
        {
            "lineNumber", "employeeCode", "forPeriod", "wageTypeName", "accountInfoCode",
            "accountInfoName", "offsetAccountInfoCode", "offsetAccountInfoName", "project",
            "lineOfBusiness", "vehicleMachinery", "departments", "costPercentage", "amount"
        });
    }

    [Fact]
    public async Task Should_Number_Rows_Continuously_And_Page_Them()
    {
        var wageType = _data.CreateWageType("1000");

        foreach (var (code, glCode) in new[]
                 {
                     ("JV19-PG-A", "89001"), ("JV19-PG-B", "89002"), ("JV19-PG-C", "89003")
                 })
        {
            var employee = _data.CreateEmployee(code);
            _data.CreateWageAccount(employee, wageType, _data.CreateAccount(glCode, glCode));
            _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);
        }

        var secondPage = await _appService.GetPayrollJVCustom019Report(
            Input(maxResultCount: 1, skipCount: 1));

        secondPage.TotalCount.ShouldBe(3);
        secondPage.Items.Single().LineNumber.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var wageType = _data.CreateWageType("1000");

        foreach (var (code, glCode) in new[]
                 {
                     ("JV19-EX-A", "90001"), ("JV19-EX-B", "90002"), ("JV19-EX-C", "90003")
                 })
        {
            var employee = _data.CreateEmployee(code);
            _data.CreateWageAccount(employee, wageType, _data.CreateAccount(glCode, glCode));
            _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);
        }

        // A different month -- must not reach the export.
        var other = _data.CreateEmployee("JV19-EX-OUT");
        _data.CreateWageAccount(other, wageType, _data.CreateAccount("90999", "Out"));
        _data.CreateDetail(_data.CreateRegister(other, new DateTime(2024, 7, 31)), wageType, 100);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetPayrollJVCustom019ReportToExcel(Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom019.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
    }

    /// <summary>
    /// Every exported header of EVERY format must be a RESOLVED caption. A localization key with no XML
    /// entry renders as "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Theory]
    [InlineData(PayrollJVCustom019Format.MainFormat)]
    [InlineData(PayrollJVCustom019Format.ProvisionFormat)]
    [InlineData(PayrollJVCustom019Format.MissingAccounts)]
    [InlineData(PayrollJVCustom019Format.EmployeeWiseBreakDown)]
    public async Task Should_Resolve_Every_Exported_Column_Header(PayrollJVCustom019Format format)
    {
        var employee = _data.CreateEmployee("JV19-HDR-" + (int)format);
        var wageType = _data.CreateWageType("1000");
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);

        var file = await _appService.GetPayrollJVCustom019ReportToExcel(Input(format: format));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
