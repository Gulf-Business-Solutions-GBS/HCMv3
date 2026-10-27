using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Daily Attendance Summary grid report (legacy
/// <c>uscRptDailyAttendanceSummary.ascx</c>, <c>Table1()</c> and <c>Table2(selectedType)</c>), a
/// single-day two-level drill-down.
///
/// The Day Off / Medical buckets are keyed on legacy's HARD-CODED AbsenceType ids 19 and 3, so these
/// tests insert absence types with exactly those ids -- see the app service remarks.
/// </summary>
// ReSharper disable once InconsistentNaming
public class DailyAttendanceSummaryReportAppService_Tests : AppTestBase
{
    private const int DayOffAbsenceTypeId = 19;
    private const int MedicalAbsenceTypeId = 3;

    private static readonly DateTime ReportDate = new(2024, 6, 10);

    private readonly IDailyAttendanceSummaryReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly int _clockInTransTypeId;

    public DailyAttendanceSummaryReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IDailyAttendanceSummaryReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
        _clockInTransTypeId = CreateTransType("1");
        CreateAbsenceTypeWithId(DayOffAbsenceTypeId, "DAYOFF");
        CreateAbsenceTypeWithId(MedicalAbsenceTypeId, "MEDICAL");
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

    /// <summary>Inserts an absence type with an EXPLICIT id -- legacy identifies these two by id.</summary>
    private void CreateAbsenceTypeWithId(int id, string code)
    {
        UsingDbContext(context =>
        {
            context.Set<AbsenceType>().Add(new AbsenceType
            {
                Id = id,
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            });
            context.SaveChanges();
        });
    }

    private int CreateAbsenceType(string code)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            }).Entity;
            context.SaveChanges();
            id = absenceType.Id;
        });

        return id;
    }

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

    private void CreatePunch(Employee employee, DateTime day)
    {
        UsingDbContext(context =>
        {
            context.Set<TimeEvent>().Add(new TimeEvent
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                TransTypeId = _clockInTransTypeId,
                TransDate = day,
                TransTime = day.AddHours(8)
            });
        });
    }

    private void CreateAbsence(Employee employee, int absenceTypeId, DateTime start, DateTime end, double absenceHours = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                AbsenceTypeId = absenceTypeId,
                StartDate = start,
                EndDate = end,
                AbsenceHours = absenceHours
            });
        });
    }

    private static GetDailyAttendanceSummaryReportInput SummaryInput(DateTime date, int maxResultCount = 100)
    {
        return new GetDailyAttendanceSummaryReportInput
        {
            FromDate = date,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    private static GetDailyAttendanceSummaryDetailReportInput DetailInput(
        DateTime date,
        DailyAttendanceType type,
        int maxResultCount = 100)
    {
        return new GetDailyAttendanceSummaryDetailReportInput
        {
            FromDate = date,
            Type = type,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    private async Task<int> GetBucketCountAsync(DateTime date, DailyAttendanceType type)
    {
        var summary = await _appService.GetDailyAttendanceSummaryReport(SummaryInput(date));
        return summary.Items.Single(i => i.Type == type).TotalEmployees;
    }

    [Fact]
    public async Task Should_Count_Each_Bucket_For_The_Effective_Date()
    {
        var onDuty = CreateEmployee("DAS-DUTY");
        var dayOff = CreateEmployee("DAS-DAYOFF");
        var medical = CreateEmployee("DAS-MED");
        var onLeave = CreateEmployee("DAS-LEAVE");

        var otherAbsenceTypeId = CreateAbsenceType("ANNUAL");

        CreatePunch(onDuty, ReportDate);
        CreateAbsence(dayOff, DayOffAbsenceTypeId, ReportDate, ReportDate);
        CreateAbsence(medical, MedicalAbsenceTypeId, ReportDate, ReportDate);
        CreateAbsence(onLeave, otherAbsenceTypeId, ReportDate, ReportDate);

        var summary = await _appService.GetDailyAttendanceSummaryReport(SummaryInput(ReportDate));

        summary.Items.Count.ShouldBe(5);
        summary.Items.Single(i => i.Type == DailyAttendanceType.OnDuty).TotalEmployees.ShouldBe(1);
        summary.Items.Single(i => i.Type == DailyAttendanceType.DayOff).TotalEmployees.ShouldBe(1);
        summary.Items.Single(i => i.Type == DailyAttendanceType.Medical).TotalEmployees.ShouldBe(1);
        summary.Items.Single(i => i.Type == DailyAttendanceType.OnLeave).TotalEmployees.ShouldBe(1);
        // None of the four is Absent: the bucket is disjoint from the other four here.
        var absentDetail = await _appService.GetDailyAttendanceSummaryDetailReport(
            DetailInput(ReportDate, DailyAttendanceType.Absent));
        absentDetail.Items.Select(i => i.EmployeeCode)
            .ShouldNotContain("DAS-DUTY");
        summary.Items.Single(i => i.Type == DailyAttendanceType.OnDuty).TypeName.ShouldBe("On Duty");
    }

    [Fact]
    public async Task Should_Count_Only_The_Absences_That_Cover_The_Effective_Date()
    {
        var covering = CreateEmployee("DAS-COVER");
        var ending = CreateEmployee("DAS-BEFORE");

        // Boundary: an absence ENDING on the effective date still covers it (StartDate <= d <= EndDate).
        CreateAbsence(covering, DayOffAbsenceTypeId, ReportDate.AddDays(-3), ReportDate);
        CreateAbsence(ending, DayOffAbsenceTypeId, ReportDate.AddDays(-3), ReportDate.AddDays(-1));

        var detail = await _appService.GetDailyAttendanceSummaryDetailReport(
            DetailInput(ReportDate, DailyAttendanceType.DayOff));

        var codes = detail.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("DAS-COVER");
        codes.ShouldNotContain("DAS-BEFORE");
    }

    /// <summary>
    /// LEGACY DEFECT PIN. <c>Table1()</c>'s Absent count requires NO absence row of any kind (lines
    /// 438-440) while <c>Table2()</c>'s Absent list only requires the employee not to be On Duty /
    /// Day Off / Medical / On Leave (line 531). An employee with a PARTIAL-day absence and no punch is
    /// therefore listed but not counted.
    /// </summary>
    [Fact]
    public async Task Should_Count_And_List_Absent_Differently_Reproducing_Legacy_Defect()
    {
        var partial = CreateEmployee("DAS-PARTIAL");
        var otherAbsenceTypeId = CreateAbsenceType("HOURLY");

        var before = await GetBucketCountAsync(ReportDate, DailyAttendanceType.Absent);

        // AbsenceHours != 0 -> not "On Leave"; but the absence row exists -> not counted as Absent.
        CreateAbsence(partial, otherAbsenceTypeId, ReportDate, ReportDate, absenceHours: 4);

        var after = await GetBucketCountAsync(ReportDate, DailyAttendanceType.Absent);

        // The employee was counted as Absent a moment ago; the partial absence REMOVES them...
        after.ShouldBe(before - 1);

        // ...yet the drill-down DOES list them.
        var detail = await _appService.GetDailyAttendanceSummaryDetailReport(
            DetailInput(ReportDate, DailyAttendanceType.Absent));
        detail.Items.Select(i => i.EmployeeCode).ShouldContain("DAS-PARTIAL");
    }

    [Fact]
    public async Task Should_Show_The_Leave_Type_Only_In_The_On_Leave_Drill_Down()
    {
        var onLeave = CreateEmployee("DAS-LT");
        var dayOff = CreateEmployee("DAS-LT-DO");
        var otherAbsenceTypeId = CreateAbsenceType("ANNUAL2");

        CreateAbsence(onLeave, otherAbsenceTypeId, ReportDate, ReportDate);
        CreateAbsence(dayOff, DayOffAbsenceTypeId, ReportDate, ReportDate);

        var leaveDetail = await _appService.GetDailyAttendanceSummaryDetailReport(
            DetailInput(ReportDate, DailyAttendanceType.OnLeave));
        leaveDetail.Items.Single(i => i.EmployeeCode == "DAS-LT").LeaveTypeName.ShouldBe("ANNUAL2-En");

        var dayOffDetail = await _appService.GetDailyAttendanceSummaryDetailReport(
            DetailInput(ReportDate, DailyAttendanceType.DayOff));
        dayOffDetail.Items.Single(i => i.EmployeeCode == "DAS-LT-DO").LeaveTypeName.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Export_All_Matching_Detail_Rows_Not_Just_The_Current_Page()
    {
        foreach (var code in new[] { "DAS-EXP-A", "DAS-EXP-B", "DAS-EXP-C" })
        {
            CreatePunch(CreateEmployee(code), ReportDate);
        }

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetDailyAttendanceSummaryDetailReportToExcel(
            DetailInput(ReportDate, DailyAttendanceType.OnDuty, maxResultCount: 1));

        file.FileName.ShouldBe("DailyAttendanceSummaryDetail.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
    }

    /// <summary>
    /// Every exported header of BOTH levels must be a RESOLVED caption. A localization key with no XML
    /// entry renders as "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler. The
    /// On Leave detail is used because it is the only variant carrying the extra Leave Type column.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("DAS-HDR");
        CreateAbsence(employee, CreateAbsenceType("ANNUAL3"), ReportDate, ReportDate);

        var summaryFile = await _appService.GetDailyAttendanceSummaryReportToExcel(SummaryInput(ReportDate));
        var detailFile = await _appService.GetDailyAttendanceSummaryDetailReportToExcel(
            DetailInput(ReportDate, DailyAttendanceType.OnLeave));

        foreach (var file in new[] { summaryFile, detailFile })
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
