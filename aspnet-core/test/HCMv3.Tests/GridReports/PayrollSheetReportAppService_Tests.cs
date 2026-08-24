using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Payroll Sheet grid report (legacy <c>uscRptPayrollSheet.ascx</c>, generic branch).
///
/// This report's column set is DATA-DRIVEN -- one column per wage type actually present in the filtered
/// period -- so the tests pin both the row filtering and the derived column list, plus the rule that
/// neither the rows nor the columns may be narrowed by the current page.
///
/// With no ReportSettings row configured, <c>ShowPeriodInPayrollSheet</c> is off and the sheet covers
/// the FROM month alone, which is the path these tests exercise.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollSheetReportAppService_Tests : AppTestBase
{
    private readonly IPayrollSheetReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public PayrollSheetReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollSheetReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private WageType CreateWageType(
        string code,
        string nameEn,
        BillingType billingType = BillingType.Earning,
        int payrollOrder = 1)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = nameEn,
                NameAr = nameEn + "-Ar",
                ShortNameEn = code,
                ShortNameAr = code,
                BillingType = billingType,
                PayrollOrder = payrollOrder,
                Status = true
            }).Entity;
        });

        return wageType;
    }

    private SalaryRegister CreateRegister(
        Employee employee,
        DateTime forPeriod,
        DateTime? inPeriod = null,
        PayrollRunType runType = PayrollRunType.Regular)
    {
        SalaryRegister register = null;
        UsingDbContext(context =>
        {
            register = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ForPeriod = forPeriod,
                InPeriod = inPeriod ?? forPeriod,
                PayrollRunType = runType,
                PayrollResultType = PayrollResultType.Current,
                OffCyclePaymentDate = forPeriod
            }).Entity;
        });

        return register;
    }

    private void CreateDetail(SalaryRegister register, WageType wageType, double amount)
    {
        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = register.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                Rate = 0,
                Number = 0
            });
        });
    }

    private static GetPayrollSheetReportInput Input(
        int month,
        int year,
        string employeeCode = null,
        PayrollRunType? runType = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollSheetReportInput
        {
            Month = month,
            Year = year,
            EmployeeCode = employeeCode,
            PayrollRunType = runType,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Cover_The_From_Month_Only_When_Period_Range_Is_Disabled()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        CreateDetail(CreateRegister(CreateEmployee("PS-JUN"), new DateTime(2024, 6, 30)), wageType, 100);
        CreateDetail(CreateRegister(CreateEmployee("PS-JUL"), new DateTime(2024, 7, 31)), wageType, 100);

        var result = await _appService.GetPayrollSheetReport(Input(6, 2024));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("PS-JUN");
        codes.ShouldNotContain("PS-JUL");
    }

    [Fact]
    public async Task Should_Resolve_Month_And_Year_To_The_Month_End()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        CreateDetail(CreateRegister(CreateEmployee("PS-EOM"), new DateTime(2024, 6, 30)), wageType, 100);
        // Mid-month register: legacy matched the month END exactly, so this is not reported.
        CreateDetail(CreateRegister(CreateEmployee("PS-MID"), new DateTime(2024, 6, 15)), wageType, 100);

        var result = await _appService.GetPayrollSheetReport(Input(6, 2024));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("PS-EOM");
        codes.ShouldNotContain("PS-MID");
    }

    /// <summary>
    /// Legacy required <c>ForPeriod == InPeriod</c> (lines 2676-2677 / 2853-2854): the payroll sheet shows
    /// the IN-PERIOD run only, never a retro run paid in a later month.
    /// </summary>
    [Fact]
    public async Task Should_Exclude_Retro_Runs_Where_InPeriod_Differs_From_ForPeriod()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        CreateDetail(CreateRegister(CreateEmployee("PS-INPERIOD"), new DateTime(2024, 6, 30)), wageType, 100);
        CreateDetail(
            CreateRegister(CreateEmployee("PS-RETRO"), new DateTime(2024, 6, 30), new DateTime(2024, 7, 31)),
            wageType,
            100);

        var result = await _appService.GetPayrollSheetReport(Input(6, 2024));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("PS-INPERIOD");
        codes.ShouldNotContain("PS-RETRO");
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        foreach (var code in new[] { "PS-CODE-A", "PS-CODE-B", "PS-CODE-C" })
        {
            CreateDetail(CreateRegister(CreateEmployee(code), new DateTime(2024, 6, 30)), wageType, 100);
        }

        var result = await _appService.GetPayrollSheetReport(
            Input(6, 2024, employeeCode: "PS-CODE-A , PS-CODE-C"));

        result.Items.Select(i => i.EmployeeCode).OrderBy(c => c)
            .ShouldBe(new List<string> { "PS-CODE-A", "PS-CODE-C" });
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Run_Type_When_Supplied()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        CreateDetail(CreateRegister(CreateEmployee("PS-RT-REG"), new DateTime(2024, 6, 30)), wageType, 100);
        CreateDetail(
            CreateRegister(
                CreateEmployee("PS-RT-OFF"),
                new DateTime(2024, 6, 30),
                runType: PayrollRunType.OffCycle),
            wageType,
            100);

        var result = await _appService.GetPayrollSheetReport(
            Input(6, 2024, runType: PayrollRunType.OffCycle));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("PS-RT-OFF");
        codes.ShouldNotContain("PS-RT-REG");
    }

    /// <summary>
    /// The wage-type column list is derived from the filtered data, and only from lines that actually
    /// carry a value (legacy skipped all-zero wage types, lines 2683-2727).
    /// </summary>
    [Fact]
    public async Task Should_Derive_Wage_Type_Columns_From_The_Data_Skipping_All_Zero_Lines()
    {
        var paid = CreateWageType("1000", "PS Basic Salary", BillingType.Earning, 1);
        var zero = CreateWageType("1099", "PS Never Paid", BillingType.Earning, 2);

        var register = CreateRegister(CreateEmployee("PS-COLS"), new DateTime(2024, 6, 30));
        CreateDetail(register, paid, 500);
        CreateDetail(register, zero, 0);   // rate/number/amount all zero -> no column

        var result = await _appService.GetPayrollSheetReport(Input(6, 2024));

        result.EarningDeductionColumns.ShouldContain("PS Basic Salary");
        result.EarningDeductionColumns.ShouldNotContain("PS Never Paid");
    }

    /// <summary>
    /// The column list must come from the whole filtered set, never from the current page -- legacy built
    /// it once before the row loop, so a narrow page must not narrow the sheet.
    /// </summary>
    [Fact]
    public async Task Should_Not_Narrow_The_Column_Set_To_The_Current_Page()
    {
        var onFirstEmployee = CreateWageType("1000", "PS Wide A", BillingType.Earning, 1);
        var onSecondEmployee = CreateWageType("1001", "PS Wide B", BillingType.Earning, 2);

        CreateDetail(CreateRegister(CreateEmployee("PS-WIDE-A"), new DateTime(2024, 6, 30)), onFirstEmployee, 100);
        CreateDetail(CreateRegister(CreateEmployee("PS-WIDE-B"), new DateTime(2024, 6, 30)), onSecondEmployee, 200);

        // Page size 1 -> only the first employee's row comes back, but BOTH columns must be declared.
        var result = await _appService.GetPayrollSheetReport(Input(6, 2024, maxResultCount: 1));

        result.Items.Count.ShouldBe(1);
        result.EarningDeductionColumns.ShouldContain("PS Wide A");
        result.EarningDeductionColumns.ShouldContain("PS Wide B");
    }

    [Fact]
    public async Task Should_Number_Rows_Continuing_Across_Pages()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        foreach (var code in new[] { "PS-SN-A", "PS-SN-B", "PS-SN-C" })
        {
            CreateDetail(CreateRegister(CreateEmployee(code), new DateTime(2024, 6, 30)), wageType, 100);
        }

        var secondPage = await _appService.GetPayrollSheetReport(
            Input(6, 2024, maxResultCount: 1, skipCount: 1));

        secondPage.Items.Single().SerialNumber.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        foreach (var code in new[] { "PS-EXP-A", "PS-EXP-B", "PS-EXP-C" })
        {
            CreateDetail(CreateRegister(CreateEmployee(code), new DateTime(2024, 6, 30)), wageType, 100);
        }

        // A different month -- must not reach the export.
        CreateDetail(CreateRegister(CreateEmployee("PS-EXP-OUT"), new DateTime(2024, 7, 31)), wageType, 100);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetPayrollSheetReportToExcel(
            Input(6, 2024, employeeCode: "PS-EXP-A,PS-EXP-B,PS-EXP-C", maxResultCount: 1));

        file.FileName.ShouldBe("PayrollSheet.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c)
            .ShouldBe(new List<string> { "PS-EXP-A", "PS-EXP-B", "PS-EXP-C" });
    }

    /// <summary>
    /// Every fixed exported header must be a RESOLVED caption. A localization key with no XML entry
    /// renders as "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// (The data-driven wage-type columns are literal headers by design and are not keys.)
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Fixed_Exported_Column_Header()
    {
        CreateDetail(
            CreateRegister(CreateEmployee("PS-HDR"), new DateTime(2024, 6, 30)),
            CreateWageType("1000", "Basic Salary"),
            100);

        var file = await _appService.GetPayrollSheetReportToExcel(Input(6, 2024));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
