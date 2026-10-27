using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Payroll JV (Custom 017) grid report (legacy <c>uscRptPayrollJV_Custom017.ascx</c>,
/// <c>Table1()</c> lines 558-1011).
///
/// Custom 017 emits a Microsoft Dynamics AX journal-import file rather than a summary, so the tests pin
/// the AX layout rules: the mandatory <c>AXBatch</c> row, the AX date format, the cost-element-to-
/// AX-dimension mapping, the Customer-account dimension shuffle, the provision offset row, the merge
/// behaviour and the closing Net Pay line -- plus the two legacy defects reproduced on purpose.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom017ReportAppService_Tests : AppTestBase
{
    private readonly IPayrollJVCustom017ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly PayrollJVReportTestDataBuilder _data;

    public PayrollJVCustom017ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom017ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private int TenantId => AbpSession.TenantId ?? 1;

    private void CreateAxBatch(string journalBatchNumber = "JB-001")
    {
        UsingDbContext(context =>
        {
            context.Set<AXBatch>().Add(new AXBatch
            {
                TenantId = TenantId,
                JournalBatchNumber = journalBatchNumber,
                Name = "Payroll",
                Description = "Payroll journal"
            });
        });
    }

    private static GetPayrollJVCustom017ReportInput Input(
        int month = 6,
        int year = 2024,
        PayrollJVCustom017Format format = PayrollJVCustom017Format.MainFormat,
        string employeeCode = null,
        PayrollRunType? runType = null,
        int? payrollAreaId = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollJVCustom017ReportInput
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
    /// Legacy lines 624-626: no <c>AXBatch</c> row is a hard error. The journal batch number is a
    /// mandatory AX import field, so an empty report would produce a file AX silently rejects.
    /// </summary>
    [Fact]
    public async Task Should_Throw_When_There_Is_No_AXBatch_Record()
    {
        await Should.ThrowAsync<UserFriendlyException>(
            () => _appService.GetPayrollJVCustom017Report(Input()));
    }

    /// <summary>
    /// Legacy lines 643-646: <c>InPeriod == ForPeriod == EndDate</c>, so only registers stamped on the
    /// requested month end and not paid as a retro run reach the journal.
    /// </summary>
    [Fact]
    public async Task Should_Only_Emit_Lines_For_The_Requested_Month_End()
    {
        CreateAxBatch();

        var wageType = _data.CreateWageType("1000");
        var account = _data.CreateAccount("10001", "Salary Expense");

        var inMonth = _data.CreateEmployee("JV17-EOM");
        _data.CreateWageAccount(inMonth, wageType, account);
        _data.CreateDetail(_data.CreateRegister(inMonth, JuneEnd), wageType, 100);

        var nextMonth = _data.CreateEmployee("JV17-JUL");
        _data.CreateWageAccount(nextMonth, wageType, account);
        _data.CreateDetail(_data.CreateRegister(nextMonth, new DateTime(2024, 7, 31)), wageType, 700);

        var retro = _data.CreateEmployee("JV17-RETRO");
        _data.CreateWageAccount(retro, wageType, account);
        _data.CreateDetail(_data.CreateRegister(retro, JuneEnd, new DateTime(2024, 7, 31)), wageType, 900);

        var result = await _appService.GetPayrollJVCustom017Report(Input());

        // One posting row plus the closing Net Pay row.
        var postings = result.Items.Where(i => i.Description != "Net Pay").ToList();
        postings.Count.ShouldBe(1);
        postings.Single().Debit.ShouldBe(100);
    }

    /// <summary>Legacy line 645: Net Pay (wage type 9000) never enters this journal.</summary>
    [Fact]
    public async Task Should_Exclude_The_Net_Pay_Wage_Type_From_The_Postings()
    {
        CreateAxBatch();

        var employee = _data.CreateEmployee("JV17-NET");
        var register = _data.CreateRegister(employee, JuneEnd);

        var basic = _data.CreateWageType("1000");
        var netPay = _data.CreateWageType("9000");

        _data.CreateWageAccount(employee, basic, _data.CreateAccount("11001", "Salary Expense"));
        _data.CreateWageAccount(employee, netPay, _data.CreateAccount("11002", "Salary Payable"));

        _data.CreateDetail(register, basic, 1000);
        _data.CreateDetail(register, netPay, 1000);

        var result = await _appService.GetPayrollJVCustom017Report(Input());

        result.Items.Select(i => i.LedgerDimensionMainAccount).ShouldNotContain("11002");
    }

    /// <summary>
    /// Legacy line 795: unlike its 019/023 siblings, Custom 017 DOES multiply the amount by the wage
    /// account's cost percentage. A 60/40 split of 1000 posts 600 and 400, not 1000 twice.
    /// </summary>
    [Fact]
    public async Task Should_Apply_The_Cost_Percentage_To_Each_Split()
    {
        CreateAxBatch();

        var employee = _data.CreateEmployee("JV17-SPLIT");
        var wageType = _data.CreateWageType("1000");
        var account = _data.CreateAccount("12001", "Salary Expense");

        _data.CreateWageAccount(employee, wageType, account,
            costElement1: _data.CreateCostElement("CO-A-17"), costPercentage: 60);
        _data.CreateWageAccount(employee, wageType, account,
            costElement1: _data.CreateCostElement("CO-B-17"), costPercentage: 40);

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 1000);

        var result = await _appService.GetPayrollJVCustom017Report(Input());

        var postings = result.Items.Where(i => i.LedgerDimensionMainAccount == "12001").ToList();
        postings.Count.ShouldBe(2);
        postings.Select(p => p.Debit).OrderBy(d => d).ShouldBe(new[] { 400d, 600d });
        postings.Sum(p => p.Debit).ShouldBe(1000);
    }

    /// <summary>
    /// ACCOUNTING DEFECT PINNED, NOT FIXED. Because the amount is
    /// <c>CostPercentage * Amount / 100</c> and the wage-account join is a LEFT join, a salary line with
    /// NO wage-account mapping comes out as zero and is then dropped by legacy's
    /// <c>Debit != 0 || Credit != 0</c> guard (line 842). Unmapped pay VANISHES from the AX file instead
    /// of surfacing as an exception, and the file still looks complete. Custom 017 has no "Missing
    /// Accounts" format to catch it -- its Type combo is inert. Needs a customer decision.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_Defect_Where_Unmapped_Wage_Types_Vanish_From_The_Journal()
    {
        CreateAxBatch();

        var employee = _data.CreateEmployee("JV17-UNMAPPED");
        var register = _data.CreateRegister(employee, JuneEnd);

        var mapped = _data.CreateWageType("1000");
        var unmapped = _data.CreateWageType("1002");
        _data.CreateWageAccount(employee, mapped, _data.CreateAccount("13001", "Salary Expense"));

        _data.CreateDetail(register, mapped, 1000);
        // 500 of real pay with no GL mapping -- silently absent from the journal below.
        _data.CreateDetail(register, unmapped, 500);

        var result = await _appService.GetPayrollJVCustom017Report(Input());

        var postings = result.Items.Where(i => i.Description != "Net Pay").ToList();
        postings.Count.ShouldBe(1);
        postings.Single().Debit.ShouldBe(1000);

        // ...and the closing Net Pay line only balances the 1000 that made it in, so the 500 is gone
        // from the voucher entirely rather than showing up as an imbalance.
        result.Items.Single(i => i.Description == "Net Pay").Credit.ShouldBe(1000);
    }

    /// <summary>
    /// LEGACY DEFECT, REPRODUCED ON PURPOSE. <c>BindType()</c> (line 241) offers four formats but the
    /// live <c>Table1()</c> never reads <c>cmbType</c> -- it always builds the AX layout. Only the
    /// execution-log parameter string and the export file name use the combo. This test exists so nobody
    /// "fixes" it into a real branch without a customer decision.
    /// </summary>
    [Fact]
    public async Task Should_Ignore_The_Format_Combo_Because_Legacy_Table1_Never_Reads_It()
    {
        CreateAxBatch();

        var employee = _data.CreateEmployee("JV17-FMT");
        var wageType = _data.CreateWageType("1000");
        _data.CreateWageAccount(employee, wageType, _data.CreateAccount("14001", "Salary Expense"));
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);

        var main = await _appService.GetPayrollJVCustom017Report(Input());
        var missing = await _appService.GetPayrollJVCustom017Report(
            Input(format: PayrollJVCustom017Format.MissingAccounts));

        missing.TotalCount.ShouldBe(main.TotalCount);
        missing.Columns.Select(c => c.Field).ShouldBe(main.Columns.Select(c => c.Field));
    }

    /// <summary>
    /// Legacy lines 799/812-817: cost elements 1-7 map onto the AX dimensions in a fixed order that is
    /// NOT the same as the 019/023 mapping.
    /// </summary>
    [Fact]
    public async Task Should_Map_Cost_Elements_One_To_Seven_Onto_The_Ax_Dimensions()
    {
        CreateAxBatch();

        var employee = _data.CreateEmployee("JV17-DIMS");
        var wageType = _data.CreateWageType("1000");

        _data.CreateWageAccount(employee, wageType, _data.CreateAccount("15001", "Salary Expense"),
            costElement1: _data.CreateCostElement("CE1-17"),
            costElement2: _data.CreateCostElement("CE2-17"),
            costElement3: _data.CreateCostElement("CE3-17"),
            costElement4: _data.CreateCostElement("CE4-17"));

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 1000);

        var result = await _appService.GetPayrollJVCustom017Report(Input());

        var row = result.Items.Single(i => i.LedgerDimensionMainAccount == "15001");
        row.CompanyAccounts.ShouldBe("CE1-17");
        row.LedgerDimensionLocation.ShouldBe("CE2-17");
        row.LedgerDimensionBrand.ShouldBe("CE3-17");
        row.LedgerDimensionDepartment.ShouldBe("CE4-17");
    }

    /// <summary>Legacy line 54: AX wants the posting date as "MM/dd/yyyy" TEXT, not a real date.</summary>
    [Fact]
    public async Task Should_Emit_The_Posting_Date_In_The_Ax_Text_Format()
    {
        CreateAxBatch();

        var employee = _data.CreateEmployee("JV17-DATE");
        var wageType = _data.CreateWageType("1000");
        _data.CreateWageAccount(employee, wageType, _data.CreateAccount("16001", "Salary Expense"));
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);

        var result = await _appService.GetPayrollJVCustom017Report(Input());

        result.Items.First().LineNumberDate.ShouldBe("06/30/2024");
    }

    /// <summary>
    /// Legacy lines 880-919: a provision line gets a SECOND row against the offset account with Debit
    /// and Credit swapped -- the pair that makes the provision balance.
    /// </summary>
    [Fact]
    public async Task Should_Emit_An_Offset_Row_For_Each_Provision_Line()
    {
        CreateAxBatch();

        var employee = _data.CreateEmployee("JV17-PROV");
        var provision = _data.CreateWageType("1066", BillingType.Provision);

        _data.CreateWageAccount(employee, provision,
            _data.CreateAccount("17001", "Provision Expense"),
            offsetAccount: _data.CreateAccount("17002", "Provision Liability"));

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), provision, 300);

        var result = await _appService.GetPayrollJVCustom017Report(Input());

        var main = result.Items.Single(i => i.LedgerDimensionMainAccount == "17001");
        main.Debit.ShouldBe(300);
        main.Credit.ShouldBe(0);
        // Legacy lines 634-638 relabel the two increment wage types as "Provision".
        main.BillingType.ShouldBe("Provision");

        var offset = result.Items.Single(i => i.LedgerDimensionMainAccount == "17002");
        offset.Debit.ShouldBe(0);
        offset.Credit.ShouldBe(300);

        // Sequential AX line numbers across the pair.
        offset.RecId.ShouldBe(main.RecId + 1);
    }

    /// <summary>
    /// Legacy lines 973-1000: a closing Net Pay CREDIT for the total of every non-provision
    /// Earning/Deduction line, booked against the selected payroll area's FI bank and currency.
    /// </summary>
    [Fact]
    public async Task Should_Close_The_Journal_With_A_Net_Pay_Credit_On_The_Payroll_Areas_Fi_Bank()
    {
        CreateAxBatch("JB-NET");

        var payrollArea = _data.CreatePayrollArea("PA-17", "Head Office");
        var fiBank = _data.CreateAccount("18999", "Payroll Bank");

        UsingDbContext(context =>
        {
            var area = context.Set<PayrollArea>().Find(payrollArea.Id);
            area.FIBankId = fiBank.Id;
        });

        var employee = _data.CreateEmployee("JV17-CLOSE");
        _data.CreateOrgAssignment(employee, payrollArea, new DateTime(2024, 1, 1), new DateTime(2099, 12, 31));

        var earning = _data.CreateWageType("1000");
        var deduction = _data.CreateWageType("2001", BillingType.Deduction);

        _data.CreateWageAccount(employee, earning, _data.CreateAccount("18001", "Salary Expense"));
        _data.CreateWageAccount(employee, deduction, _data.CreateAccount("18002", "Loan Recovery"));

        var register = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(register, earning, 1000);
        _data.CreateDetail(register, deduction, 150);

        var result = await _appService.GetPayrollJVCustom017Report(Input(payrollAreaId: payrollArea.Id));

        var netPay = result.Items.Single(i => i.Description == "Net Pay");
        netPay.Credit.ShouldBe(850);
        netPay.Debit.ShouldBe(0);
        netPay.LedgerDimensionMainAccount.ShouldBe("18999");
        netPay.JournalBatchNumber.ShouldBe("JB-NET");
        netPay.BillingType.ShouldBe("Earning");
    }

    /// <summary>Legacy line 979: provision lines are excluded from the closing Net Pay total.</summary>
    [Fact]
    public async Task Should_Exclude_Provisions_From_The_Closing_Net_Pay_Total()
    {
        CreateAxBatch();

        var employee = _data.CreateEmployee("JV17-NOPROV");
        var register = _data.CreateRegister(employee, JuneEnd);

        var earning = _data.CreateWageType("1000");
        var provision = _data.CreateWageType("1066", BillingType.Provision);

        _data.CreateWageAccount(employee, earning, _data.CreateAccount("19001", "Salary Expense"));
        _data.CreateWageAccount(employee, provision,
            _data.CreateAccount("19002", "Provision Expense"),
            offsetAccount: _data.CreateAccount("19003", "Provision Liability"));

        _data.CreateDetail(register, earning, 1000);
        _data.CreateDetail(register, provision, 300);

        var result = await _appService.GetPayrollJVCustom017Report(Input());

        result.Items.Single(i => i.Description == "Net Pay").Credit.ShouldBe(1000);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Code_And_Run_Type()
    {
        CreateAxBatch();

        var wageType = _data.CreateWageType("1000");

        var kept = _data.CreateEmployee("JV17-F-A");
        _data.CreateWageAccount(kept, wageType, _data.CreateAccount("20001", "Kept"));
        _data.CreateDetail(_data.CreateRegister(kept, JuneEnd), wageType, 100);

        var dropped = _data.CreateEmployee("JV17-F-B");
        _data.CreateWageAccount(dropped, wageType, _data.CreateAccount("20002", "Dropped"));
        _data.CreateDetail(_data.CreateRegister(dropped, JuneEnd), wageType, 200);

        var byCode = await _appService.GetPayrollJVCustom017Report(Input(employeeCode: "JV17-F-A"));
        byCode.Items.Select(i => i.LedgerDimensionMainAccount).ShouldNotContain("20002");

        var offCycle = _data.CreateEmployee("JV17-F-OFF");
        _data.CreateWageAccount(offCycle, wageType, _data.CreateAccount("20003", "OffCycle"));
        _data.CreateDetail(
            _data.CreateRegister(offCycle, JuneEnd, runType: PayrollRunType.OffCycle), wageType, 300);

        var byRunType = await _appService.GetPayrollJVCustom017Report(
            Input(runType: PayrollRunType.OffCycle));
        byRunType.Items.Select(i => i.LedgerDimensionMainAccount).ShouldContain("20003");
        byRunType.Items.Select(i => i.LedgerDimensionMainAccount).ShouldNotContain("20001");
    }

    [Fact]
    public async Task Should_Number_Rows_Continuously_And_Page_Them()
    {
        CreateAxBatch();

        var wageType = _data.CreateWageType("1000");

        foreach (var (code, glCode) in new[]
                 {
                     ("JV17-PG-A", "21001"), ("JV17-PG-B", "21002"), ("JV17-PG-C", "21003")
                 })
        {
            var employee = _data.CreateEmployee(code);
            _data.CreateWageAccount(employee, wageType, _data.CreateAccount(glCode, glCode));
            _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);
        }

        // Three postings plus the closing Net Pay row.
        var secondPage = await _appService.GetPayrollJVCustom017Report(
            Input(maxResultCount: 1, skipCount: 1));

        secondPage.TotalCount.ShouldBe(4);
        secondPage.Items.Single().LineNumber.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        CreateAxBatch();

        var wageType = _data.CreateWageType("1000");

        foreach (var (code, glCode) in new[]
                 {
                     ("JV17-EX-A", "22001"), ("JV17-EX-B", "22002"), ("JV17-EX-C", "22003")
                 })
        {
            var employee = _data.CreateEmployee(code);
            _data.CreateWageAccount(employee, wageType, _data.CreateAccount(glCode, glCode));
            _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);
        }

        // A different month -- must not reach the export.
        var other = _data.CreateEmployee("JV17-EX-OUT");
        _data.CreateWageAccount(other, wageType, _data.CreateAccount("22999", "Out"));
        _data.CreateDetail(_data.CreateRegister(other, new DateTime(2024, 7, 31)), wageType, 100);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetPayrollJVCustom017ReportToExcel(Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom017.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        // Three postings plus the closing Net Pay row.
        exported.Count.ShouldBe(4);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        CreateAxBatch();

        var employee = _data.CreateEmployee("JV17-HDR");
        var wageType = _data.CreateWageType("1000");
        _data.CreateWageAccount(employee, wageType, _data.CreateAccount("23001", "Salary Expense"));
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), wageType, 100);

        var file = await _appService.GetPayrollJVCustom017ReportToExcel(Input());

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
