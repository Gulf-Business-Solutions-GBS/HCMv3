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
/// Tests for the Payroll JV (Custom 051) grid report (legacy <c>uscRptPayrollJV_Custom051.ascx</c>,
/// <c>Table1()</c> lines 508-702).
///
/// The smallest member of the JV family: one voucher line per (employee, wage type) with a non-zero
/// amount. The tests concentrate on the period boundary, the net-pay-last ordering, the export contract --
/// and on PINNING the three legacy accounting defects the port reproduces on purpose.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollJVCustom051ReportAppService_Tests : AppTestBase
{
    private readonly IPayrollJVCustom051ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly PayrollJVReportTestDataBuilder _data;

    public PayrollJVCustom051ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollJVCustom051ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
        _data = new PayrollJVReportTestDataBuilder(UsingDbContext, AbpSession.TenantId ?? 1);
    }

    private static readonly DateTime JuneEnd = new(2024, 6, 30);

    private static GetPayrollJVCustom051ReportInput Input(
        int month = 6,
        int year = 2024,
        int? payrollAreaId = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollJVCustom051ReportInput
        {
            Month = month,
            Year = year,
            PayrollAreaId = payrollAreaId,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Emit_One_Line_Per_NonZero_WageType_With_Its_Mapped_Account()
    {
        var employee = _data.CreateEmployee("JV51-A");
        var basic = _data.CreateWageType("1000");
        var account = _data.CreateAccount("5100", "Basic Salary Expense");
        _data.CreateWageAccount(employee, basic, account);

        var register = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(register, basic, 500);

        var result = await _appService.GetPayrollJVCustom051Report(Input());

        result.TotalCount.ShouldBe(1);

        var row = result.Items.Single();
        row.Values["Account"].ShouldBe("5100");
        row.Values["Account Name"].ShouldBe("Basic Salary Expense");
        row.Amounts["Amount|Debit"].ShouldBe(500);
        row.Amounts["Amount|Credit"].ShouldBe(0);
        row.Amounts["Local|Debit"].ShouldBe(500);
        row.Values["Currency"].ShouldBe("KWD");
        row.Amounts["Rate"].ShouldBe(1);
    }

    [Fact]
    public async Task Should_Drop_ZeroAmount_Lines()
    {
        var employee = _data.CreateEmployee("JV51-ZERO");
        var basic = _data.CreateWageType("1000");
        var allowance = _data.CreateWageType("1001");

        var register = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(register, basic, 0);
        _data.CreateDetail(register, allowance, 25);

        var result = await _appService.GetPayrollJVCustom051Report(Input());

        result.TotalCount.ShouldBe(1);
        result.Items.Single().Amounts["Amount|Debit"].ShouldBe(25);
    }

    [Fact]
    public async Task Should_Credit_The_Employee_Subsidiary_On_The_NetPay_Line_And_Emit_It_Last()
    {
        var employee = _data.CreateEmployee("JV51-NET");
        UsingDbContext(context =>
        {
            var tracked = context.Set<HCMv3.PersonnelAdministration.Employee>().Find(employee.Id);
            tracked.ReferenceCode = "REF-51";
        });

        var basic = _data.CreateWageType("1000");
        var netPay = _data.CreateWageType("9000", BillingType.Deduction);

        var register = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(register, basic, 400);
        _data.CreateDetail(register, netPay, 400);

        var result = await _appService.GetPayrollJVCustom051Report(Input());

        result.TotalCount.ShouldBe(2);

        // Legacy lines 612-614: net pay sorts last within the employee.
        var last = result.Items.OrderBy(r => r.SortOrder).Last();
        last.Values["Subsidiary: Entity Type"].ShouldBe("Employee");
        // Legacy line 597 uses Employee.ReferenceCode, not Employee.Code, as the subsidiary value.
        last.Values["Subsidiary: Value"].ShouldBe("REF-51");
        last.Amounts["Amount|Credit"].ShouldBe(400);
        last.Amounts["Local|Credit"].ShouldBe(400);
        last.Amounts["Amount|Debit"].ShouldBe(0);
    }

    [Fact]
    public async Task Should_Resolve_Period_To_The_Last_Day_Of_The_Month_And_Exclude_The_Neighbouring_Months()
    {
        var basic = _data.CreateWageType("1000");

        var inside = _data.CreateEmployee("JV51-IN");
        _data.CreateDetail(_data.CreateRegister(inside, JuneEnd), basic, 100);

        // 30 June is the boundary: the last day of May and the last day of July must both be excluded.
        var before = _data.CreateEmployee("JV51-BEFORE");
        _data.CreateDetail(_data.CreateRegister(before, new DateTime(2024, 5, 31)), basic, 100);

        var after = _data.CreateEmployee("JV51-AFTER");
        _data.CreateDetail(_data.CreateRegister(after, new DateTime(2024, 7, 31)), basic, 100);

        // A retro run (ForPeriod != InPeriod) is excluded even inside the window (legacy line 592).
        var retro = _data.CreateEmployee("JV51-RETRO");
        _data.CreateDetail(_data.CreateRegister(retro, JuneEnd, new DateTime(2024, 7, 31)), basic, 100);

        var result = await _appService.GetPayrollJVCustom051Report(Input());

        result.TotalCount.ShouldBe(1);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. <c>Table1()</c> narrows a local query by the selected payroll area (lines
    /// 578-584) and then renders a SECOND, UNFILTERED query (line 590), so the Payroll Area filter has no
    /// effect at all. Reproduced: passing a payroll area must not change the result.
    /// </summary>
    [Fact]
    public async Task Should_Ignore_PayrollArea_Filter_Reproducing_Legacy_Defect()
    {
        var basic = _data.CreateWageType("1000");
        var payrollArea = _data.CreatePayrollArea("PA-51", "Area 51");

        var assigned = _data.CreateEmployee("JV51-PA-IN");
        _data.CreateOrgAssignment(assigned, payrollArea, new DateTime(2020, 1, 1), new DateTime(2099, 12, 31));
        _data.CreateDetail(_data.CreateRegister(assigned, JuneEnd), basic, 100);

        var unassigned = _data.CreateEmployee("JV51-PA-OUT");
        _data.CreateDetail(_data.CreateRegister(unassigned, JuneEnd), basic, 200);

        var filtered = await _appService.GetPayrollJVCustom051Report(Input(payrollAreaId: payrollArea.Id));

        // Both employees survive -- the filter is inert, exactly as in legacy.
        filtered.TotalCount.ShouldBe(2);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. Unit / Department / Division / Project columns are declared (lines 543-550)
    /// and never assigned, so every voucher line posts with a blank cost centre even when the employee's
    /// wage account carries cost elements.
    /// </summary>
    [Fact]
    public async Task Should_Leave_CostCentre_Columns_Blank_Reproducing_Legacy_Defect()
    {
        var employee = _data.CreateEmployee("JV51-CC");
        var basic = _data.CreateWageType("1000");
        var account = _data.CreateAccount("5100", "Basic");
        _data.CreateWageAccount(
            employee,
            basic,
            account,
            costElement1: _data.CreateCostElement("D1"),
            costElement2: _data.CreateCostElement("D2"),
            costElement3: _data.CreateCostElement("D3"));

        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 100);

        var row = (await _appService.GetPayrollJVCustom051Report(Input())).Items.Single();

        row.Values["Unit"].ShouldBe(string.Empty);
        row.Values["Department"].ShouldBe(string.Empty);
        row.Values["Division"].ShouldBe(string.Empty);
        row.Values["Project: Value"].ShouldBe(string.Empty);
    }

    /// <summary>
    /// LEGACY DEFECT PINNED. A wage type with no <c>EmployeeWageAccount</c> yields an EMPTY account code
    /// rather than an error (line 635), so an unpostable voucher line is indistinguishable from a good one.
    /// </summary>
    [Fact]
    public async Task Should_Emit_A_Blank_Account_When_The_WageType_Is_Unmapped_Reproducing_Legacy_Defect()
    {
        var employee = _data.CreateEmployee("JV51-UNMAPPED");
        var basic = _data.CreateWageType("1000");
        _data.CreateDetail(_data.CreateRegister(employee, JuneEnd), basic, 100);

        var row = (await _appService.GetPayrollJVCustom051Report(Input())).Items.Single();

        row.Values["Account"].ShouldBe(string.Empty);
        row.Amounts["Amount|Debit"].ShouldBe(100);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = _data.CreateEmployee("JV51-EXP");
        var basic = _data.CreateWageType("1000");
        var allowance = _data.CreateWageType("1001");
        var deduction = _data.CreateWageType("2001", BillingType.Deduction);

        var register = _data.CreateRegister(employee, JuneEnd);
        _data.CreateDetail(register, basic, 100);
        _data.CreateDetail(register, allowance, 50);
        _data.CreateDetail(register, deduction, -20);

        // A different month -- must not reach the export.
        var other = _data.CreateEmployee("JV51-EXP-OUT");
        _data.CreateDetail(_data.CreateRegister(other, new DateTime(2024, 7, 31)), basic, 900);

        // Page size of 1: the export must ignore it and still return every filtered row.
        var file = await _appService.GetPayrollJVCustom051ReportToExcel(Input(maxResultCount: 1));

        file.FileName.ShouldBe("PayrollJVCustom051.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);

        // Every header must be real text -- ABP renders an unknown localization key as "[Key]", which in an
        // ERP import file would silently break the import.
        var unresolved = exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ToList();
        unresolved.ShouldBeEmpty();

        // The literal ERP import tokens must survive verbatim into the sheet.
        exported.First().Keys.ShouldContain("Amount|Debit");
        exported.First().Keys.ShouldContain("Subsidiary: Entity Type");
        exported.First().Keys.ShouldContain("#headerconnector");
    }
}
