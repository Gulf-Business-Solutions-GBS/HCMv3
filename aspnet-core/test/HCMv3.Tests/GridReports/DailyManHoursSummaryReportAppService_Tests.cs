using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports;
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
/// Tests for the Daily Man Hours Summary grid report (legacy <c>uscRptDailyManHoursSummary.ascx</c>,
/// <c>Table1()</c> / <c>Table2()</c> / <c>Table3()</c>) -- a single-day drill-down whose two detail
/// levels have different column sets AND different grains.
/// </summary>
// ReSharper disable once InconsistentNaming
public class DailyManHoursSummaryReportAppService_Tests : AppTestBase
{
    private static readonly DateTime ReportDate = new(2024, 6, 10);

    private readonly IDailyManHoursSummaryReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly int _clockInTransTypeId;
    private readonly int _clockOutTransTypeId;

    public DailyManHoursSummaryReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IDailyManHoursSummaryReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
        _clockInTransTypeId = CreateTransType("1");
        _clockOutTransTypeId = CreateTransType("2");
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateTransType(string code)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var transType = context.Set<TransType>().Add(new TransType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = "T" + code,
                NameAr = "T" + code
            }).Entity;
            context.SaveChanges();
            id = transType.Id;
        });

        return id;
    }

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

    private void CreatePunch(Employee employee, DateTime day, int hour, int minute, bool isIn)
    {
        UsingDbContext(context =>
        {
            context.Set<TimeEvent>().Add(new TimeEvent
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                TransTypeId = isIn ? _clockInTransTypeId : _clockOutTransTypeId,
                TransDate = day,
                TransTime = day.AddHours(hour).AddMinutes(minute)
            });
        });
    }

    private void CreateOvertime(Employee employee, DateTime effectiveDate, double approvedHours)
    {
        UsingDbContext(context =>
        {
            var overtimeType = context.Set<OvertimeType>().Add(new OvertimeType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "OT",
                NameAr = "OT"
            }).Entity;
            context.SaveChanges();

            context.Set<EmployeeOvertime>().Add(new EmployeeOvertime
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                EffectiveDate = effectiveDate,
                OvertimeTypeId = overtimeType.Id,
                OvertimeHours = approvedHours,
                ActualOvertimeHours = approvedHours,
                ApprovedOvertimeHours = approvedHours
            });
        });
    }

    /// <summary>Assigns the employee to a work location for a window covering the report date.</summary>
    private string CreateWorkLocationAssignment(Employee employee, DateTime start, DateTime end)
    {
        var code = "WL-" + Guid.NewGuid().ToString("N").Substring(0, 6);
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

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                WorkLocationId = workLocation.Id
            });
        });

        return code;
    }

    private static GetDailyManHoursSummaryReportInput SummaryInput(
        DateTime date,
        string workLocationCode = null,
        int maxResultCount = 100)
    {
        return new GetDailyManHoursSummaryReportInput
        {
            FromDate = date,
            WorkLocationCode = workLocationCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    private static GetDailyManHoursSummaryDetailReportInput DetailInput(
        DateTime date,
        string workLocationCode = null,
        int maxResultCount = 100)
    {
        return new GetDailyManHoursSummaryDetailReportInput
        {
            FromDate = date,
            WorkLocationCode = workLocationCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Count_Distinct_Employees_Per_Bucket_For_The_Effective_Date()
    {
        var punched = CreateEmployee("MH-PUNCH");
        var overtime = CreateEmployee("MH-OT");

        // Two punches, ONE employee -- the count is DISTINCT employees.
        CreatePunch(punched, ReportDate, 8, 0, isIn: true);
        CreatePunch(punched, ReportDate, 17, 0, isIn: false);
        // A punch on the neighbouring day must not count.
        CreatePunch(CreateEmployee("MH-OTHERDAY"), ReportDate.AddDays(1), 8, 0, isIn: true);

        CreateOvertime(overtime, ReportDate, 2);
        CreateOvertime(overtime, ReportDate.AddDays(-1), 3);

        var summary = await _appService.GetDailyManHoursSummaryReport(SummaryInput(ReportDate));

        summary.Items.Count.ShouldBe(2);
        summary.Items.Single(i => i.Type == ManHourType.Regular).EmployeeCount.ShouldBe(1);
        summary.Items.Single(i => i.Type == ManHourType.Overtime).EmployeeCount.ShouldBe(1);
        summary.Items.Single(i => i.Type == ManHourType.Regular).TypeName.ShouldBe("Regular");
    }

    [Fact]
    public async Task Should_Filter_By_Work_Location_Using_The_Assignment_Covering_The_Effective_Date()
    {
        var matching = CreateEmployee("MH-WL-MATCH");
        var other = CreateEmployee("MH-WL-OTHER");

        CreatePunch(matching, ReportDate, 8, 0, isIn: true);
        CreatePunch(other, ReportDate, 8, 0, isIn: true);

        var workLocationCode = CreateWorkLocationAssignment(matching, ReportDate.AddDays(-10), ReportDate.AddDays(10));
        // Same shape but a DIFFERENT location, and its window ends before the report date anyway.
        CreateWorkLocationAssignment(other, ReportDate.AddDays(-10), ReportDate.AddDays(-1));

        var detail = await _appService.GetDailyManHoursRegularDetailReport(
            DetailInput(ReportDate, workLocationCode));

        var codes = detail.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("MH-WL-MATCH");
        codes.ShouldNotContain("MH-WL-OTHER");
    }

    [Fact]
    public async Task Should_Show_First_In_Last_Out_And_Their_Span_In_The_Regular_Drill_Down()
    {
        var employee = CreateEmployee("MH-SPAN");

        CreatePunch(employee, ReportDate, 8, 5, isIn: true);
        CreatePunch(employee, ReportDate, 12, 0, isIn: false);
        CreatePunch(employee, ReportDate, 13, 0, isIn: true);
        CreatePunch(employee, ReportDate, 17, 35, isIn: false);

        var detail = await _appService.GetDailyManHoursRegularDetailReport(DetailInput(ReportDate));

        var row = detail.Items.Single(i => i.EmployeeCode == "MH-SPAN");
        // Legacy formats with "hh\:mm" -- a 12-hour clock with no designator, so 17:35 reads "05:35".
        row.SignIn.ShouldBe("08:05");
        row.SignOut.ShouldBe("05:35");
        // First in to last out INCLUDING the break.
        row.TotalHours.ShouldBe("09:30:00");
    }

    [Fact]
    public async Task Should_Leave_Total_Hours_Empty_When_The_Day_Has_No_Clock_Out()
    {
        var employee = CreateEmployee("MH-NOOUT");
        CreatePunch(employee, ReportDate, 8, 0, isIn: true);

        var detail = await _appService.GetDailyManHoursRegularDetailReport(DetailInput(ReportDate));

        var row = detail.Items.Single(i => i.EmployeeCode == "MH-NOOUT");
        row.SignIn.ShouldBe("08:00");
        row.SignOut.ShouldBe(string.Empty);
        row.TotalHours.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Accumulate_Overtime_From_The_First_Of_The_Month_To_The_Effective_Date()
    {
        var employee = CreateEmployee("MH-ACC");

        CreateOvertime(employee, new DateTime(2024, 6, 1), 1);
        CreateOvertime(employee, ReportDate, 2);
        // After the effective date -- excluded. And a previous month -- excluded.
        CreateOvertime(employee, new DateTime(2024, 6, 20), 4);
        CreateOvertime(employee, new DateTime(2024, 5, 31), 8);

        var detail = await _appService.GetDailyManHoursOvertimeDetailReport(DetailInput(ReportDate));

        var row = detail.Items.Single(i => i.EmployeeCode == "MH-ACC");
        row.OvertimeHours.ShouldBe(2);
        row.AccumulatedOvertimeHours.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Emit_One_Overtime_Row_Per_Record_Reproducing_The_Legacy_Grain()
    {
        var employee = CreateEmployee("MH-TWO-OT");

        CreateOvertime(employee, ReportDate, 1);
        CreateOvertime(employee, ReportDate, 2);

        var detail = await _appService.GetDailyManHoursOvertimeDetailReport(DetailInput(ReportDate));

        var rows = detail.Items.Where(i => i.EmployeeCode == "MH-TWO-OT").ToList();
        // Legacy loops the overtime RECORDS, so the same employee appears twice, both rows carrying the
        // same (employee) Id and the same month-to-date accumulation.
        rows.Count.ShouldBe(2);
        rows.Select(r => r.OvertimeHours).OrderBy(h => h).ShouldBe(new List<double> { 1, 2 });
        rows.ShouldAllBe(r => r.AccumulatedOvertimeHours == 3);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        foreach (var code in new[] { "MH-EXP-A", "MH-EXP-B", "MH-EXP-C" })
        {
            CreatePunch(CreateEmployee(code), ReportDate, 8, 0, isIn: true);
        }

        // A different day -- must not reach the export.
        CreatePunch(CreateEmployee("MH-EXP-OUT"), ReportDate.AddDays(1), 8, 0, isIn: true);

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetDailyManHoursRegularDetailReportToExcel(
            DetailInput(ReportDate, maxResultCount: 1));

        file.FileName.ShouldBe("DailyManHoursRegularDetail.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
    }

    /// <summary>
    /// Every exported header of ALL THREE levels must be a RESOLVED caption. A localization key with no
    /// XML entry renders as "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("MH-HDR");
        CreatePunch(employee, ReportDate, 8, 0, isIn: true);
        CreateOvertime(employee, ReportDate, 1);

        var files = new[]
        {
            await _appService.GetDailyManHoursSummaryReportToExcel(SummaryInput(ReportDate)),
            await _appService.GetDailyManHoursRegularDetailReportToExcel(DetailInput(ReportDate)),
            await _appService.GetDailyManHoursOvertimeDetailReportToExcel(DetailInput(ReportDate))
        };

        foreach (var file in files)
        {
            var bytes = _tempFileCacheManager.GetFile(file.FileToken);
            using var stream = new MemoryStream(bytes);
            var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

            var unresolved = exported.First().Keys
                .Where(k => k.StartsWith("[") && k.EndsWith("]"))
                .ToList();

            unresolved.ShouldBeEmpty();
        }
    }
}
