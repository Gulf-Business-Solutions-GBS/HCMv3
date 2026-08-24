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
/// Tests for the Payroll Sheet (Custom 023) grid report (legacy
/// <c>uscRptPayrollSheet_Custom023.ascx</c>).
///
/// Unlike the generic Payroll Sheet, this customer variant has a FIXED column set hard-coded in the
/// legacy control (seven "Act ..." basic-pay columns plus 56 earning/deduction/net columns), and it
/// matches wage types by NAME rather than by code. The tests pin that the column set is constant
/// regardless of the data, alongside the usual filtering and export rules.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollSheetCustom023ReportAppService_Tests : AppTestBase
{
    private readonly IPayrollSheetCustom023ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public PayrollSheetCustom023ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollSheetCustom023ReportAppService>();
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

    private WageType CreateWageType(string code, string nameEn, BillingType billingType = BillingType.Earning)
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
                PayrollOrder = 1,
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

    private static GetPayrollSheetCustom023ReportInput Input(
        int month,
        int year,
        string employeeCode = null,
        PayrollRunType? runType = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollSheetCustom023ReportInput
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
    public async Task Should_Resolve_Month_And_Year_To_The_Month_End()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        CreateDetail(CreateRegister(CreateEmployee("C23-EOM"), new DateTime(2024, 6, 30)), wageType, 100);
        CreateDetail(CreateRegister(CreateEmployee("C23-MID"), new DateTime(2024, 6, 15)), wageType, 100);
        CreateDetail(CreateRegister(CreateEmployee("C23-JUL"), new DateTime(2024, 7, 31)), wageType, 100);

        var result = await _appService.GetPayrollSheetCustom023Report(Input(6, 2024));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("C23-EOM");
        codes.ShouldNotContain("C23-MID");
        codes.ShouldNotContain("C23-JUL");
    }

    /// <summary>
    /// Legacy required <c>ForPeriod == InPeriod</c>: the payroll sheet shows the IN-PERIOD run only,
    /// never a retro run paid in a later month.
    /// </summary>
    [Fact]
    public async Task Should_Exclude_Retro_Runs_Where_InPeriod_Differs_From_ForPeriod()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        CreateDetail(CreateRegister(CreateEmployee("C23-INPERIOD"), new DateTime(2024, 6, 30)), wageType, 100);
        CreateDetail(
            CreateRegister(CreateEmployee("C23-RETRO"), new DateTime(2024, 6, 30), new DateTime(2024, 7, 31)),
            wageType,
            100);

        var result = await _appService.GetPayrollSheetCustom023Report(Input(6, 2024));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("C23-INPERIOD");
        codes.ShouldNotContain("C23-RETRO");
    }

    /// <summary>
    /// The Custom 023 filter panel (uscRptPayrollSheet_Custom023.ascx lines 24-48) is Month/Year,
    /// To-Month/To-Year, Run Type, Payroll Area and Reason Type -- there is NO employee-code text box,
    /// unlike the generic Payroll Sheet, which has one at line 68. The EmployeeCode field inherited from
    /// GridReportInputBase is therefore ignored on purpose; the Angular screen hides it. This test pins
    /// that so nobody "fixes" it into a filter the legacy screen never had.
    /// </summary>
    [Fact]
    public async Task Should_Ignore_The_Inherited_EmployeeCode_Filter_Because_Legacy_Has_No_Such_Box()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        foreach (var code in new[] { "C23-CODE-A", "C23-CODE-B", "C23-CODE-C" })
        {
            CreateDetail(CreateRegister(CreateEmployee(code), new DateTime(2024, 6, 30)), wageType, 100);
        }

        var result = await _appService.GetPayrollSheetCustom023Report(
            Input(6, 2024, employeeCode: "C23-CODE-A , C23-CODE-C"));

        // All three come back: the code filter is not applied.
        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("C23-CODE-A");
        codes.ShouldContain("C23-CODE-B");
        codes.ShouldContain("C23-CODE-C");
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Run_Type_When_Supplied()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        CreateDetail(CreateRegister(CreateEmployee("C23-RT-REG"), new DateTime(2024, 6, 30)), wageType, 100);
        CreateDetail(
            CreateRegister(
                CreateEmployee("C23-RT-OFF"),
                new DateTime(2024, 6, 30),
                runType: PayrollRunType.OffCycle),
            wageType,
            100);

        var result = await _appService.GetPayrollSheetCustom023Report(
            Input(6, 2024, runType: PayrollRunType.OffCycle));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("C23-RT-OFF");
        codes.ShouldNotContain("C23-RT-REG");
    }

    /// <summary>
    /// The Custom 023 column set is HARD-CODED in the legacy control, not derived from the data: the
    /// seven "Act ..." basic-pay columns and the 56 earning/deduction/net columns are always emitted,
    /// even for a period in which none of those wage types were paid.
    /// </summary>
    [Fact]
    public async Task Should_Always_Declare_The_Fixed_Legacy_Column_Set()
    {
        // One wage type paid, none of which is in the fixed list beyond Basic Salary.
        CreateDetail(
            CreateRegister(CreateEmployee("C23-COLS"), new DateTime(2024, 6, 30)),
            CreateWageType("1000", "Basic Salary"),
            100);

        var result = await _appService.GetPayrollSheetCustom023Report(Input(6, 2024));

        result.ActualBasicPayColumns.Count.ShouldBe(7);
        result.ActualBasicPayColumns.ShouldContain("Basic Salary");
        result.ActualBasicPayColumns.ShouldContain("Parking Allowance");

        result.EarningDeductionColumns.Count.ShouldBe(56);
        result.EarningDeductionColumns.ShouldContain("Basic Salary");
        // Never paid in this data set, but still declared -- the list is static.
        result.EarningDeductionColumns.ShouldContain("Mandub Commissions");
        result.EarningDeductionColumns.ShouldContain("Cash Loan Recovery Ded Arrear");
    }

    [Fact]
    public async Task Should_Match_Wage_Types_By_Name_Into_Their_Fixed_Column()
    {
        var employee = CreateEmployee("C23-MATCH");
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));

        CreateDetail(register, CreateWageType("1000", "Basic Salary"), 750);
        CreateDetail(register, CreateWageType("1070", "Parking Allowance"), 25);
        // Not one of the 56 declared names -- legacy dropped it silently.
        CreateDetail(register, CreateWageType("1999", "Not A Declared Column"), 999);

        var result = await _appService.GetPayrollSheetCustom023Report(Input(6, 2024));

        var row = result.Items.Single(i => i.EmployeeCode == "C23-MATCH");
        row.WageAmounts["Basic Salary"].ShouldBe(750);
        row.WageAmounts["Parking Allowance"].ShouldBe(25);
        row.WageAmounts.ContainsKey("Not A Declared Column").ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Number_Rows_Continuing_Across_Pages()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        foreach (var code in new[] { "C23-SN-A", "C23-SN-B", "C23-SN-C" })
        {
            CreateDetail(CreateRegister(CreateEmployee(code), new DateTime(2024, 6, 30)), wageType, 100);
        }

        var secondPage = await _appService.GetPayrollSheetCustom023Report(
            Input(6, 2024, maxResultCount: 1, skipCount: 1));

        secondPage.Items.Single().SerialNumber.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var wageType = CreateWageType("1000", "Basic Salary");

        foreach (var code in new[] { "C23-EXP-A", "C23-EXP-B", "C23-EXP-C" })
        {
            CreateDetail(CreateRegister(CreateEmployee(code), new DateTime(2024, 6, 30)), wageType, 100);
        }

        // A different month -- must not reach the export.
        CreateDetail(CreateRegister(CreateEmployee("C23-EXP-OUT"), new DateTime(2024, 7, 31)), wageType, 100);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetPayrollSheetCustom023ReportToExcel(
            Input(6, 2024, maxResultCount: 1));

        file.FileName.ShouldBe("PayrollSheetCustom023.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c)
            .ShouldBe(new List<string> { "C23-EXP-A", "C23-EXP-B", "C23-EXP-C" });
    }

    /// <summary>
    /// Every fixed exported header must be a RESOLVED caption. A localization key with no XML entry
    /// renders as "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Fixed_Exported_Column_Header()
    {
        CreateDetail(
            CreateRegister(CreateEmployee("C23-HDR"), new DateTime(2024, 6, 30)),
            CreateWageType("1000", "Basic Salary"),
            100);

        var file = await _appService.GetPayrollSheetCustom023ReportToExcel(Input(6, 2024));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
