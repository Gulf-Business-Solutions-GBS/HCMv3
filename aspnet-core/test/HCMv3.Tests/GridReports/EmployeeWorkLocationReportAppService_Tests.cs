using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Work Location grid-report port (legacy <c>uscRptEmployeeWorkLocation.ascx</c>,
/// <c>Table1()</c> lines 513-683). Pins the date-window boundary, the report-specific work-location-code
/// filter, the "Remarks = last day in service" column and that the export ignores the page size.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeWorkLocationReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeWorkLocationReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeWorkLocationReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeWorkLocationReportAppService>();
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
                NameEn = "Nationality " + code,
                NameAr = "Nationality " + code
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
                CivilID = 123456789,
                Status = status
            }).Entity;
        });

        return employee;
    }

    private int CreateWorkLocation(string code)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var workLocation = context.Set<WorkLocation>().Add(new WorkLocation
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            }).Entity;
            context.SaveChanges();
            id = workLocation.Id;
        });

        return id;
    }

    private void CreateAssignment(Employee employee, DateTime start, DateTime end, int? workLocationId = null)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                WorkLocationId = workLocationId
            });
        });
    }

    private void CreateSeparation(Employee employee, DateTime submissionDate, DateTime lastDayInService)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeSeparation>().Add(new EmployeeSeparation
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                SubmissionDate = submissionDate,
                LastDayInService = lastDayInService,
                PlannedLastDayInService = lastDayInService,
                SeparationType = SeparationType.Resignation,
                SeparationReasonId = CreateSeparationReason()
            });
        });
    }

    private int CreateSeparationReason()
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var reason = context.Set<SeparationReason>().Add(new SeparationReason
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Reason",
                NameAr = "Reason"
            }).Entity;
            context.SaveChanges();
            id = reason.Id;
        });

        return id;
    }

    private static GetEmployeeWorkLocationReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        string workLocationCode = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeWorkLocationReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            WorkLocationCode = workLocationCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Assignments_Overlapping_The_Requested_Window()
    {
        var inRange = CreateEmployee("WL-IN");
        var before = CreateEmployee("WL-BEFORE");
        var after = CreateEmployee("WL-AFTER");

        CreateAssignment(inRange, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateAssignment(before, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));
        CreateAssignment(after, new DateTime(2025, 6, 1), new DateTime(2025, 12, 31));

        var result = await _appService.GetEmployeeWorkLocationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("WL-IN");
        codes.ShouldNotContain("WL-BEFORE");
        codes.ShouldNotContain("WL-AFTER");
    }

    [Fact]
    public async Task Should_Include_Assignments_Touching_The_Window_Boundary()
    {
        var startsOnToDate = CreateEmployee("WL-EDGE-START");
        var endsOnFromDate = CreateEmployee("WL-EDGE-END");

        // Legacy predicate is NOT(StartDate > To || EndDate < From), so both boundary rows are included.
        CreateAssignment(startsOnToDate, new DateTime(2024, 12, 31), new DateTime(2025, 12, 31));
        CreateAssignment(endsOnFromDate, new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));

        var result = await _appService.GetEmployeeWorkLocationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("WL-EDGE-START");
        codes.ShouldContain("WL-EDGE-END");
    }

    [Fact]
    public async Task Should_Filter_By_Work_Location_Code()
    {
        var head = CreateWorkLocation("WLC-HEAD");
        var branch = CreateWorkLocation("WLC-BRANCH");

        var atHead = CreateEmployee("WL-CODE-HEAD");
        var atBranch = CreateEmployee("WL-CODE-BRANCH");

        CreateAssignment(atHead, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), head);
        CreateAssignment(atBranch, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), branch);

        var result = await _appService.GetEmployeeWorkLocationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), workLocationCode: "WLC-HEAD"));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("WL-CODE-HEAD");
        codes.ShouldNotContain("WL-CODE-BRANCH");
    }

    [Fact]
    public async Task Should_Project_Legacy_Columns_Including_Work_Location_From_The_Row_Itself()
    {
        var workLocationId = CreateWorkLocation("WLC-PROJ");
        var employee = CreateEmployee("WL-PROJ");

        CreateAssignment(employee, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), workLocationId);

        var result = await _appService.GetEmployeeWorkLocationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "WL-PROJ"));

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe("WL-PROJ");
        row.EmployeeName.ShouldBe("Full WL-PROJ");
        row.NationalityName.ShouldBe("Nationality WL-PROJ");
        row.CivilId.ShouldBe(123456789);
        row.WorkLocationName.ShouldBe("WLC-PROJ-En");
    }

    [Fact]
    public async Task Should_Report_Last_Day_In_Service_Only_For_Separations_Submitted_By_The_From_Date()
    {
        var early = CreateEmployee("WL-SEP-EARLY");
        var late = CreateEmployee("WL-SEP-LATE");

        CreateAssignment(early, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateAssignment(late, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        // Submitted before the FROM date -> legacy shows it.
        CreateSeparation(early, new DateTime(2023, 12, 1), new DateTime(2024, 3, 31));
        // Submitted after the FROM date -> legacy ignores it.
        CreateSeparation(late, new DateTime(2024, 7, 1), new DateTime(2024, 9, 30));

        var result = await _appService.GetEmployeeWorkLocationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
                employeeCode: "WL-SEP-EARLY,WL-SEP-LATE"));

        var rows = result.Items.ToDictionary(r => r.EmployeeCode, r => r.LastDayInService);
        rows["WL-SEP-EARLY"].ShouldBe(new DateTime(2024, 3, 31));
        rows["WL-SEP-LATE"].ShouldBeNull();
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var first = CreateEmployee("WL-EXP-A");
        var second = CreateEmployee("WL-EXP-B");
        var third = CreateEmployee("WL-EXP-C");
        var excluded = CreateEmployee("WL-EXP-OUT");

        CreateAssignment(first, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateAssignment(second, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateAssignment(third, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateAssignment(excluded, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            employeeCode: "WL-EXP-A,WL-EXP-B,WL-EXP-C",
            maxResultCount: 1);

        var file = await _appService.GetEmployeeWorkLocationReportToExcel(input);

        file.FileName.ShouldBe("EmployeeWorkLocation.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "WL-EXP-A", "WL-EXP-B", "WL-EXP-C" });
        // The exporter writes LOCALIZED header cells, not the raw localization key.
        exported.First().Keys.ShouldContain("Last Day In Service");
    }
}
