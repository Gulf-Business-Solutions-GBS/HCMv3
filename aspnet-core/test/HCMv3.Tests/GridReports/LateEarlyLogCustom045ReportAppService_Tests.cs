using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Custom045 Late/Early Log grid report (legacy
/// <c>uscRptLateEarlyLog_Custom045.ascx</c>, <c>GetTimeLogData(...)</c>).
///
/// Custom045's defining change is a DIFFERENT PUNCH MODEL -- one flexi window for the whole day and four
/// transaction types instead of two -- so most of these tests exercise that, alongside the two extra
/// columns and the changed full-day-leave arithmetic.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LateEarlyLogCustom045ReportAppService_Tests : LateEarlyLogCustomReportTestBase
{
    /// <summary>
    /// CUSTOM045-ONLY transaction types. The base control has no concept of these: it infers the break
    /// from ordinary clock-in/clock-out punches falling inside a break window.
    /// </summary>
    private const string BreakOutCode = "3";

    private const string BreakInCode = "4";

    private readonly ILateEarlyLogCustom045ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public LateEarlyLogCustom045ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILateEarlyLogCustom045ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private static GetLateEarlyLogCustom045ReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        string payrollAreaCode = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetLateEarlyLogCustom045ReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            PayrollAreaCode = payrollAreaCode,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    /// <summary>Creates a punch of an ARBITRARY transaction type -- the base fixture only knows "1"/"2".</summary>
    private void CreateTypedPunch(Employee employee, DateTime day, TimeSpan timeOfDay, string transTypeCode)
    {
        UsingDbContext(context =>
        {
            var transType = context.Set<TransType>()
                .FirstOrDefault(t => t.TenantId == TenantId && t.Code == transTypeCode);

            if (transType == null)
            {
                transType = context.Set<TransType>().Add(new TransType
                {
                    TenantId = TenantId,
                    Code = transTypeCode,
                    NameEn = "T" + transTypeCode,
                    NameAr = "T" + transTypeCode
                }).Entity;
                context.SaveChanges();
            }

            context.Set<TimeEvent>().Add(new TimeEvent
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                TransTypeId = transType.Id,
                TransDate = day.Date,
                TransTime = day.Date.Add(timeOfDay)
            });
        });
    }

    /// <summary>Attaches an OvertimeType to a duty so the two Custom045-only columns have something to show.</summary>
    private int CreateDutyWithOvertimeType(TimeSpan start, TimeSpan end, string overtimeTypeNameEn)
    {
        var dutyId = CreateDuty(start, end);

        UsingDbContext(context =>
        {
            var overtimeType = context.Set<OvertimeType>().Add(new OvertimeType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = overtimeTypeNameEn,
                NameAr = "عربي " + overtimeTypeNameEn
            }).Entity;
            context.SaveChanges();

            var duty = context.Set<Duty>().Single(d => d.Id == dutyId);
            duty.OvertimeTypeId = overtimeType.Id;
            context.SaveChanges();
        });

        return dutyId;
    }

    [Fact]
    public async Task Should_Include_Both_Boundary_Days_Of_The_Window()
    {
        var employee = CreateEmployee("C045-BOUND");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 12);

        GiveStandardDayShift(employee, from.AddDays(-30), to.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom045Report(Input(from, to, employee.Code));

        result.Items.Select(r => r.Date).OrderBy(d => d)
            .ShouldBe(new[] { from, from.AddDays(1), to });
    }

    [Fact]
    public async Task Should_Return_Nothing_When_FromDate_Is_After_ToDate()
    {
        var employee = CreateEmployee("C045-INVERTED");
        GiveStandardDayShift(employee, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetLateEarlyLogCustom045Report(
            Input(new DateTime(2024, 6, 12), new DateTime(2024, 6, 10), employee.Code));

        result.TotalCount.ShouldBe(0);
    }

    /// <summary>
    /// THE DEFINING CUSTOM045 CHANGE (legacy lines 1130-1131). The break pair comes from DEDICATED
    /// transaction types "3" and "4", not from clock-in/clock-out punches inside a break window, and the
    /// duty needs no break shift at all for them to be picked up.
    /// </summary>
    [Fact]
    public async Task Should_Read_The_Break_Pair_From_Its_Own_Transaction_Types_Three_And_Four()
    {
        var employee = CreateEmployee("C045-BREAK");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);
        CreateTypedPunch(employee, day, TimeSpan.FromHours(12), BreakOutCode);
        CreateTypedPunch(employee, day, TimeSpan.FromHours(13), BreakInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom045Report(Input(day, day, employee.Code));

        var row = result.Items.Single();
        row.BreakOut.ShouldBe(TimeSpan.FromHours(12));
        row.BreakIn.ShouldBe(TimeSpan.FromHours(13));
        row.BreakHours.ShouldBe(TimeSpan.FromHours(1));

        // Total hours are net of the MEASURED break: 08:00 -> 16:00 minus the hour off.
        row.TotalHours.ShouldBe(TimeSpan.FromHours(7));
    }

    /// <summary>
    /// The window is <c>[day + FlexiStartTime, day + FlexiEndTime]</c> with NO tolerance limits and no
    /// night-shift extension, so a punch outside the flexi span is simply not seen -- the base's
    /// ClockInLimit would have caught it.
    /// </summary>
    [Fact]
    public async Task Should_Ignore_A_Punch_Outside_The_Flexi_Window_Even_Within_The_Clock_In_Limit()
    {
        var employee = CreateEmployee("C045-WINDOW");
        var day = new DateTime(2024, 6, 10);

        // Flexi window 08:00-16:00, but a generous four-hour ClockInLimit that Custom045 never consults.
        var dutyId = CreateDuty(
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(16),
            flexiStartTime: TimeSpan.FromHours(8),
            flexiEndTime: TimeSpan.FromHours(16));

        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), day.AddDays(-30), day.AddDays(30));
        AssignTimePolicy(employee, CreateTimePolicy(clockInLimit: 240), day.AddDays(-30), day.AddDays(30));

        // 06:00 is two hours before the flexi window opens but well inside the clock-in limit.
        CreatePunch(employee, day, TimeSpan.FromHours(6), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom045Report(Input(day, day, employee.Code));

        var row = result.Items.Single();
        row.SignIn.ShouldBeNull();
        row.Status.ShouldBe("Missed In");
    }

    /// <summary>
    /// Custom045 KEEPS the base's status wording, unlike Custom040 -- the customer's alternative
    /// single-punch wording is present in the legacy source but commented out.
    /// </summary>
    [Fact]
    public async Task Should_Keep_The_Base_Missed_In_Missed_Out_Status_Wording()
    {
        var employee = CreateEmployee("C045-STATUS");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom045Report(Input(day, day, employee.Code));

        result.Items.Single().Status.ShouldBe("Missed In Missed Out");
    }

    [Fact]
    public async Task Should_Report_The_Duty_Overtime_Type_On_The_Row()
    {
        var employee = CreateEmployee("C045-OT");
        var day = new DateTime(2024, 6, 10);

        var dutyId = CreateDutyWithOvertimeType(TimeSpan.FromHours(8), TimeSpan.FromHours(16), "Weekday OT");
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), day.AddDays(-30), day.AddDays(30));
        AssignTimePolicy(employee, CreateTimePolicy(), day.AddDays(-30), day.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom045Report(Input(day, day, employee.Code));

        var row = result.Items.Single();
        row.OvertimeTypeName.ShouldBe("Weekday OT");
        row.OvertimeTypeId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Report_Minus_One_As_The_Overtime_Type_Id_When_The_Duty_Has_None()
    {
        var employee = CreateEmployee("C045-NO-OT");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom045Report(Input(day, day, employee.Code));

        var row = result.Items.Single();
        row.OvertimeTypeName.ShouldBe(string.Empty);
        // Legacy's own "none" sentinel (line 1019), not 0 and not null.
        row.OvertimeTypeId.ShouldBe(-1);
    }

    /// <summary>
    /// LEGACY CHANGE PINNED (base line 470 vs Custom045 line 494). Custom045 DELETED the base's
    /// <c>absencehours = plannedHours</c>, so a full-day leave no longer charges the planned shift -- and
    /// the cell is written only for an UNPAID absence type.
    /// </summary>
    [Fact]
    public async Task Should_Not_Charge_The_Planned_Shift_As_Absence_On_A_Full_Day_Leave()
    {
        var unpaidEmployee = CreateEmployee("C045-LEAVE-UNPAID");
        var paidEmployee = CreateEmployee("C045-LEAVE-PAID");
        var day = new DateTime(2024, 6, 10);

        foreach (var employee in new[] { unpaidEmployee, paidEmployee })
        {
            GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));
        }

        CreateAbsence(unpaidEmployee, CreateAbsenceType(isPaid: false), day, day);
        CreateAbsence(paidEmployee, CreateAbsenceType(isPaid: true), day, day);

        var result = await _appService.GetLateEarlyLogCustom045Report(Input(day, day));

        var unpaidRow = result.Items.Single(r => r.EmployeeCode == unpaidEmployee.Code);
        unpaidRow.Status.ShouldBe("Leave");
        // ZERO, not the eight-hour planned shift the base would have charged.
        unpaidRow.AbsenceHours.ShouldBe(TimeSpan.Zero);
        unpaidRow.PlannedHours.ShouldBe(TimeSpan.FromHours(8));

        var paidRow = result.Items.Single(r => r.EmployeeCode == paidEmployee.Code);
        // A PAID absence leaves the cell blank altogether.
        paidRow.AbsenceHours.ShouldBeNull();
        paidRow.PlannedHours.ShouldBe(TimeSpan.FromHours(8));
    }

    /// <summary>
    /// LEGACY DEFECT PINNED (legacy line 819). As in Custom040, planned hours on a night shift are
    /// <c>shiftStart - shiftEnd</c> computed from bare times, so a 22:00-06:00 shift reports SIXTEEN hours.
    /// </summary>
    [Fact]
    public async Task Should_Report_Sixteen_Planned_Hours_For_An_Eight_Hour_Night_Shift_Reproducing_Legacy_Defect()
    {
        var employee = CreateEmployee("C045-NIGHT");
        var day = new DateTime(2024, 6, 10);

        var dutyId = CreateDuty(TimeSpan.FromHours(22), TimeSpan.FromHours(6), isNightShift: true);
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), day.AddDays(-30), day.AddDays(30));
        AssignTimePolicy(employee, CreateTimePolicy(), day.AddDays(-30), day.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom045Report(Input(day, day, employee.Code));

        result.Items.Single().PlannedHours.ShouldBe(TimeSpan.FromHours(16));
    }

    [Fact]
    public async Task Should_Skip_An_Employee_With_No_Duty_Period_In_The_Window()
    {
        var employee = CreateEmployee("C045-NODP");
        var day = new DateTime(2024, 6, 10);

        AssignTimePolicy(employee, CreateTimePolicy(), day.AddDays(-30), day.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom045Report(Input(day, day, employee.Code));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Exclude_Inactive_Employees()
    {
        var day = new DateTime(2024, 6, 10);
        var inactive = CreateEmployee("C045-INACTIVE", EmployeeStatus.Inactive);
        GiveStandardDayShift(inactive, day.AddDays(-30), day.AddDays(30));

        var result = await _appService.GetLateEarlyLogCustom045Report(Input(day, day, inactive.Code));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("C045-EXPORT");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 14);

        GiveStandardDayShift(employee, from.AddDays(-30), to.AddDays(30));

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetLateEarlyLogCustom045ReportToExcel(
            Input(from, to, employee.Code, maxResultCount: 1));

        file.FileName.ShouldBe("LateEarlyLogCustom045.xlsx");

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        CountExportedRows(bytes).ShouldBe(5);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption -- including the Custom045-only OvertimeType
    /// column, which is exactly the kind of new key that goes missing from the XML.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("C045-HDR");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        var file = await _appService.GetLateEarlyLogCustom045ReportToExcel(Input(day, day, employee.Code));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotBeEmpty();
        headers.Where(h => h.StartsWith("[") && h.EndsWith("]")).ShouldBeEmpty();
    }

    /// <summary>
    /// OvertimeTypeOid is a raw database id nothing in the legacy grid read -- it exists for the ESS
    /// caller -- so it must NOT reach the spreadsheet, exactly as the row-sequence Oid does not.
    /// </summary>
    [Fact]
    public async Task Should_Export_The_Overtime_Type_Caption_But_Not_Its_Technical_Id()
    {
        var employee = CreateEmployee("C045-HDR-OT");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, day.AddDays(-30), day.AddDays(30));

        var file = await _appService.GetLateEarlyLogCustom045ReportToExcel(Input(day, day, employee.Code));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldContain("Overtime Type");
        headers.ShouldNotContain("OvertimeTypeOid");
        headers.ShouldNotContain("OvertimeTypeId");
    }
}
