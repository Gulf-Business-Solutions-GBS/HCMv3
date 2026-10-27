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
/// Tests for the Payroll Analysis grid report (legacy <c>uscRptPayrollAnalysis.ascx</c>).
///
/// A SUMMARY report: one row per (OrgLevel1, period), summing wage type 9000 (net pay). The
/// distinguishing legacy semantics pinned here are that Month/Year resolve to the LAST day of that
/// month, that BOTH ForPeriod and InPeriod must equal it (so retro runs are excluded), and that only
/// wage type 9000 contributes.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollAnalysisReportAppService_Tests : AppTestBase
{
    private readonly IPayrollAnalysisReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public PayrollAnalysisReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollAnalysisReportAppService>();
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

    private WageType CreateWageType(string code)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "WT-" + code,
                NameAr = "WT-" + code,
                ShortNameEn = code,
                ShortNameAr = code,
                Status = true
            }).Entity;
        });

        return wageType;
    }

    private void CreatePayslip(
        Employee employee,
        DateTime forPeriod,
        DateTime inPeriod,
        WageType wageType,
        double amount,
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
                InPeriod = inPeriod,
                PayrollRunType = runType,
                PayrollResultType = PayrollResultType.Current,
                OffCyclePaymentDate = inPeriod
            }).Entity;
        });

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

    private static GetPayrollAnalysisReportInput Input(
        int month,
        int year,
        PayrollRunType? runType = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollAnalysisReportInput
        {
            Month = month,
            Year = year,
            PayrollRunType = runType,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Resolve_Month_And_Year_To_The_Last_Day_Of_That_Month()
    {
        var netPay = CreateWageType("9000");
        var employee = CreateEmployee("PA-EOM");

        // June 2024 ends on the 30th -- only a register stamped with that exact date is analysed.
        CreatePayslip(employee, new DateTime(2024, 6, 30), new DateTime(2024, 6, 30), netPay, 500);

        var result = await _appService.GetPayrollAnalysisReport(Input(6, 2024));

        result.Items.Sum(i => i.TotalAmount).ShouldBe(500);
        result.Items.ShouldAllBe(i => i.Period == new DateTime(2024, 6, 30));
    }

    [Fact]
    public async Task Should_Ignore_Registers_Not_Stamped_On_The_Month_End()
    {
        var netPay = CreateWageType("9000");

        CreatePayslip(CreateEmployee("PA-EOM-OK"), new DateTime(2024, 6, 30), new DateTime(2024, 6, 30), netPay, 100);
        // Mid-month stamp -- legacy's equality predicate excludes it.
        CreatePayslip(CreateEmployee("PA-EOM-MID"), new DateTime(2024, 6, 15), new DateTime(2024, 6, 15), netPay, 999);

        var result = await _appService.GetPayrollAnalysisReport(Input(6, 2024));

        result.Items.Sum(i => i.TotalAmount).ShouldBe(100);
    }

    /// <summary>
    /// Legacy required ForPeriod AND InPeriod to both equal the month end (lines 618-632), which is what
    /// excludes a retro run -- one paid in a later month FOR an earlier period.
    /// </summary>
    [Fact]
    public async Task Should_Exclude_Retro_Runs_Where_InPeriod_Differs_From_ForPeriod()
    {
        var netPay = CreateWageType("9000");

        CreatePayslip(CreateEmployee("PA-REG"), new DateTime(2024, 6, 30), new DateTime(2024, 6, 30), netPay, 100);
        // Paid in July FOR June -- ForPeriod matches but InPeriod does not, so legacy drops it.
        CreatePayslip(CreateEmployee("PA-RETRO"), new DateTime(2024, 6, 30), new DateTime(2024, 7, 31), netPay, 777);

        var result = await _appService.GetPayrollAnalysisReport(Input(6, 2024));

        result.Items.Sum(i => i.TotalAmount).ShouldBe(100);
    }

    [Fact]
    public async Task Should_Sum_Only_The_Net_Pay_Wage_Type()
    {
        var netPay = CreateWageType("9000");
        var basicSalary = CreateWageType("1000");
        var employee = CreateEmployee("PA-WT");

        CreatePayslip(employee, new DateTime(2024, 6, 30), new DateTime(2024, 6, 30), netPay, 300);
        CreatePayslip(employee, new DateTime(2024, 6, 30), new DateTime(2024, 6, 30), basicSalary, 9999);

        var result = await _appService.GetPayrollAnalysisReport(Input(6, 2024));

        // Only wage type 9000 contributes; the basic-salary line is ignored entirely.
        result.Items.Sum(i => i.TotalAmount).ShouldBe(300);
    }

    [Fact]
    public async Task Should_Roll_Several_Employees_Up_Into_One_Org_Level_1_Row()
    {
        var netPay = CreateWageType("9000");

        // Neither employee has an org assignment, so both fall into the same "" OrgLevel1 bucket --
        // legacy's `OrgLevel1 != null ? ... : ""`.
        CreatePayslip(CreateEmployee("PA-ROLL-A"), new DateTime(2024, 6, 30), new DateTime(2024, 6, 30), netPay, 100);
        CreatePayslip(CreateEmployee("PA-ROLL-B"), new DateTime(2024, 6, 30), new DateTime(2024, 6, 30), netPay, 250);

        var result = await _appService.GetPayrollAnalysisReport(Input(6, 2024));

        var row = result.Items.Single(i => i.OrgLevel1Name == string.Empty);
        row.TotalAmount.ShouldBe(350);
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Run_Type_When_Supplied()
    {
        var netPay = CreateWageType("9000");

        CreatePayslip(CreateEmployee("PA-RT-REG"), new DateTime(2024, 6, 30), new DateTime(2024, 6, 30), netPay, 100);
        CreatePayslip(
            CreateEmployee("PA-RT-OFF"),
            new DateTime(2024, 6, 30),
            new DateTime(2024, 6, 30),
            netPay,
            60,
            PayrollRunType.OffCycle);

        var result = await _appService.GetPayrollAnalysisReport(
            Input(6, 2024, runType: PayrollRunType.OffCycle));

        result.Items.Sum(i => i.TotalAmount).ShouldBe(60);
    }

    [Fact]
    public async Task Should_Return_No_Rows_When_Nothing_Was_Paid_In_The_Period()
    {
        var netPay = CreateWageType("9000");
        CreatePayslip(CreateEmployee("PA-EMPTY"), new DateTime(2024, 6, 30), new DateTime(2024, 6, 30), netPay, 100);

        var result = await _appService.GetPayrollAnalysisReport(Input(2, 2024));

        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var netPay = CreateWageType("9000");

        CreatePayslip(CreateEmployee("PA-EXP-A"), new DateTime(2024, 6, 30), new DateTime(2024, 6, 30), netPay, 100);
        CreatePayslip(CreateEmployee("PA-EXP-B"), new DateTime(2024, 6, 30), new DateTime(2024, 6, 30), netPay, 200);
        // A different month -- must not reach the export.
        CreatePayslip(CreateEmployee("PA-EXP-OUT"), new DateTime(2024, 7, 31), new DateTime(2024, 7, 31), netPay, 900);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetPayrollAnalysisReportToExcel(Input(6, 2024, maxResultCount: 1));

        file.FileName.ShouldBe("PayrollAnalysis.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        // Both employees roll into the single "" OrgLevel1 row, whose total covers June only.
        exported.Count.ShouldBe(1);
        exported.First().Keys.ShouldContain("Amount");
        Convert.ToDouble(exported.First()["Amount"]).ShouldBe(300);

        var unresolved = exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ToList();
        unresolved.ShouldBeEmpty();
    }
}
