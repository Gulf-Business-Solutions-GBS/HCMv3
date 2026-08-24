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
/// Tests for the Payroll JV (Custom 035) grid report (legacy <c>uscRptPayrollJV_Custom035.ascx</c>,
/// <c>Table1()</c> lines 558-1224).
///
/// Three sheets behind one screen. Besides the shared preamble the tests cover this customer's two
/// distinguishing behaviours -- the concatenated Account string built from four cost-element slots, and the
/// trailing Net Salary bank row -- and PIN the four legacy accounting defects the port reproduces.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom035ReportAppService_Tests : AppTestBase
{
    private readonly IPayrollJVCustom035ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly PayrollJVReportTestDataBuilder _data;

    public PayrollJVCustom035ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom035ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private static GetPayrollJVCustom035ReportInput Input(
        PayrollJVCustom035ReportType reportType = PayrollJVCustom035ReportType.MainFormat,
        int month = 6,
        int year = 2024,
        int? payrollAreaId = null,
        PayrollRunType? runType = null,
        string employeeCode = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollJVCustom035ReportInput
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

    /// <summary>
    /// Gives the payroll area its FI company and FI bank -- the two masters this screen reads off it
    /// (legacy lines 692, 844-849).
    /// </summary>
    private PayrollArea CreatePayrollAreaWithFiMasters(string code)
    {
        var payrollArea = _data.CreatePayrollArea(code, "Area " + code);
        var company = _data.CreateCostElement("CO-" + code);
        var bank = _data.CreateAccount("1000-" + code, "Payroll Bank");

        UsingDbContext(context =>
        {
            var tracked = context.Set<PayrollArea>().Find(payrollArea.Id);
            tracked.FICompanyId = company.Id;
            tracked.FIBankId = bank.Id;
        });

        return payrollArea;
    }

    [Fact]
    public async Task Should_Build_The_Account_String_From_The_Four_CostElement_Slots()
    {
        var employee = _data.CreateEmployee("JV35-ACC");
        var basic = _data.CreateWageType("1000");

        // Legacy slots on this control: CostElement1 = Department, 2 = StaffCodes, 3 = DepartmentAllocation,
        // 7 = Sector. The builder does not expose slot 7, so Sector stays unmapped here.
        _data.CreateWageAccount(
            employee,
            basic,
            _data.CreateAccount("5100", "Basic Salary Expense"),
            costElement1: _data.CreateCostElement("DEP"),
            costElement2: _data.CreateCostElement("STF"),
            costElement3: _data.CreateCostElement("ALO"));

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 500);

        var rows = (await _appService.GetPayrollJVCustom035Report(Input())).Items.ToList();
        var debitRow = rows.First();

        debitRow.Values["Account"].ShouldBe("5100-DEP-STF-ALO----");
        debitRow.Values["Description"].ShouldBe("Jun-2024-Basic Salary Expense");
        debitRow.Values["Date"].ShouldBe("30/06/2024");
        debitRow.Amounts["Debit"].ShouldBe(500);
        debitRow.Amounts["Credit"].ShouldBe(0);
        debitRow.Values["Payment status"].ShouldBe("None");
    }

    [Fact]
    public async Task Should_Resolve_Period_To_The_Last_Day_Of_The_Month_And_Exclude_The_Neighbouring_Months()
    {
        var basic = _data.CreateWageType("1000");

        var inside = _data.CreateEmployee("JV35-IN");
        _data.CreateDetail(_data.CreateRegister(inside, JuneEnd), basic, 100);

        var before = _data.CreateEmployee("JV35-BEFORE");
        _data.CreateDetail(_data.CreateRegister(before, new DateTime(2024, 5, 31)), basic, 100);

        var after = _data.CreateEmployee("JV35-AFTER");
        _data.CreateDetail(_data.CreateRegister(after, new DateTime(2024, 7, 31)), basic, 100);

        var retro = _data.CreateEmployee("JV35-RETRO");
        _data.CreateDetail(_data.CreateRegister(retro, JuneEnd, new DateTime(2024, 7, 31)), basic, 100);

        var result = await _appService.GetPayrollJVCustom035Report(Input());

        // One earning row (the June employee) plus the unconditional Net Salary row.
        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Exclude_Provision_WageTypes_Unlike_Its_Custom045_Sibling()
    {
        var employee = _data.CreateEmployee("JV35-PROV");
        var basic = _data.CreateWageType("1000");
        var provision = _data.CreateWageType("3000", BillingType.Provision);

        var register = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(register, basic, 100);
        _data.CreateDetail(register, provision, 999);

        var rows = (await _appService.GetPayrollJVCustom035Report(Input())).Items.ToList();

        // The earning row plus the Net Salary row -- the provision never enters the working set.
        rows.Count.ShouldBe(2);
        rows.First().Amounts["Debit"].ShouldBe(100);
    }

    [Fact]
    public async Task Should_Fill_Company_And_Bank_From_The_Selected_PayrollArea()
    {
        var payrollArea = CreatePayrollAreaWithFiMasters("PA35");
        var employee = _data.CreateEmployee("JV35-PA");
        _data.CreateOrgAssignment(employee, payrollArea, new DateTime(2020, 1, 1), new DateTime(2099, 12, 31));

        var basic = _data.CreateWageType("1000");
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 100);

        var rows = (await _appService.GetPayrollJVCustom035Report(
            Input(payrollAreaId: payrollArea.Id))).Items.ToList();

        rows.Count.ShouldBe(2);
        rows.First().Values["Company accounts"].ShouldBe("CO-PA35");

        var bankRow = rows.Last();
        bankRow.Values["Account"].ShouldBe("1000-PA35");
        bankRow.Values["Account type"].ShouldBe("Ledger");
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. The Credit Entry loop takes EVERY Deduction wage type with no exclusion for
    /// the net-pay code (line 800), and the Net Salary row then credits the bank with the same wage type
    /// 9000 again (line 843). Net pay is therefore credited TWICE and the voucher does not foot.
    /// </summary>
    [Fact]
    public async Task Should_Credit_NetPay_Twice_Reproducing_Legacy_Defect()
    {
        var employee = _data.CreateEmployee("JV35-NET2");
        var basic = _data.CreateWageType("1000");
        // Net pay is modelled the way this customer's master data does: a Deduction that posts to FI.
        var netPay = _data.CreateWageType("9000", BillingType.Deduction);

        var register = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(register, basic, 500);
        _data.CreateDetail(register, netPay, 500);

        var rows = (await _appService.GetPayrollJVCustom035Report(Input())).Items.ToList();

        var totalDebit = rows.Sum(r => r.Amounts["Debit"]);
        var totalCredit = rows.Sum(r => r.Amounts["Credit"]);

        totalDebit.ShouldBe(500);
        // 500 from the Deduction grouping AND 500 again from the bank row.
        totalCredit.ShouldBe(1000);
        totalCredit.ShouldNotBe(totalDebit);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. The Net Salary row is appended outside any conditional (lines 838-869), so an
    /// empty period still yields one voucher line -- with a blank account when no payroll area was chosen.
    /// </summary>
    [Fact]
    public async Task Should_Emit_An_Empty_NetSalary_Row_For_An_Empty_Period_Reproducing_Legacy_Defect()
    {
        var result = await _appService.GetPayrollJVCustom035Report(Input(month: 1, year: 2001));

        result.TotalCount.ShouldBe(1);

        var row = result.Items.Single();
        row.Values["Account"].ShouldBe(string.Empty);
        row.Values["Company accounts"].ShouldBe(string.Empty);
        row.Amounts["Debit"].ShouldBe(0);
        row.Amounts["Credit"].ShouldBe(0);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. EmployeeWise BreakDown is a per-employee sheet, yet its trailing bank row is
    /// emitted with no EmpCode and no WageType (lines 1196-1220).
    /// </summary>
    [Fact]
    public async Task Should_Emit_The_BreakDown_NetSalary_Row_Without_An_Employee_Reproducing_Legacy_Defect()
    {
        var employee = _data.CreateEmployee("JV35-BD");
        var basic = _data.CreateWageType("1000");
        _data.CreateWageAccount(employee, basic, _data.CreateAccount("5100", "Basic"));
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 100);

        var rows = (await _appService.GetPayrollJVCustom035Report(
            Input(PayrollJVCustom035ReportType.EmployeeWiseBreakDown))).Items.ToList();

        rows.Count.ShouldBe(2);
        rows.First().Values["EmpCode"].ShouldBe("JV35-BD");

        var bankRow = rows.Last();
        bankRow.Values["EmpCode"].ShouldBe(string.Empty);
        bankRow.Values["WageType"].ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_List_Unmapped_Lines_On_MissingAccounts()
    {
        var mapped = _data.CreateEmployee("JV35-MA-MAPPED");
        var unmapped = _data.CreateEmployee("JV35-MA-UNMAPPED");
        var basic = _data.CreateWageType("1000");

        _data.CreateWageAccount(mapped, basic, _data.CreateAccount("5100", "Basic"));
        _data.CreateDetail(_data.CreateRegister(mapped, JuneEnd), basic, 100);
        _data.CreateDetail(_data.CreateRegister(unmapped, JuneEnd), basic, 200);

        var result = await _appService.GetPayrollJVCustom035Report(
            Input(PayrollJVCustom035ReportType.MissingAccounts));

        result.TotalCount.ShouldBe(1);

        var row = result.Items.Single();
        row.Values["EmpCode"].ShouldBe("JV35-MA-UNMAPPED");
        row.Values["ForPeriod"].ShouldBe("2024-06-30");
        row.Amounts["Amount"].ShouldBe(200);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var basic = _data.CreateWageType("1000");

        foreach (var code in new[] { "JV35-EXP-A", "JV35-EXP-B", "JV35-EXP-C" })
        {
            var employee = _data.CreateEmployee(code);
            _data.CreateWageAccount(employee, basic, _data.CreateAccount("5100-" + code, "Basic " + code));
            _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 100);
        }

        // Page size of 1: the export must ignore it and still return every filtered row (three account
        // groupings plus the Net Salary row).
        var file = await _appService.GetPayrollJVCustom035ReportToExcel(Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom035.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(4);

        // Every header must be real text -- ABP renders an unknown localization key as "[Key]", which the
        // customer's reconciliation against the legacy file would not survive.
        var unresolved = exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ToList();
        unresolved.ShouldBeEmpty();

        exported.First().Keys.ShouldContain("Company accounts");
        exported.First().Keys.ShouldContain("Offset account type");
    }
}
