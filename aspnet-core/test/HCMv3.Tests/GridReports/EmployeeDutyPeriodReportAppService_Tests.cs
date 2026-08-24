using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Duty Period grid report (legacy <c>uscRptEmployeeDutyPeriod.ascx</c>,
/// <c>Table1()</c>), including its overlap window, its duty-period-code filter and the
/// empty-string-not-null convention legacy used for every unresolved lookup on the row.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeDutyPeriodReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeDutyPeriodReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeDutyPeriodReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDutyPeriodReportAppService>();
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

    private DutyPeriod CreateDutyPeriod(string code)
    {
        DutyPeriod dutyPeriod = null;
        UsingDbContext(context =>
        {
            dutyPeriod = context.Set<DutyPeriod>().Add(new DutyPeriod
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31)
            }).Entity;
        });

        return dutyPeriod;
    }

    private void CreateEmployeeDutyPeriod(
        Employee employee,
        DutyPeriod dutyPeriod,
        DateTime startDate,
        DateTime endDate,
        int startPoint = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DutyPeriodId = dutyPeriod.Id,
                StartDate = startDate,
                EndDate = endDate,
                StartPoint = startPoint
            });
        });
    }

    /// <summary>
    /// The org columns come from the assignment current as of TODAY (legacy lines 570-573), not from the
    /// duty-period row's own dates, so the assignment is created around today deliberately.
    /// </summary>
    private void CreateJobTitleAssignment(Employee employee, string jobTitleName)
    {
        UsingDbContext(context =>
        {
            var jobTitle = context.Set<JobTitle>().Add(new JobTitle
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = jobTitleName,
                NameAr = jobTitleName + "-Ar"
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = Clock.Now.Date.AddYears(-1),
                EndDate = Clock.Now.Date.AddYears(1),
                JobTitleId = jobTitle.Id
            });
        });
    }

    private static GetEmployeeDutyPeriodReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        string dutyPeriodCode = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeDutyPeriodReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            DutyPeriodCode = dutyPeriodCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Rows_Overlapping_The_Requested_Window()
    {
        var inRange = CreateEmployee("EDP-IN");
        var before = CreateEmployee("EDP-BEFORE");
        var after = CreateEmployee("EDP-AFTER");
        var dutyPeriod = CreateDutyPeriod("EDP-DP-OVL");

        CreateEmployeeDutyPeriod(inRange, dutyPeriod, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateEmployeeDutyPeriod(before, dutyPeriod, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));
        CreateEmployeeDutyPeriod(after, dutyPeriod, new DateTime(2025, 6, 1), new DateTime(2025, 12, 31));

        var result = await _appService.GetEmployeeDutyPeriodReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("EDP-IN");
        codes.ShouldNotContain("EDP-BEFORE");
        codes.ShouldNotContain("EDP-AFTER");
    }

    [Fact]
    public async Task Should_Include_Rows_Touching_The_Window_Boundary()
    {
        var startsOnToDate = CreateEmployee("EDP-EDGE-START");
        var endsOnFromDate = CreateEmployee("EDP-EDGE-END");
        var dutyPeriod = CreateDutyPeriod("EDP-DP-EDGE");

        // Legacy predicate is NOT(StartDate > To || EndDate < From), so both boundary rows are included.
        CreateEmployeeDutyPeriod(startsOnToDate, dutyPeriod, new DateTime(2024, 12, 31), new DateTime(2025, 12, 31));
        CreateEmployeeDutyPeriod(endsOnFromDate, dutyPeriod, new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));

        var result = await _appService.GetEmployeeDutyPeriodReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("EDP-EDGE-START");
        codes.ShouldContain("EDP-EDGE-END");
    }

    [Fact]
    public async Task Should_Filter_By_Duty_Period_Code()
    {
        var matching = CreateEmployee("EDP-DPC-MATCH");
        var other = CreateEmployee("EDP-DPC-OTHER");
        var wanted = CreateDutyPeriod("EDP-DP-WANTED");
        var unwanted = CreateDutyPeriod("EDP-DP-UNWANTED");

        CreateEmployeeDutyPeriod(matching, wanted, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeDutyPeriod(other, unwanted, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeDutyPeriodReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), dutyPeriodCode: "EDP-DP-WANTED"));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("EDP-DPC-MATCH");
        codes.ShouldNotContain("EDP-DPC-OTHER");
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        var first = CreateEmployee("EDP-CODE-A");
        var second = CreateEmployee("EDP-CODE-B");
        var third = CreateEmployee("EDP-CODE-C");
        var dutyPeriod = CreateDutyPeriod("EDP-DP-CODE");

        foreach (var employee in new[] { first, second, third })
        {
            CreateEmployeeDutyPeriod(employee, dutyPeriod, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        }

        var result = await _appService.GetEmployeeDutyPeriodReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "EDP-CODE-A , EDP-CODE-C"));

        var codes = result.Items.Select(i => i.EmployeeCode).OrderBy(c => c).ToList();
        codes.ShouldBe(new List<string> { "EDP-CODE-A", "EDP-CODE-C" });
    }

    [Fact]
    public async Task Should_Project_The_Legacy_Columns_Including_The_Job_Title_Current_Today()
    {
        var employee = CreateEmployee("EDP-PROJ");
        var dutyPeriod = CreateDutyPeriod("EDP-DP-PROJ");

        CreateEmployeeDutyPeriod(
            employee,
            dutyPeriod,
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            startPoint: 3);

        CreateJobTitleAssignment(employee, "Analyst");

        var result = await _appService.GetEmployeeDutyPeriodReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "EDP-PROJ"));

        var row = result.Items.Single();
        row.EmployeeName.ShouldBe("Full EDP-PROJ");
        row.JobTitleName.ShouldBe("Analyst");
        row.DutyPeriodName.ShouldBe("EDP-DP-PROJ-En");
        row.StartDate.ShouldBe(new DateTime(2024, 1, 1));
        row.EndDate.ShouldBe(new DateTime(2024, 12, 31));
        row.StartPoint.ShouldBe(3);
    }

    /// <summary>
    /// Legacy lines 580/587/588 wrote an EMPTY STRING, not null, whenever the employee had no current org
    /// assignment -- so an unassigned employee still renders as blank cells, never as "null".
    /// </summary>
    [Fact]
    public async Task Should_Return_Empty_Strings_Not_Null_When_There_Is_No_Current_Org_Assignment()
    {
        var employee = CreateEmployee("EDP-NOASSIGN");
        var dutyPeriod = CreateDutyPeriod("EDP-DP-NOASSIGN");

        CreateEmployeeDutyPeriod(employee, dutyPeriod, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeDutyPeriodReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "EDP-NOASSIGN"));

        var row = result.Items.Single();
        row.JobTitleName.ShouldBe(string.Empty);
        row.CountryName.ShouldBe(string.Empty);
        row.WorkLocationName.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_Page_Results()
    {
        var first = CreateEmployee("EDP-PAGE-A");
        var second = CreateEmployee("EDP-PAGE-B");
        var dutyPeriod = CreateDutyPeriod("EDP-DP-PAGE");

        CreateEmployeeDutyPeriod(first, dutyPeriod, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeDutyPeriod(second, dutyPeriod, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var page = await _appService.GetEmployeeDutyPeriodReport(
            Input(
                new DateTime(2024, 1, 1),
                new DateTime(2024, 12, 31),
                employeeCode: "EDP-PAGE-A,EDP-PAGE-B",
                maxResultCount: 1));

        page.TotalCount.ShouldBe(2);
        page.Items.Count.ShouldBe(1);
        // Default sort is EmployeeCode ASC, StartDate ASC, EndDate ASC.
        page.Items.Single().EmployeeCode.ShouldBe("EDP-PAGE-A");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var first = CreateEmployee("EDP-EXP-A");
        var second = CreateEmployee("EDP-EXP-B");
        var third = CreateEmployee("EDP-EXP-C");
        var excluded = CreateEmployee("EDP-EXP-OUT");
        var dutyPeriod = CreateDutyPeriod("EDP-DP-EXP");

        foreach (var employee in new[] { first, second, third, excluded })
        {
            CreateEmployeeDutyPeriod(employee, dutyPeriod, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        }

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetEmployeeDutyPeriodReportToExcel(
            Input(
                new DateTime(2024, 1, 1),
                new DateTime(2024, 12, 31),
                employeeCode: "EDP-EXP-A,EDP-EXP-B,EDP-EXP-C",
                maxResultCount: 1));

        file.FileName.ShouldBe("EmployeeDutyPeriod.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("EDP-HDR");
        var dutyPeriod = CreateDutyPeriod("EDP-DP-HDR");
        CreateEmployeeDutyPeriod(employee, dutyPeriod, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var file = await _appService.GetEmployeeDutyPeriodReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "EDP-HDR"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
