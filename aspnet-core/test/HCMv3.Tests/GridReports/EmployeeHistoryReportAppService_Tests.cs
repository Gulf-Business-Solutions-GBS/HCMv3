using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee History grid-report port (legacy <c>uscRptEmployeeHistory.ascx</c>,
/// <c>Table1()</c>). The distinctive semantics pinned here are the CHANGE-DATE row grain, the
/// as-of-that-date org assignment lookup, and the legacy Active-only quirk.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeHistoryReportAppService_Tests : AppTestBase
{
    private const string BasicSalaryWageTypeCode = "1000";

    private readonly IEmployeeHistoryReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeHistoryReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeHistoryReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
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
                Status = status
            }).Entity;
        });

        return employee;
    }

    private JobTitle CreateJobTitle(string name)
    {
        JobTitle jobTitle = null;
        UsingDbContext(context =>
        {
            jobTitle = context.Set<JobTitle>().Add(new JobTitle
            {
                TenantId = TenantId,
                NameEn = name,
                NameAr = name + "-Ar"
            }).Entity;
        });

        return jobTitle;
    }

    private void CreateOrgAssignment(Employee employee, DateTime start, DateTime end, JobTitle jobTitle)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                JobTitleId = jobTitle?.Id
            });
        });
    }

    /// <summary>Creates a basic pay whose detail is wage type 1000 -- the only one legacy reads.</summary>
    private void CreateBasicPay(Employee employee, DateTime start, DateTime end, double amount)
    {
        UsingDbContext(context =>
        {
            var wageType = context.Set<WageType>().FirstOrDefault(w => w.Code == BasicSalaryWageTypeCode);
            if (wageType == null)
            {
                wageType = context.Set<WageType>().Add(new WageType
                {
                    TenantId = TenantId,
                    Code = BasicSalaryWageTypeCode,
                    NameEn = "Basic Pay",
                    NameAr = "Basic Pay Ar"
                }).Entity;
                context.SaveChanges();
            }

            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;
            context.SaveChanges();

            var basicPay = context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                Amount = amount
            }).Entity;
            context.SaveChanges();

            context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
            {
                TenantId = TenantId,
                EmployeeBasicPayId = basicPay.Id,
                WageTypeId = wageType.Id,
                CurrencyId = currency.Id,
                Amount = amount
            });
        });
    }

    private static GetEmployeeHistoryReportInput Input(
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeHistoryReportInput
        {
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_One_Row_Per_Distinct_Change_Date()
    {
        var employee = CreateEmployee("HIS-DATES");
        var first = CreateJobTitle("HIS-JOB-1");
        var second = CreateJobTitle("HIS-JOB-2");

        var today = DateTime.Now.Date;

        // Two org assignment start dates + one basic-pay start date = three distinct change dates.
        CreateOrgAssignment(employee, new DateTime(2022, 1, 1), new DateTime(2022, 12, 31), first);
        CreateOrgAssignment(employee, new DateTime(2023, 1, 1), today.AddYears(2), second);
        CreateBasicPay(employee, new DateTime(2023, 6, 1), today.AddYears(2), 500);

        var result = await _appService.GetEmployeeHistoryReport(Input(employeeCode: "HIS-DATES"));

        var dates = result.Items.Select(i => i.StartDate.Date).OrderBy(d => d).ToList();
        dates.ShouldBe(new List<DateTime>
        {
            new DateTime(2022, 1, 1),
            new DateTime(2023, 1, 1),
            new DateTime(2023, 6, 1)
        });
    }

    [Fact]
    public async Task Should_Show_The_Assignment_And_Salary_In_Force_On_Each_Change_Date()
    {
        var employee = CreateEmployee("HIS-ASOF");
        var oldJob = CreateJobTitle("HIS-OLD-JOB");
        var newJob = CreateJobTitle("HIS-NEW-JOB");

        var today = DateTime.Now.Date;

        CreateOrgAssignment(employee, new DateTime(2022, 1, 1), new DateTime(2022, 12, 31), oldJob);
        CreateOrgAssignment(employee, new DateTime(2023, 1, 1), today.AddYears(2), newJob);
        CreateBasicPay(employee, new DateTime(2023, 1, 1), today.AddYears(2), 750);

        var result = await _appService.GetEmployeeHistoryReport(Input(employeeCode: "HIS-ASOF"));

        var oldRow = result.Items.Single(i => i.StartDate.Date == new DateTime(2022, 1, 1));
        oldRow.JobTitle.ShouldBe("HIS-OLD-JOB");
        // The (currently effective) basic pay only starts in 2023, so the 2022 row has no salary.
        oldRow.BasicSalary.ShouldBe(0);

        var newRow = result.Items.Single(i => i.StartDate.Date == new DateTime(2023, 1, 1));
        newRow.JobTitle.ShouldBe("HIS-NEW-JOB");
        newRow.BasicSalary.ShouldBe(750);
        newRow.EmployeeName.ShouldBe("Full HIS-ASOF");
        newRow.EmployeeNameAr.ShouldBe("عربي HIS-ASOF");
    }

    [Fact]
    public async Task Should_Reproduce_The_Legacy_Active_Only_Base_Query()
    {
        // Legacy's base query is Status == Active (line 529) and the status combo is ANDed on top, so an
        // Inactive employee can never appear -- with or without the filter. This pins that quirk.
        var inactive = CreateEmployee("HIS-INACTIVE", EmployeeStatus.Inactive);
        CreateOrgAssignment(inactive, new DateTime(2023, 1, 1), new DateTime(2030, 1, 1), null);

        var unfiltered = await _appService.GetEmployeeHistoryReport(Input(employeeCode: "HIS-INACTIVE"));
        unfiltered.Items.ShouldBeEmpty();

        var filtered = await _appService.GetEmployeeHistoryReport(
            Input(employeeCode: "HIS-INACTIVE", status: EmployeeStatus.Inactive));
        filtered.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("HIS-EXP");
        var jobTitle = CreateJobTitle("HIS-EXP-JOB");

        CreateOrgAssignment(employee, new DateTime(2021, 1, 1), new DateTime(2021, 12, 31), jobTitle);
        CreateOrgAssignment(employee, new DateTime(2022, 1, 1), new DateTime(2022, 12, 31), jobTitle);
        CreateOrgAssignment(employee, new DateTime(2023, 1, 1), new DateTime(2030, 12, 31), jobTitle);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetEmployeeHistoryReportToExcel(
            Input(employeeCode: "HIS-EXP", maxResultCount: 1));

        file.FileName.ShouldBe("EmployeeHistory.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        exported.First().Keys.ShouldContain("Start Date");
    }
}
