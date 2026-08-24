using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using MiniExcelLibs;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Shared fixture for the four customer-specific Late/Early Log report suites
/// (<c>uscRptLateEarlyLog_Custom019 / _Custom023 / _Custom033 / _Custom036</c>).
///
/// All four read the SAME six input sets -- employees, punches, duty periods, time policies, absences and
/// org assignments -- so the seeding helpers live here once. What the four disagree about is how a row is
/// SHAPED from those inputs, and that is what each suite tests individually.
/// </summary>
// ReSharper disable once InconsistentNaming
public abstract class LateEarlyLogCustomReportTestBase : AppTestBase
{
    /// <summary>Legacy <c>TransType.Code</c> "1" is the clock-in event, "2" the clock-out event.</summary>
    protected const string PunchInCode = "1";

    protected const string PunchOutCode = "2";

    private int _punchInTransTypeId;
    private int _punchOutTransTypeId;

    protected int TenantId => AbpSession.TenantId ?? 1;

    protected int PunchInTransTypeId => _punchInTransTypeId != 0
        ? _punchInTransTypeId
        : _punchInTransTypeId = CreateTransType(PunchInCode);

    protected int PunchOutTransTypeId => _punchOutTransTypeId != 0
        ? _punchOutTransTypeId
        : _punchOutTransTypeId = CreateTransType(PunchOutCode);

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

    protected Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
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
                AccessID = "ACC-" + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = status
            }).Entity;
        });

        return employee;
    }

    /// <summary>
    /// A duty. The flexi window defaults to the shift window because Custom033/036 select punches through
    /// the FLEXI times regardless of the time policy's management status.
    /// </summary>
    protected int CreateDuty(
        TimeSpan startTime,
        TimeSpan endTime,
        bool isNightShift = false,
        bool isWeeklyOff = false,
        bool isRestDay = false,
        TimeSpan? flexiStartTime = null,
        TimeSpan? flexiEndTime = null)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var duty = context.Set<Duty>().Add(new Duty
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Duty",
                NameAr = "دوام",
                StartTime = startTime,
                EndTime = endTime,
                IsNightShift = isNightShift,
                IsWeeklyOff = isWeeklyOff,
                IsRestDay = isRestDay,
                FlexiStartTime = flexiStartTime ?? startTime,
                FlexiEndTime = flexiEndTime ?? endTime
            }).Entity;
            context.SaveChanges();
            id = duty.Id;
        });

        return id;
    }

    /// <summary>Assigns one duty to every day of the week, for the whole window.</summary>
    protected int CreateDutyPeriod(int dutyId, int? holidayCalendarId = null)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var dutyPeriod = context.Set<DutyPeriod>().Add(new DutyPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "DP",
                NameAr = "DP",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                HolidayCalendarId = holidayCalendarId,
                SundayDutyId = dutyId,
                MondayDutyId = dutyId,
                TuesdayDutyId = dutyId,
                WednesdayDutyId = dutyId,
                ThursdayDutyId = dutyId,
                FridayDutyId = dutyId,
                SaturdayDutyId = dutyId
            }).Entity;
            context.SaveChanges();
            id = dutyPeriod.Id;
        });

        return id;
    }

    /// <summary>
    /// A duty period with ONE weekday left unassigned. Every one of the four reports carries legacy's
    /// STICKY SHIFT DEFECT -- the shift locals live outside both loops and are only reassigned when a Duty
    /// resolves -- and this is what produces a day whose Duty is null so the defect can be pinned.
    /// </summary>
    protected int CreateDutyPeriodMissingOneWeekday(int dutyId, DayOfWeek missingDay, int? holidayCalendarId = null)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            int? DutyFor(DayOfWeek day) => day == missingDay ? (int?)null : dutyId;

            var dutyPeriod = context.Set<DutyPeriod>().Add(new DutyPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "DP-GAP",
                NameAr = "DP-GAP",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                HolidayCalendarId = holidayCalendarId,
                SundayDutyId = DutyFor(DayOfWeek.Sunday),
                MondayDutyId = DutyFor(DayOfWeek.Monday),
                TuesdayDutyId = DutyFor(DayOfWeek.Tuesday),
                WednesdayDutyId = DutyFor(DayOfWeek.Wednesday),
                ThursdayDutyId = DutyFor(DayOfWeek.Thursday),
                FridayDutyId = DutyFor(DayOfWeek.Friday),
                SaturdayDutyId = DutyFor(DayOfWeek.Saturday)
            }).Entity;
            context.SaveChanges();
            id = dutyPeriod.Id;
        });

        return id;
    }

    protected void AssignDutyPeriod(Employee employee, int dutyPeriodId, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DutyPeriodId = dutyPeriodId,
                StartDate = start,
                EndDate = end
            });
        });
    }

    protected int CreateTimePolicy(
        TimeEventRule timeEventRule = TimeEventRule.FirstInLastOut,
        TimeMgmtStatus timeMgmtStatus = TimeMgmtStatus.NA,
        double clockInLimit = 240,
        double clockOutLimit = 240,
        double otMinLimit = 0)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var timePolicy = context.Set<TimePolicy>().Add(new TimePolicy
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Policy",
                NameAr = "سياسة",
                ClockInLimit = clockInLimit,
                ClockOutLimit = clockOutLimit,
                BreakInLimit = 60,
                BreakOutLimit = 60,
                TimeEventRule = timeEventRule,
                TimeMgmtStatus = timeMgmtStatus,
                OTMinLimit = otMinLimit
            }).Entity;
            context.SaveChanges();
            id = timePolicy.Id;
        });

        return id;
    }

    protected void AssignTimePolicy(Employee employee, int timePolicyId, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeTimePolicy>().Add(new EmployeeTimePolicy
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                TimePolicyId = timePolicyId,
                StartDate = start,
                EndDate = end
            });
        });
    }

    protected void CreatePunch(Employee employee, DateTime day, TimeSpan timeOfDay, string transTypeCode)
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
                // Legacy read only the TIME-OF-DAY of TransTime and combined it with TransDate.
                TransTime = day.Date.Add(timeOfDay)
            });
        });
    }

    protected int CreateAbsenceType(bool isPaid = false, bool isReturnDateRequired = false)
    {
        var id = 0;
        UsingDbContext(context =>
        {
            var absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Annual",
                NameAr = "سنوي",
                IsPaid = isPaid,
                IsReturnDateRequired = isReturnDateRequired
            }).Entity;
            context.SaveChanges();
            id = absenceType.Id;
        });

        return id;
    }

    protected void CreateAbsence(
        Employee employee,
        int absenceTypeId,
        DateTime start,
        DateTime end,
        double absenceHours = 0,
        DateTime returnDate = default)
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
                AbsenceHours = absenceHours,
                ReturnDate = returnDate
            });
        });
    }

    /// <summary>
    /// A holiday calendar carrying one public holiday. The calendar must be attached to the DUTY PERIOD
    /// for <c>ITimeDataService</c> to consider the date a public holiday for the employee.
    /// </summary>
    protected int CreateHolidayCalendarWithHoliday(DateTime holidayDate)
    {
        var calendarId = 0;
        UsingDbContext(context =>
        {
            var calendar = context.Set<HolidayCalendar>().Add(new HolidayCalendar
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Cal",
                NameAr = "Cal"
            }).Entity;
            context.SaveChanges();
            calendarId = calendar.Id;

            var holiday = context.Set<Holiday>().Add(new Holiday
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "National Day",
                NameAr = "العيد الوطني",
                HolidayCalendarId = calendar.Id
            }).Entity;
            context.SaveChanges();

            context.Set<HolidayPeriod>().Add(new HolidayPeriod
            {
                TenantId = TenantId,
                HolidayId = holiday.Id,
                HolidayDate = holidayDate.Date
            });
            context.SaveChanges();
        });

        return calendarId;
    }

    /// <summary>
    /// Seeds the minimum an employee needs to produce normal attendance rows: a duty period covering the
    /// window and a time policy covering it.
    /// </summary>
    protected void GiveStandardDayShift(
        Employee employee,
        DateTime from,
        DateTime to,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        TimeEventRule timeEventRule = TimeEventRule.FirstInLastOut,
        TimeMgmtStatus timeMgmtStatus = TimeMgmtStatus.NA)
    {
        var dutyId = CreateDuty(startTime ?? TimeSpan.FromHours(8), endTime ?? TimeSpan.FromHours(16));
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), from, to);
        AssignTimePolicy(employee, CreateTimePolicy(timeEventRule, timeMgmtStatus), from, to);
    }

    /// <summary>
    /// Reads an exported xlsx back and returns its header captions. Used by every suite's
    /// header-resolution test: a localization key with no XML entry renders as "[SomeKey]" in the file --
    /// visible to users, invisible to the compiler and to a source grep.
    /// </summary>
    protected static List<string> ReadExportedHeaders(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        return exported.Count == 0
            ? new List<string>()
            : exported.First().Keys.ToList();
    }

    protected static int CountExportedRows(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().Count();
    }
}
