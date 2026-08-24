using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Custom053 Late/Early Log grid report (legacy
/// <c>uscRptLateEarlyLog_Custom053.ascx</c>, <c>Table1()</c>).
///
/// The most divergent member of the family. It changes the question the report answers -- from "did the
/// employee meet THEIR OWN roster?" to "did they meet the roster of the SITE THEY LEFT FROM?" -- so the
/// tests concentrate on the work-location duty, the same-day-only punch window, the week-off compensate
/// overtime rule and the decimal Overtime Hours column.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LateEarlyLogCustom053ReportAppService_Tests : LateEarlyLogCustomReportTestBase
{
    /// <summary>
    /// Legacy line 891: the "WeekOff Compensate" absence type is identified by this HARD-CODED
    /// <c>AbsenceType.Code</c> literal, not by a setting or a flag.
    /// </summary>
    private const string WeekOffCompensateCode = "18";

    private readonly ILateEarlyLogCustom053ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public LateEarlyLogCustom053ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILateEarlyLogCustom053ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private static GetLateEarlyLogCustom053ReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        string payrollAreaCode = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetLateEarlyLogCustom053ReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            PayrollAreaCode = payrollAreaCode,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    private int CreateWorkLocation(string nameEn)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var workLocation = context.Set<WorkLocation>().Add(new WorkLocation
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = "عربي " + nameEn
            }).Entity;
            context.SaveChanges();
            id = workLocation.Id;
        });

        return id;
    }

    /// <summary>A punch carrying a work location -- what selects the site's roster on this variant.</summary>
    private void CreatePunchAt(
        Employee employee,
        DateTime day,
        TimeSpan timeOfDay,
        string transTypeCode,
        int? workLocationId = null)
    {
        var transTypeId = transTypeCode == PunchInCode ? PunchInTransTypeId : PunchOutTransTypeId;

        UsingDbContext(context =>
        {
            context.Set<TimeEvent>().Add(new TimeEvent
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                TransTypeId = transTypeId,
                TransDate = day.Date,
                TransTime = day.Date.Add(timeOfDay),
                WorkLocationId = workLocationId
            });
        });
    }

    private void AssignWorkLocationDutyPeriod(int workLocationId, int dutyPeriodId, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<WorkLocationDutyPeriod>().Add(new WorkLocationDutyPeriod
            {
                TenantId = TenantId,
                WorkLocationId = workLocationId,
                DutyPeriodId = dutyPeriodId,
                StartDate = start,
                EndDate = end
            });
        });
    }

    private int CreateWeekOffCompensateAbsenceType()
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = WeekOffCompensateCode,
                NameEn = "WeekOff Compensate",
                NameAr = "تعويض",
                IsPaid = true
            }).Entity;
            context.SaveChanges();
            id = absenceType.Id;
        });

        return id;
    }

    private void CreateCompensateLeave(Employee employee, int absenceTypeId, DateTime start, DateTime end, double days)
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
                // NON-ZERO absence hours on purpose: a zero would make this a FULL-DAY LEAVE and the day
                // would take the leave branch instead of the attendance branch.
                AbsenceHours = 1,
                TotalLeaveDays = days
            });
        });
    }

    [Fact]
    public async Task Should_Include_Both_Boundary_Days_Of_The_Window()
    {
        var employee = CreateEmployee("C053-BOUND");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 12);

        GiveStandardDayShift(employee, from.AddDays(-30), to.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom053Report(Input(from, to, employee.Code));

        result.Items.Select(r => r.Date).OrderBy(d => d)
            .ShouldBe(new[] { from, from.AddDays(1), to });
    }

    [Fact]
    public async Task Should_Return_Nothing_When_FromDate_Is_After_ToDate()
    {
        var employee = CreateEmployee("C053-INVERTED");
        GiveStandardDayShift(employee, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetLateEarlyLogCustom053Report(
            Input(new DateTime(2024, 6, 12), new DateTime(2024, 6, 10), employee.Code));

        result.TotalCount.ShouldBe(0);
    }

    /// <summary>
    /// THE DEFINING CUSTOM053 CHANGE (legacy lines 949-1044). The day's LAST clock-out carrying a work
    /// location selects that site's duty period, and the site's shift -- not the employee's -- is what the
    /// early/late arithmetic is measured against.
    /// </summary>
    [Fact]
    public async Task Should_Measure_The_Day_Against_The_Work_Location_Shift_Not_The_Employee_Shift()
    {
        var employee = CreateEmployee("C053-WLDUTY");
        var day = new DateTime(2024, 6, 10);

        // The EMPLOYEE's own roster is 08:00-16:00 ...
        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        // ... but the SITE they clocked out from runs 09:00-17:00.
        var workLocationId = CreateWorkLocation("Site B");
        var siteDutyId = CreateDuty(
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17),
            flexiStartTime: TimeSpan.FromHours(6),
            flexiEndTime: TimeSpan.FromHours(20));
        AssignWorkLocationDutyPeriod(
            workLocationId, CreateDutyPeriod(siteDutyId), day.AddDays(-30), day.AddDays(30));

        CreatePunchAt(employee, day, TimeSpan.FromHours(9), PunchInCode, workLocationId);
        CreatePunchAt(employee, day, TimeSpan.FromHours(17), PunchOutCode, workLocationId);

        var result = await _appService.GetLateEarlyLogCustom053Report(Input(day, day, employee.Code));

        var row = result.Items.Single();
        // The site's bounds, not the employee's 08:00-16:00.
        row.ShiftStart.ShouldBe(TimeSpan.FromHours(9));
        row.ShiftEnd.ShouldBe(TimeSpan.FromHours(17));
        // And therefore neither late nor early, where the employee's own roster would have said "late in".
        row.LateIn.ShouldBeNull();
        row.EarlyOut.ShouldBeNull();
    }

    /// <summary>
    /// Legacy line 1035: with no work-location duty period the employee's own duty is the FALLBACK, so the
    /// report degrades to the familiar behaviour rather than to a blank row.
    /// </summary>
    [Fact]
    public async Task Should_Fall_Back_To_The_Employee_Duty_When_The_Work_Location_Has_No_Duty_Period()
    {
        var employee = CreateEmployee("C053-FALLBACK");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        var workLocationId = CreateWorkLocation("Site With No Roster");
        CreatePunchAt(employee, day, TimeSpan.FromHours(8), PunchInCode, workLocationId);
        CreatePunchAt(employee, day, TimeSpan.FromHours(16), PunchOutCode, workLocationId);

        var result = await _appService.GetLateEarlyLogCustom053Report(Input(day, day, employee.Code));

        var row = result.Items.Single();
        row.ShiftStart.ShouldBe(TimeSpan.FromHours(8));
        row.ShiftEnd.ShouldBe(TimeSpan.FromHours(16));
    }

    /// <summary>
    /// LEGACY CHANGE PINNED (legacy lines 945-946). Custom053 takes ONLY same-day punches, where the base
    /// takes the day before, the day and the day after -- so a night shift's next-morning clock-out is
    /// invisible to this report.
    /// </summary>
    [Fact]
    public async Task Should_Ignore_A_Punch_From_The_Neighbouring_Day()
    {
        var employee = CreateEmployee("C053-SAMEDAY");
        var day = new DateTime(2024, 6, 10);

        var dutyId = CreateDuty(
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(16),
            flexiStartTime: TimeSpan.Zero,
            flexiEndTime: new TimeSpan(23, 59, 59));
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), day.AddDays(-30), day.AddDays(30));
        AssignTimePolicy(employee, CreateTimePolicy(), day.AddDays(-30), day.AddDays(30));

        CreatePunchAt(employee, day, TimeSpan.FromHours(8), PunchInCode);
        // The clock-out lands on the FOLLOWING calendar day.
        CreatePunchAt(employee, day.AddDays(1), TimeSpan.FromHours(2), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom053Report(Input(day, day, employee.Code));

        var row = result.Items.Single();
        row.SignIn.ShouldBe(TimeSpan.FromHours(8));
        row.SignOut.ShouldBeNull();
        row.Status.ShouldBe("Missed Out");
    }

    /// <summary>
    /// Legacy line 1381: Overtime Hours is <c>typeof(double)</c> on this variant and written as
    /// <c>Math.Round(TotalHours, 3)</c> -- DECIMAL HOURS, not a duration like every other time column.
    /// </summary>
    [Fact]
    public async Task Should_Report_Overtime_Hours_As_Rounded_Decimal_Hours()
    {
        var employee = CreateEmployee("C053-OT");
        var day = new DateTime(2024, 6, 10);

        var dutyId = CreateDuty(
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(16),
            flexiStartTime: TimeSpan.Zero,
            flexiEndTime: new TimeSpan(23, 59, 59));
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), day.AddDays(-30), day.AddDays(30));
        AssignTimePolicy(employee, CreateTimePolicy(), day.AddDays(-30), day.AddDays(30));

        // Nine and a half hours worked against an eight-hour planned shift.
        CreatePunchAt(employee, day, TimeSpan.FromHours(8), PunchInCode);
        CreatePunchAt(employee, day, new TimeSpan(17, 30, 0), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom053Report(Input(day, day, employee.Code));

        result.Items.Single().OvertimeHours.ShouldBe(1.5);
    }

    /// <summary>
    /// THE WEEK-OFF COMPENSATE RULE (legacy lines 1339-1355). On a week-off day the employee's whole shift
    /// counts as overtime UNLESS they hold a "WeekOff Compensate" leave (AbsenceType.Code "18"), in which
    /// case only the excess over the site's planned shift does -- and one day of the stock is consumed.
    /// </summary>
    [Fact]
    public async Task Should_Count_A_Whole_Week_Off_Day_As_Overtime_When_No_Compensate_Leave_Is_Held()
    {
        var employee = CreateEmployee("C053-WEEKOFF-NOCOMP");
        var day = new DateTime(2024, 6, 10);

        var weekOffDutyId = CreateDuty(
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(16),
            isWeeklyOff: true,
            flexiStartTime: TimeSpan.Zero,
            flexiEndTime: new TimeSpan(23, 59, 59));
        AssignDutyPeriod(employee, CreateDutyPeriod(weekOffDutyId), day.AddDays(-30), day.AddDays(30));
        AssignTimePolicy(employee, CreateTimePolicy(), day.AddDays(-30), day.AddDays(30));

        CreatePunchAt(employee, day, TimeSpan.FromHours(8), PunchInCode);
        CreatePunchAt(employee, day, TimeSpan.FromHours(13), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom053Report(Input(day, day, employee.Code));

        var row = result.Items.Single();
        row.Status.ShouldBe("Week Off");
        // The ENTIRE five hours, not the excess over anything.
        row.OvertimeHours.ShouldBe(5);
    }

    /// <summary>
    /// The counterpart of the test above, and the ONLY arrangement in which the compensate branch is
    /// observable: the EMPLOYEE's duty is a week off (so the compensate branch is entered) while the WORK
    /// LOCATION's is a normal four-hour shift (so <c>plannedHours_WL</c> is non-zero and there is an
    /// "excess" to subtract). When both are week offs, <c>plannedHours_WL</c> is blanked by its own
    /// week-off guard and the two branches collapse to the same answer.
    /// </summary>
    [Fact]
    public async Task Should_Only_Count_The_Excess_On_A_Week_Off_Day_When_A_Compensate_Leave_Is_Held()
    {
        var withLeave = CreateEmployee("C053-WEEKOFF-COMP");
        var withoutLeave = CreateEmployee("C053-WEEKOFF-NOCOMP2");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 10);

        // Both employees are rostered OFF ...
        var weekOffDutyId = CreateDuty(
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(16),
            isWeeklyOff: true,
            flexiStartTime: TimeSpan.Zero,
            flexiEndTime: new TimeSpan(23, 59, 59));
        var weekOffDutyPeriodId = CreateDutyPeriod(weekOffDutyId);

        // ... and both clocked out from a site whose own roster is a normal four-hour shift.
        var workLocationId = CreateWorkLocation("Site Open On Weekends");
        var siteDutyId = CreateDuty(
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(12),
            flexiStartTime: TimeSpan.Zero,
            flexiEndTime: new TimeSpan(23, 59, 59));
        AssignWorkLocationDutyPeriod(
            workLocationId, CreateDutyPeriod(siteDutyId), from.AddDays(-30), to.AddDays(30));

        foreach (var employee in new[] { withLeave, withoutLeave })
        {
            AssignDutyPeriod(employee, weekOffDutyPeriodId, from.AddDays(-30), to.AddDays(30));
            AssignTimePolicy(employee, CreateTimePolicy(), from.AddDays(-30), to.AddDays(30));

            CreatePunchAt(employee, from, TimeSpan.FromHours(8), PunchInCode, workLocationId);
            CreatePunchAt(employee, from, TimeSpan.FromHours(13), PunchOutCode, workLocationId);
        }

        // Only ONE of them holds a compensate day, fully inside the window (legacy's containment is strict).
        CreateCompensateLeave(withLeave, CreateWeekOffCompensateAbsenceType(), from, to, days: 1);

        var result = await _appService.GetLateEarlyLogCustom053Report(Input(from, to));

        // Five hours worked against the SITE's four-hour shift -> one hour of overtime.
        result.Items.Single(r => r.EmployeeCode == withLeave.Code).OvertimeHours.ShouldBe(1);

        // No compensate day left -> the whole five hours are overtime.
        result.Items.Single(r => r.EmployeeCode == withoutLeave.Code).OvertimeHours.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Not_Report_A_Country_Column_The_Base_Report_Has()
    {
        var employee = CreateEmployee("C053-NOCOUNTRY");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        var file = await _appService.GetLateEarlyLogCustom053ReportToExcel(Input(day, day, employee.Code));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotContain("Country");
        headers.ShouldNotContain("Break In");
        headers.ShouldNotContain("Break Out");
        headers.ShouldNotContain("Break Hours");
        headers.ShouldContain("Work Location");
    }

    [Fact]
    public async Task Should_Exclude_Inactive_Employees()
    {
        var day = new DateTime(2024, 6, 10);
        var inactive = CreateEmployee("C053-INACTIVE", EmployeeStatus.Inactive);
        GiveStandardDayShift(inactive, day.AddDays(-30), day.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom053Report(Input(day, day, inactive.Code));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("C053-EXPORT");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 14);

        GiveStandardDayShift(employee, from.AddDays(-30), to.AddDays(30));

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetLateEarlyLogCustom053ReportToExcel(
            Input(from, to, employee.Code, maxResultCount: 1));

        file.FileName.ShouldBe("LateEarlyLogCustom053.xlsx");

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        CountExportedRows(bytes).ShouldBe(5);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("C053-HDR");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        var file = await _appService.GetLateEarlyLogCustom053ReportToExcel(Input(day, day, employee.Code));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotBeEmpty();
        headers.Where(h => h.StartsWith("[") && h.EndsWith("]")).ShouldBeEmpty();
    }
}
