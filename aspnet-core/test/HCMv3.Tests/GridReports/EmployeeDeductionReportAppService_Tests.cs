using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Deduction grid-report port (legacy <c>uscRptEmployeeDeduction.ascx</c>,
/// <c>Table1()</c>). This report's filter panel is Month/Year/RunType/PayrollArea rather than the family's
/// usual date range, so the month-end period boundary is the equivalent of the reference report's
/// boundary-date test.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeDeductionReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeDeductionReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeDeductionReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDeductionReportAppService>();
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

    private WageType GetOrCreateWageType(string code)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().FirstOrDefault(w => w.Code == code);
            if (wageType == null)
            {
                wageType = context.Set<WageType>().Add(new WageType
                {
                    TenantId = TenantId,
                    Code = code,
                    NameEn = "WT-" + code,
                    NameAr = "WT-" + code
                }).Entity;
            }
        });

        return wageType;
    }

    /// <summary>
    /// Creates a register whose ForPeriod and InPeriod are both <paramref name="period"/> -- the only shape
    /// the legacy query accepts.
    /// </summary>
    private SalaryRegister CreateRegister(
        Employee employee,
        DateTime period,
        PayrollRunType runType = PayrollRunType.Regular)
    {
        SalaryRegister register = null;
        UsingDbContext(context =>
        {
            register = context.Set<SalaryRegister>().Add(new SalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ForPeriod = period,
                InPeriod = period,
                PayrollRunType = runType
            }).Entity;
        });

        return register;
    }

    private void CreateRegisterDetail(SalaryRegister register, WageType wageType, double amount, double number)
    {
        UsingDbContext(context =>
        {
            context.Set<SalaryRegisterDetail>().Add(new SalaryRegisterDetail
            {
                TenantId = TenantId,
                SalaryRegisterId = register.Id,
                WageTypeId = wageType.Id,
                Amount = amount,
                Number = number
            });
        });
    }

    private static GetEmployeeDeductionReportInput Input(
        int month,
        int year,
        PayrollRunType? runType = null,
        int? payrollAreaId = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeDeductionReportInput
        {
            Month = month,
            Year = year,
            PayrollRunType = runType,
            PayrollAreaId = payrollAreaId,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Sum_Each_Deduction_Wage_Type_Into_Its_Own_Column()
    {
        var employee = CreateEmployee("DED-SUM");
        var register = CreateRegister(employee, new DateTime(2024, 6, 30));

        CreateRegisterDetail(register, GetOrCreateWageType("2000"), 10, 30);
        CreateRegisterDetail(register, GetOrCreateWageType("2001"), 20, 2);
        CreateRegisterDetail(register, GetOrCreateWageType("2007"), 30, 3);
        CreateRegisterDetail(register, GetOrCreateWageType("2010"), 40, 4);

        var result = await _appService.GetEmployeeDeductionReport(Input(6, 2024));

        var row = result.Items.Single(i => i.EmployeeCode == "DED-SUM");
        row.LateEarlyDeduction.ShouldBe(10);
        row.LateEarlyMinutes.ShouldBe(30);
        row.LoanRecoveryDeduction.ShouldBe(20);
        row.LoanRecoveryDays.ShouldBe(2);
        row.UnpaidLeaveDeduction.ShouldBe(30);
        row.UnpaidLeaveDays.ShouldBe(3);
        row.LateEarlyAfterMonthlyTolerance.ShouldBe(40);
        row.LateEarlyAfterMonthlyToleranceDays.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Match_The_Period_On_The_Last_Day_Of_The_Requested_Month()
    {
        var onLastDay = CreateEmployee("DED-PER-IN");
        var onFirstDay = CreateEmployee("DED-PER-OUT");

        // Legacy turns Month/Year into the LAST day of the month and requires an exact match.
        var inRegister = CreateRegister(onLastDay, new DateTime(2024, 6, 30));
        var outRegister = CreateRegister(onFirstDay, new DateTime(2024, 6, 1));

        var wageType = GetOrCreateWageType("2000");
        CreateRegisterDetail(inRegister, wageType, 10, 1);
        CreateRegisterDetail(outRegister, wageType, 99, 9);

        var result = await _appService.GetEmployeeDeductionReport(Input(6, 2024));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("DED-PER-IN");
        codes.ShouldNotContain("DED-PER-OUT");
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Run_Type()
    {
        var regular = CreateEmployee("DED-RT-REG");
        var offCycle = CreateEmployee("DED-RT-OFF");

        var wageType = GetOrCreateWageType("2000");
        CreateRegisterDetail(CreateRegister(regular, new DateTime(2024, 7, 31)), wageType, 10, 1);
        CreateRegisterDetail(
            CreateRegister(offCycle, new DateTime(2024, 7, 31), PayrollRunType.OffCycle), wageType, 20, 2);

        var result = await _appService.GetEmployeeDeductionReport(
            Input(7, 2024, runType: PayrollRunType.OffCycle));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("DED-RT-OFF");
        codes.ShouldNotContain("DED-RT-REG");
    }

    [Fact]
    public async Task Should_Ignore_Wage_Types_Outside_The_Four_Deduction_Codes()
    {
        var employee = CreateEmployee("DED-OTHER");
        var register = CreateRegister(employee, new DateTime(2024, 8, 31));

        // 1000 is basic pay -- it must not create a row at all.
        CreateRegisterDetail(register, GetOrCreateWageType("1000"), 500, 1);

        var result = await _appService.GetEmployeeDeductionReport(Input(8, 2024));

        result.Items.Select(i => i.EmployeeCode).ShouldNotContain("DED-OTHER");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var wageType = GetOrCreateWageType("2000");

        for (var i = 1; i <= 3; i++)
        {
            var employee = CreateEmployee("DED-EXP-" + i);
            CreateRegisterDetail(CreateRegister(employee, new DateTime(2024, 9, 30)), wageType, 10 * i, i);
        }

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetEmployeeDeductionReportToExcel(Input(9, 2024, maxResultCount: 1));

        file.FileName.ShouldBe("EmployeeDeduction.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "DED-EXP-1", "DED-EXP-2", "DED-EXP-3" });
    }
}
