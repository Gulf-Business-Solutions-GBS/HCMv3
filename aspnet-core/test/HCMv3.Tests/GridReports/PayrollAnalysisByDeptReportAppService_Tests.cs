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
/// Tests for the Payroll Analysis By Department grid report (legacy
/// <c>uscRptPayrollAnalysisByDept.ascx</c>).
///
/// A SUMMARY report: one row per (department, period), summing wage type 9000. Unlike its sibling
/// Payroll Analysis screen, the period comes from the PayrollCalendar's EndDate rather than from the
/// calendar month end, and only ForPeriod is matched (InPeriod is not consulted, so retro runs DO
/// count here).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollAnalysisByDeptReportAppService_Tests : AppTestBase
{
    private readonly IPayrollAnalysisByDeptReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public PayrollAnalysisByDeptReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPayrollAnalysisByDeptReportAppService>();
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

    /// <summary>
    /// The report resolves its period from a PayrollCalendar row, so every test needs one. A payroll
    /// area (with its currency and period lookups) has to exist for the calendar to hang off.
    /// </summary>
    private void CreateCalendar(int month, int year, DateTime endDate)
    {
        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;

            var payrollPeriod = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "P",
                NameAr = "P"
            }).Entity;

            context.SaveChanges();

            var payrollArea = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = "PA",
                CurrencyId = currency.Id,
                PayrollPeriodId = payrollPeriod.Id
            }).Entity;

            context.SaveChanges();

            context.Set<PayrollCalendar>().Add(new PayrollCalendar
            {
                TenantId = TenantId,
                PayrollAreaId = payrollArea.Id,
                PayrollMonth = month,
                PayrollYear = year,
                StartDate = new DateTime(endDate.Year, endDate.Month, 1),
                EndDate = endDate
            });
        });
    }

    private void CreatePayslip(Employee employee, DateTime forPeriod, WageType wageType, double amount)
    {
        SalaryRegister register = null;
        UsingDbContext(context =>
        {
            register = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ForPeriod = forPeriod,
                InPeriod = forPeriod,
                PayrollRunType = PayrollRunType.Regular,
                PayrollResultType = PayrollResultType.Current,
                OffCyclePaymentDate = forPeriod
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

    private static GetPayrollAnalysisByDeptReportInput Input(
        int month,
        int year,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetPayrollAnalysisByDeptReportInput
        {
            Month = month,
            Year = year,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Take_The_Period_From_The_Payroll_Calendar_End_Date()
    {
        var netPay = CreateWageType("9000");
        // A deliberately NON month-end calendar close, to prove the calendar drives the period rather
        // than the calendar month (which is what the sibling Payroll Analysis screen uses).
        CreateCalendar(6, 2024, new DateTime(2024, 6, 25));

        CreatePayslip(CreateEmployee("BD-CAL"), new DateTime(2024, 6, 25), netPay, 400);
        CreatePayslip(CreateEmployee("BD-CAL-EOM"), new DateTime(2024, 6, 30), netPay, 999);

        var result = await _appService.GetPayrollAnalysisByDeptReport(Input(6, 2024));

        result.Items.Sum(i => i.TotalAmount).ShouldBe(400);
        result.Items.ShouldAllBe(i => i.Period == new DateTime(2024, 6, 25));
    }

    [Fact]
    public async Task Should_Sum_Only_The_Net_Pay_Wage_Type()
    {
        var netPay = CreateWageType("9000");
        var basicSalary = CreateWageType("1000");
        CreateCalendar(6, 2024, new DateTime(2024, 6, 30));

        var employee = CreateEmployee("BD-WT");
        CreatePayslip(employee, new DateTime(2024, 6, 30), netPay, 300);
        CreatePayslip(employee, new DateTime(2024, 6, 30), basicSalary, 9999);

        var result = await _appService.GetPayrollAnalysisByDeptReport(Input(6, 2024));

        result.Items.Sum(i => i.TotalAmount).ShouldBe(300);
    }

    [Fact]
    public async Task Should_Roll_Several_Employees_Up_Into_One_Department_Row()
    {
        var netPay = CreateWageType("9000");
        CreateCalendar(6, 2024, new DateTime(2024, 6, 30));

        // Neither employee has a current org assignment, so both land in the "" department --
        // legacy's `g.Key.org != null ? ... : ""`.
        CreatePayslip(CreateEmployee("BD-ROLL-A"), new DateTime(2024, 6, 30), netPay, 100);
        CreatePayslip(CreateEmployee("BD-ROLL-B"), new DateTime(2024, 6, 30), netPay, 250);

        var result = await _appService.GetPayrollAnalysisByDeptReport(Input(6, 2024));

        var row = result.Items.Single(i => i.DepartmentName == string.Empty);
        row.TotalAmount.ShouldBe(350);
    }

    /// <summary>
    /// Unlike the sibling Payroll Analysis screen, this one matches ForPeriod ONLY (legacy lines
    /// 377-380) -- InPeriod is never consulted, so a retro run paid in a later month still counts
    /// towards the period it was FOR.
    /// </summary>
    [Fact]
    public async Task Should_Include_Retro_Runs_Because_Only_ForPeriod_Is_Matched()
    {
        var netPay = CreateWageType("9000");
        CreateCalendar(6, 2024, new DateTime(2024, 6, 30));

        var employee = CreateEmployee("BD-RETRO");
        SalaryRegister retro = null;
        UsingDbContext(context =>
        {
            retro = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ForPeriod = new DateTime(2024, 6, 30),
                InPeriod = new DateTime(2024, 7, 31),
                PayrollRunType = PayrollRunType.Regular,
                PayrollResultType = PayrollResultType.Current,
                OffCyclePaymentDate = new DateTime(2024, 7, 31)
            }).Entity;
        });

        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = retro.Id,
                WageTypeId = netPay.Id,
                Amount = 175,
                Rate = 0,
                Number = 0
            });
        });

        var result = await _appService.GetPayrollAnalysisByDeptReport(Input(6, 2024));

        result.Items.Sum(i => i.TotalAmount).ShouldBe(175);
    }

    [Fact]
    public async Task Should_Return_No_Rows_When_No_Calendar_Exists_For_The_Requested_Month()
    {
        var netPay = CreateWageType("9000");
        CreateCalendar(6, 2024, new DateTime(2024, 6, 30));
        CreatePayslip(CreateEmployee("BD-NOCAL"), new DateTime(2024, 6, 30), netPay, 100);

        // No calendar row exists for month 2 at all, so the report cannot resolve a period.
        var result = await _appService.GetPayrollAnalysisByDeptReport(Input(2, 2030));

        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    /// <summary>
    /// LEGACY QUIRK, DELIBERATELY REPRODUCED: when no calendar row matches BOTH month and year, the
    /// resolver falls back to the first row matching the MONTH ALONE, ignoring the year. Asking for
    /// June 2099 therefore silently reports June 2024's close.
    /// </summary>
    [Fact]
    public async Task Should_Fall_Back_To_A_Month_Only_Calendar_Match_Ignoring_The_Year()
    {
        var netPay = CreateWageType("9000");
        CreateCalendar(6, 2024, new DateTime(2024, 6, 30));
        CreatePayslip(CreateEmployee("BD-YEARFALLBACK"), new DateTime(2024, 6, 30), netPay, 220);

        var result = await _appService.GetPayrollAnalysisByDeptReport(Input(6, 2099));

        result.Items.Sum(i => i.TotalAmount).ShouldBe(220);
        result.Items.ShouldAllBe(i => i.Period == new DateTime(2024, 6, 30));
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var netPay = CreateWageType("9000");
        CreateCalendar(6, 2024, new DateTime(2024, 6, 30));

        CreatePayslip(CreateEmployee("BD-EXP-A"), new DateTime(2024, 6, 30), netPay, 100);
        CreatePayslip(CreateEmployee("BD-EXP-B"), new DateTime(2024, 6, 30), netPay, 200);
        // A different period -- must not reach the export.
        CreatePayslip(CreateEmployee("BD-EXP-OUT"), new DateTime(2024, 7, 31), netPay, 900);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetPayrollAnalysisByDeptReportToExcel(Input(6, 2024, maxResultCount: 1));

        file.FileName.ShouldBe("PayrollAnalysisByDept.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(1);
        exported.First().Keys.ShouldContain("Department");
        exported.First().Keys.ShouldContain("Total Amount");
        Convert.ToDouble(exported.First()["Total Amount"]).ShouldBe(300);

        var unresolved = exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ToList();
        unresolved.ShouldBeEmpty();
    }
}
