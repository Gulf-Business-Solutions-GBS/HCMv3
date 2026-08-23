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
/// Tests for the Employee Info grid-report port (legacy <c>uscRptEmployeeInfo.ascx</c>, <c>Table1()</c>).
/// The distinctive semantics pinned here are the dynamic basic-pay wage-type pivot, the derived Age /
/// TotalAllowance / TotalSalary columns and the "export ignores page size" rule.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeInfoReportAppService_Tests : AppTestBase
{
    private const string BasicPayWageTypeCode = "1000";
    private const string AllowanceWageTypeCode = "1001";

    private readonly IEmployeeInfoReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeInfoReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeInfoReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(
        string code,
        EmployeeStatus status = EmployeeStatus.Active,
        DateTime? birthDate = null)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "NAT-" + code,
                NameAr = "NAT-" + code + "-Ar"
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
                BirthDate = birthDate ?? new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = status
            }).Entity;
        });

        return employee;
    }

    private WageType GetOrCreateWageType(string code, int payrollOrder)
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
                    NameAr = "WT-" + code + "-Ar",
                    PayrollOrder = payrollOrder
                }).Entity;
            }
        });

        return wageType;
    }

    /// <summary>Creates a basic pay in force TODAY -- the only shape legacy's pivot looks at.</summary>
    private void CreateCurrentBasicPay(Employee employee, params (string wageTypeCode, double amount)[] details)
    {
        var today = DateTime.Now.Date;

        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur",
                CurrencyShortFormEn = "KWD",
                CurrencyShortFormAr = "KWD-Ar"
            }).Entity;
            context.SaveChanges();

            var basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = today.AddYears(-1),
                EndDate = today.AddYears(1),
                Amount = details.Sum(d => d.amount)
            }).Entity;
            context.SaveChanges();

            foreach (var detail in details)
            {
                var wageType = context.Set<WageType>().First(w => w.Code == detail.wageTypeCode);

                context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
                {
                    TenantId = TenantId,
                    EmployeeBasicPayId = basicPay.Id,
                    WageTypeId = wageType.Id,
                    CurrencyId = currency.Id,
                    Amount = detail.amount
                });
            }
        });
    }

    private static GetEmployeeInfoReportInput Input(
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeInfoReportInput
        {
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Project_The_Core_Legacy_Columns()
    {
        var employee = CreateEmployee("INF-PROJ", birthDate: new DateTime(1990, 1, 1));

        var result = await _appService.GetEmployeeInfoReport(Input(employeeCode: "INF-PROJ"));

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe("INF-PROJ");
        // Legacy always used FullNameEn for "Employee" and had a separate FullNameAr column.
        row.EmployeeName.ShouldBe("Full INF-PROJ");
        row.EmployeeNameAr.ShouldBe("عربي INF-PROJ");
        row.NationalityName.ShouldBe("NAT-INF-PROJ");
        row.Status.ShouldBe(EmployeeStatus.Active);
        row.JoiningDate.ShouldBe(new DateTime(2020, 1, 1));
    }

    [Fact]
    public async Task Should_Compute_Age_In_Whole_Years_As_Of_Today()
    {
        var today = DateTime.Now.Date;

        // Birthday already passed this year -> exactly 30. Birthday still ahead -> 29.
        var passed = CreateEmployee("INF-AGE-PASSED", birthDate: today.AddYears(-30).AddDays(-1));
        var ahead = CreateEmployee("INF-AGE-AHEAD", birthDate: today.AddYears(-30).AddDays(1));

        var result = await _appService.GetEmployeeInfoReport(
            Input(employeeCode: "INF-AGE-PASSED,INF-AGE-AHEAD"));

        result.Items.Single(i => i.EmployeeCode == "INF-AGE-PASSED").Age.ShouldBe(30);
        result.Items.Single(i => i.EmployeeCode == "INF-AGE-AHEAD").Age.ShouldBe(29);
    }

    [Fact]
    public async Task Should_Pivot_Basic_Pay_Wage_Types_And_Total_Them()
    {
        GetOrCreateWageType(BasicPayWageTypeCode, 1);
        GetOrCreateWageType(AllowanceWageTypeCode, 2);

        var employee = CreateEmployee("INF-WAGE");
        CreateCurrentBasicPay(employee, (BasicPayWageTypeCode, 500), (AllowanceWageTypeCode, 150));

        var result = await _appService.GetEmployeeInfoReport(Input(employeeCode: "INF-WAGE"));

        result.WageTypeNames.ShouldContain("WT-" + BasicPayWageTypeCode);
        result.WageTypeNames.ShouldContain("WT-" + AllowanceWageTypeCode);

        var row = result.Items.Single();
        row.WageAmounts.Count.ShouldBe(result.WageTypeNames.Count);
        row.WageAmounts.Single(w => w.WageTypeCode == BasicPayWageTypeCode).Amount.ShouldBe(500);
        row.WageAmounts.Single(w => w.WageTypeCode == AllowanceWageTypeCode).Amount.ShouldBe(150);

        // Legacy: TotalAllowance excludes wage type 1000, TotalSalary includes everything.
        row.TotalAllowance.ShouldBe(150);
        row.TotalSalary.ShouldBe(650);
        row.Currency.ShouldBe("KWD");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status_And_Code()
    {
        CreateEmployee("INF-ST-ACTIVE");
        CreateEmployee("INF-ST-INACTIVE", EmployeeStatus.Inactive);

        var result = await _appService.GetEmployeeInfoReport(
            Input(employeeCode: "INF-ST-ACTIVE,INF-ST-INACTIVE", status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("INF-ST-INACTIVE");
        codes.ShouldNotContain("INF-ST-ACTIVE");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        CreateEmployee("INF-EXP-A");
        CreateEmployee("INF-EXP-B");
        CreateEmployee("INF-EXP-C");
        CreateEmployee("INF-EXP-OUT");

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetEmployeeInfoReportToExcel(
            Input(employeeCode: "INF-EXP-A,INF-EXP-B,INF-EXP-C", maxResultCount: 1));

        file.FileName.ShouldBe("EmployeeInfo.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "INF-EXP-A", "INF-EXP-B", "INF-EXP-C" });
    }
}
