using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class TimeDataService_Tests : TimeManagementAppServiceTestBase
{
    private TimeDataService CreateService()
    {
        return new TimeDataService(
            FakeRepository.For<HolidayPeriod>(Context),
            FakeRepository.For<DutyPeriod>(Context),
            FakeRepository.For<Duty>(Context),
            FakeRepository.For<EmployeeDutyPeriod>(Context),
            FakeRepository.For<EmployeeSubstituteDuty>(Context),
            FakeRepository.For<RosterGroupDetail>(Context),
            FakeRepository.For<RosterGroupSchedule>(Context),
            FakeRepository.For<ManageTimeSettings>(Context));
    }

    private int CreateNationality()
    {
        var entity = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Local",
            NameAr = "محلي"
        };
        Context.Nationalities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private Employee CreateEmployee(string firstName)
    {
        var entity = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = firstName,
            FirstNameEn = firstName,
            LastNameEn = "Doe",
            FirstNameAr = firstName,
            LastNameAr = "Doe",
            FullNameEn = firstName,
            FullNameAr = firstName,
            NationalityId = CreateNationality()
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity;
    }

    private int CreateBreakShift(TimeSpan start, TimeSpan end)
    {
        var entity = new BreakShift
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Lunch",
            NameAr = "غداء",
            StartTime = start,
            EndTime = end
        };
        Context.BreakShifts.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateDuty(string code, bool isWeeklyOff = false, bool isRestDay = false, TimeSpan? startTime = null, TimeSpan? endTime = null, bool isNightShift = false, int? breakShiftId = null)
    {
        var entity = new Duty
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            IsWeeklyOff = isWeeklyOff,
            IsRestDay = isRestDay,
            StartTime = startTime ?? new TimeSpan(8, 0, 0),
            EndTime = endTime ?? new TimeSpan(17, 0, 0),
            IsNightShift = isNightShift,
            BreakShiftId = breakShiftId
        };
        Context.Duties.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateHolidayCalendar(string code)
    {
        var entity = new HolidayCalendar
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.HolidayCalendars.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateHoliday(int holidayCalendarId, string nameEn)
    {
        var entity = new Holiday
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = nameEn,
            NameAr = nameEn,
            HolidayCalendarId = holidayCalendarId
        };
        Context.Holidays.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private void CreateHolidayPeriod(int holidayId, DateTime date)
    {
        Context.HolidayPeriods.Add(new HolidayPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            HolidayId = holidayId,
            HolidayDate = date
        });
        Context.SaveChanges();
    }

    private int CreateDutyPeriod(int? holidayCalendarId, int workingDutyId, int weeklyOffDutyId, int restDayDutyId = 0)
    {
        // Sunday..Thursday => working duty, Friday => weekly-off duty, Saturday => rest-day duty (or working if not provided).
        var entity = new DutyPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "DP",
            NameAr = "DP",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2030, 12, 31),
            HolidayCalendarId = holidayCalendarId,
            SundayDutyId = workingDutyId,
            MondayDutyId = workingDutyId,
            TuesdayDutyId = workingDutyId,
            WednesdayDutyId = workingDutyId,
            ThursdayDutyId = workingDutyId,
            FridayDutyId = weeklyOffDutyId,
            SaturdayDutyId = restDayDutyId != 0 ? restDayDutyId : workingDutyId
        };
        Context.DutyPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private void AssignEmployeeDutyPeriod(int employeeId, int dutyPeriodId, DateTime startDate, DateTime endDate)
    {
        Context.EmployeeDutyPeriods.Add(new EmployeeDutyPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            DutyPeriodId = dutyPeriodId,
            StartDate = startDate,
            EndDate = endDate
        });
        Context.SaveChanges();
    }

    private void CreateManageTimeSettings(bool enableRosterDuty = false, bool countWeekoffOnPH = false, bool countRestDayOnPH = false)
    {
        Context.Set<ManageTimeSettings>().Add(new ManageTimeSettings
        {
            TenantId = AbpSession.TenantId ?? 1,
            EnableRosterDuty = enableRosterDuty,
            CountWeekoffOnPH = countWeekoffOnPH,
            CountRestDayOnPH = countRestDayOnPH
        });
        Context.SaveChanges();
    }

    public TimeDataService_Tests()
    {
        LoginAsDefaultTenantAdmin();
    }

    // A standard Sun-Thu working week, Friday weekly-off, Saturday a plain working day (no rest-day duty
    // configured), matching a typical GCC work-week -- used by most tests below.
    private (Employee employee, int workingDutyId, int weeklyOffDutyId) SetupStandardWeek(DateTime? empStart = null, DateTime? empEnd = null)
    {
        var employee = CreateEmployee("John");
        var workingDutyId = CreateDuty("WORK");
        var weeklyOffDutyId = CreateDuty("WKOFF", isWeeklyOff: true);
        var dutyPeriodId = CreateDutyPeriod(null, workingDutyId, weeklyOffDutyId);
        AssignEmployeeDutyPeriod(employee.Id, dutyPeriodId, empStart ?? new DateTime(2020, 1, 1), empEnd ?? new DateTime(2030, 12, 31));
        return (employee, workingDutyId, weeklyOffDutyId);
    }

    [Fact]
    public async Task Should_Compute_TotalHoursInDay_With_Break()
    {
        var breakShiftId = CreateBreakShift(new TimeSpan(12, 0, 0), new TimeSpan(13, 0, 0));
        var dutyId = CreateDuty("D1", startTime: new TimeSpan(8, 0, 0), endTime: new TimeSpan(17, 0, 0), breakShiftId: breakShiftId);
        var dutyPeriodId = CreateDutyPeriod(null, dutyId, dutyId);
        var employee = CreateEmployee("Amy");
        AssignEmployeeDutyPeriod(employee.Id, dutyPeriodId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        var service = CreateService();

        var hours = await service.GetTotalHoursInDayAsync(employee, new DateTime(2026, 2, 2)); // Monday

        // 8:00-17:00 minus 12:00-13:00 break = 8 hours.
        hours.ShouldBe(8d);
    }

    [Fact]
    public async Task Should_Compute_TotalHoursInDay_For_NightShift_Without_Break()
    {
        var dutyId = CreateDuty("NIGHT", startTime: new TimeSpan(22, 0, 0), endTime: new TimeSpan(6, 0, 0), isNightShift: true);
        var dutyPeriodId = CreateDutyPeriod(null, dutyId, dutyId);
        var employee = CreateEmployee("Zack");
        AssignEmployeeDutyPeriod(employee.Id, dutyPeriodId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        var service = CreateService();

        var hours = await service.GetTotalHoursInDayAsync(employee, new DateTime(2026, 2, 2));

        hours.ShouldBe(8d);
    }

    [Fact]
    public async Task Should_Detect_WeekEnd_On_Friday()
    {
        var (employee, _, _) = SetupStandardWeek();
        var service = CreateService();

        (await service.IsWeekEndAsync(new DateTime(2026, 2, 6), null, employee)).ShouldBeTrue(); // Friday
        (await service.IsWeekEndAsync(new DateTime(2026, 2, 2), null, employee)).ShouldBeFalse(); // Monday
    }

    [Fact]
    public async Task Should_Not_Flag_WeekEnd_When_AbsenceType_Includes_Weekend()
    {
        var (employee, _, _) = SetupStandardWeek();
        var service = CreateService();
        var absenceType = new AbsenceType { IncludeWeekend = true };

        (await service.IsWeekEndAsync(new DateTime(2026, 2, 6), absenceType, employee)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Detect_RestDay()
    {
        var employee = CreateEmployee("Rest");
        var workingDutyId = CreateDuty("WORK2");
        var restDutyId = CreateDuty("REST", isRestDay: true);
        var dutyPeriodId = CreateDutyPeriod(null, workingDutyId, workingDutyId, restDutyId);
        AssignEmployeeDutyPeriod(employee.Id, dutyPeriodId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        var service = CreateService();

        (await service.IsRestDayAsync(new DateTime(2026, 2, 7), null, employee)).ShouldBeTrue(); // Saturday
        (await service.IsRestDayAsync(new DateTime(2026, 2, 2), null, employee)).ShouldBeFalse(); // Monday
    }

    [Fact]
    public async Task Should_Count_Weekly_Off_Days_In_Range()
    {
        var (employee, _, _) = SetupStandardWeek();
        var service = CreateService();

        // One full week: exactly one Friday should count as weekly off.
        var count = await service.GetWeeklyOffDaysCountAsync(employee, new DateTime(2026, 2, 1), new DateTime(2026, 2, 7));

        count.ShouldBe(1d);
    }

    [Fact]
    public async Task Should_Override_WeekEnd_With_EmployeeSubstituteDuty()
    {
        var (employee, workingDutyId, _) = SetupStandardWeek();

        // Friday 2026-02-06 is normally weekly-off; a substitute duty converts it to a working day.
        Context.EmployeeSubstituteDuties.Add(new EmployeeSubstituteDuty
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employee.Id,
            DutyId = workingDutyId,
            SubstituteDate = new DateTime(2026, 2, 6)
        });
        Context.SaveChanges();

        var service = CreateService();

        (await service.IsWeekEndAsync(new DateTime(2026, 2, 6), null, employee)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Detect_Holiday_And_Public_Holiday_Dates()
    {
        var (employee, _, _) = SetupStandardWeek();
        var holidayCalendarId = CreateHolidayCalendar("HC1");

        // Re-point the employee's DutyPeriod at the holiday calendar.
        var dutyPeriod = Context.DutyPeriods.First();
        dutyPeriod.HolidayCalendarId = holidayCalendarId;
        Context.SaveChanges();

        var holidayId = CreateHoliday(holidayCalendarId, "New Year");
        CreateHolidayPeriod(holidayId, new DateTime(2026, 3, 1));

        var service = CreateService();

        (await service.IsHolidayAsync(new DateTime(2026, 3, 1), null, employee)).ShouldBeTrue();
        (await service.IsHolidayAsync(new DateTime(2026, 3, 2), null, employee)).ShouldBeFalse();

        var dates = await service.GetPublicHolidaysDatesListAsync(employee.Id, new DateTime(2026, 2, 25), new DateTime(2026, 3, 5));
        dates.Count.ShouldBe(1);
        dates[0].HolidayName.ShouldBe("New Year");
    }

    [Fact]
    public async Task Should_Exclude_Holiday_Falling_On_WeeklyOff_When_CountWeekoffOnPH_Enabled()
    {
        var (employee, _, _) = SetupStandardWeek();
        var holidayCalendarId = CreateHolidayCalendar("HC2");

        var dutyPeriod = Context.DutyPeriods.First();
        dutyPeriod.HolidayCalendarId = holidayCalendarId;
        Context.SaveChanges();

        var holidayId = CreateHoliday(holidayCalendarId, "Falls On Friday");
        CreateHolidayPeriod(holidayId, new DateTime(2026, 2, 6)); // Friday, already weekly-off

        CreateManageTimeSettings(countWeekoffOnPH: true);

        var service = CreateService();

        var dates = await service.GetPublicHolidaysDatesAsync(employee, new DateTime(2026, 2, 1), new DateTime(2026, 2, 10));

        dates.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Find_First_RegularDay_Skipping_Holiday_And_WeekEnd()
    {
        var (employee, _, _) = SetupStandardWeek();
        var holidayCalendarId = CreateHolidayCalendar("HC3");

        var dutyPeriod = Context.DutyPeriods.First();
        dutyPeriod.HolidayCalendarId = holidayCalendarId;
        Context.SaveChanges();

        var holidayId = CreateHoliday(holidayCalendarId, "Monday Off");
        CreateHolidayPeriod(holidayId, new DateTime(2026, 2, 2)); // Monday is a public holiday

        var service = CreateService();

        // Range starts Sunday 2026-02-01: Sunday is regular, so it should be returned immediately.
        var regularDay = await service.GetRegularDayAsync(new DateTime(2026, 2, 1), new DateTime(2026, 2, 10), null, employee);

        regularDay.ShouldBe(new DateTime(2026, 2, 1));
    }

    [Fact]
    public async Task Should_Resolve_WeekEnd_Via_RosterGroup_When_EnableRosterDuty()
    {
        CreateManageTimeSettings(enableRosterDuty: true);

        var employee = CreateEmployee("Rider");
        var workingDutyId = CreateDuty("RWORK");
        var weeklyOffDutyId = CreateDuty("RWKOFF", isWeeklyOff: true);
        var dutyPeriodId = CreateDutyPeriod(null, workingDutyId, weeklyOffDutyId);

        var rosterGroup = new RosterGroup { TenantId = AbpSession.TenantId ?? 1, NameEn = "RG", NameAr = "RG" };
        Context.RosterGroups.Add(rosterGroup);
        Context.SaveChanges();

        Context.RosterGroupDetails.Add(new RosterGroupDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            RosterGroupId = rosterGroup.Id,
            EmployeeId = employee.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });
        Context.RosterGroupSchedules.Add(new RosterGroupSchedule
        {
            TenantId = AbpSession.TenantId ?? 1,
            RosterGroupId = rosterGroup.Id,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });
        Context.SaveChanges();

        var service = CreateService();

        (await service.IsWeekEndAsync(new DateTime(2026, 2, 6), null, employee)).ShouldBeTrue(); // Friday
        (await service.IsWeekEndAsync(new DateTime(2026, 2, 2), null, employee)).ShouldBeFalse(); // Monday

        var hours = await service.GetTotalHoursInDayAsync(employee, new DateTime(2026, 2, 2));
        hours.ShouldBe(9d); // default duty StartTime 08:00, EndTime 17:00, no break.
    }
}
