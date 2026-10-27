using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Salary By Nationality grid-report port (legacy <c>uscRptSalaryByNationality.ascx</c>,
/// <c>Table1()</c> lines 499-598).
///
/// This is an AGGREGATE report, so instead of the reference report's row-level assertions these tests
/// pin the grouping, the "current today" basic-pay window (the report's substitute for a date range),
/// and the deliberately odd percentage denominator. The "export ignores page size" test is kept.
/// </summary>
// ReSharper disable once InconsistentNaming
public class SalaryByNationalityReportAppService_Tests : AppTestBase
{
    private readonly ISalaryByNationalityReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public SalaryByNationalityReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ISalaryByNationalityReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateNationality(string code)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            }).Entity;
            context.SaveChanges();
            id = nationality.Id;
        });

        return id;
    }

    private Employee CreateEmployee(string code, int nationalityId, EmployeeStatus status = EmployeeStatus.Active)
    {
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
                NationalityId = nationalityId,
                Status = status
            }).Entity;
        });

        return employee;
    }

    private void CreateBasicPay(Employee employee, double amount, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                Amount = amount
            });
        });
    }

    private static DateTime Yesterday => DateTime.Now.Date.AddDays(-1);

    private static DateTime Tomorrow => DateTime.Now.Date.AddDays(1);

    private static GetSalaryByNationalityReportInput Input(
        string nationalityCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetSalaryByNationalityReportInput
        {
            NationalityCode = nationalityCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Group_Current_Basic_Pay_By_Nationality()
    {
        var nationalityId = CreateNationality("SBN-GRP");
        var first = CreateEmployee("SBN-GRP-1", nationalityId);
        var second = CreateEmployee("SBN-GRP-2", nationalityId);

        CreateBasicPay(first, 1000, Yesterday, Tomorrow);
        CreateBasicPay(second, 500, Yesterday, Tomorrow);

        var result = await _appService.GetSalaryByNationalityReport(Input(nationalityCode: "SBN-GRP"));

        var row = result.Items.Single();
        row.NationalityCode.ShouldBe("SBN-GRP");
        row.NationalityName.ShouldBe("SBN-GRP-En");
        row.Amount.ShouldBe(1500);
        row.Staff.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Ignore_Basic_Pay_Not_Current_Today()
    {
        var nationalityId = CreateNationality("SBN-WIN");
        var current = CreateEmployee("SBN-WIN-CURRENT", nationalityId);
        var expired = CreateEmployee("SBN-WIN-EXPIRED", nationalityId);
        var future = CreateEmployee("SBN-WIN-FUTURE", nationalityId);

        // Legacy pins the basic-pay window to DateTime.Now (lines 527-528) -- there is no date filter.
        CreateBasicPay(current, 1000, Yesterday, Tomorrow);
        CreateBasicPay(expired, 9999, new DateTime(2000, 1, 1), new DateTime(2000, 12, 31));
        CreateBasicPay(future, 8888, Tomorrow.AddDays(1), Tomorrow.AddYears(1));

        var result = await _appService.GetSalaryByNationalityReport(Input(nationalityCode: "SBN-WIN"));

        var row = result.Items.Single();
        row.Amount.ShouldBe(1000);
        row.Staff.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var nationalityId = CreateNationality("SBN-ST");
        var active = CreateEmployee("SBN-ST-ACTIVE", nationalityId);
        var inactive = CreateEmployee("SBN-ST-INACTIVE", nationalityId, EmployeeStatus.Inactive);

        CreateBasicPay(active, 700, Yesterday, Tomorrow);
        CreateBasicPay(inactive, 300, Yesterday, Tomorrow);

        var result = await _appService.GetSalaryByNationalityReport(
            Input(nationalityCode: "SBN-ST", status: EmployeeStatus.Inactive));

        var row = result.Items.Single();
        row.Amount.ShouldBe(300);
        row.Staff.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Compute_Percentage_Against_The_Unfiltered_Total()
    {
        var included = CreateNationality("SBN-PCT-IN");
        var other = CreateNationality("SBN-PCT-OUT");

        var first = CreateEmployee("SBN-PCT-1", included);
        var second = CreateEmployee("SBN-PCT-2", other);

        CreateBasicPay(first, 250, Yesterday, Tomorrow);
        CreateBasicPay(second, 750, Yesterday, Tomorrow);

        var result = await _appService.GetSalaryByNationalityReport(Input(nationalityCode: "SBN-PCT-IN"));

        // Legacy computes TotalAmount BEFORE the nationality filter (line 531), so filtering to one
        // nationality does NOT make its percentage 100 -- it stays a share of everybody's pay.
        var row = result.Items.Single();
        row.Amount.ShouldBe(250);
        row.Percentage.ShouldBeLessThan(100);
        row.Percentage.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var first = CreateNationality("SBN-EXP-A");
        var second = CreateNationality("SBN-EXP-B");
        var third = CreateNationality("SBN-EXP-C");

        CreateBasicPay(CreateEmployee("SBN-EXP-E1", first), 100, Yesterday, Tomorrow);
        CreateBasicPay(CreateEmployee("SBN-EXP-E2", second), 200, Yesterday, Tomorrow);
        CreateBasicPay(CreateEmployee("SBN-EXP-E3", third), 300, Yesterday, Tomorrow);

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var file = await _appService.GetSalaryByNationalityReportToExcel(Input(maxResultCount: 1));

        file.FileName.ShouldBe("SalaryByNationality.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBeGreaterThanOrEqualTo(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).ToList();
        exportedCodes.ShouldContain("SBN-EXP-A");
        exportedCodes.ShouldContain("SBN-EXP-B");
        exportedCodes.ShouldContain("SBN-EXP-C");
        exported.First().Keys.ShouldContain("Percentage");
    }
}
