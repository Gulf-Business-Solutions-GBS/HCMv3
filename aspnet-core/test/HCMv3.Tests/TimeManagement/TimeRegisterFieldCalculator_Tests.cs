using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// Unit tests for the wave-T2 port of legacy <c>HCM10.TimeEngine.GeneralTimeService</c>.
///
/// <para>The service under test is constructed directly with list-backed
/// <see cref="IRepository{TEntity}"/> substitutes rather than resolved from the ABP container: it
/// constructor-injects <see cref="IPunchResolutionStrategyResolver"/>, whose implementations are wave T3 and do
/// not exist yet, so container resolution would fail. Every query in <c>TimeRegisterFieldCalculator</c> is
/// plain synchronous LINQ over <c>IRepository&lt;T&gt;.GetAll()</c> (HCMv3.Core has no EF Core reference), so a
/// <c>List&lt;T&gt;.AsQueryable()</c> is a faithful stand-in -- no async LINQ provider is needed here, unlike
/// the app-service tests that use <see cref="FakeRepository"/>.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeRegisterFieldCalculator_Tests
{
    private static readonly DateTime Day = new DateTime(2024, 6, 10);

    private readonly List<HolidayPeriod> _holidayPeriods = new List<HolidayPeriod>();
    private readonly List<Holiday> _holidays = new List<Holiday>();
    private readonly List<AbsenceType> _absenceTypes = new List<AbsenceType>();
    private readonly List<AttendanceType> _attendanceTypes = new List<AttendanceType>();
    private readonly List<WageType> _wageTypes = new List<WageType>();
    private readonly List<EmployeeAttendance> _employeeAttendances = new List<EmployeeAttendance>();
    private readonly List<EmployeeOvertime> _employeeOvertimes = new List<EmployeeOvertime>();
    private readonly List<EmployeeTimesheet> _employeeTimesheets = new List<EmployeeTimesheet>();
    private readonly List<EmployeeSubGroupTicket> _employeeSubGroupTickets = new List<EmployeeSubGroupTicket>();
    private readonly List<TimeEvent> _timeEvents = new List<TimeEvent>();
    private readonly List<TransType> _transTypes = new List<TransType>();

    private readonly IPunchResolutionStrategyResolver _punchResolver =
        Substitute.For<IPunchResolutionStrategyResolver>();

    private TimeRegisterFieldCalculator CreateCalculator()
    {
        return new TimeRegisterFieldCalculator(
            ListRepository(_holidayPeriods),
            ListRepository(_holidays),
            ListRepository(_absenceTypes),
            ListRepository(_attendanceTypes),
            ListRepository(_wageTypes),
            ListRepository(_employeeAttendances),
            ListRepository(_employeeOvertimes),
            ListRepository(_employeeTimesheets),
            ListRepository(_employeeSubGroupTickets),
            ListRepository(_timeEvents),
            ListRepository(_transTypes),
            _punchResolver);
    }

    #region << Fakes / builders >>

    private static IRepository<TEntity> ListRepository<TEntity>(List<TEntity> store)
        where TEntity : class, IEntity<int>
    {
        var repository = Substitute.For<IRepository<TEntity>>();
        var nextId = 1;

        repository.GetAll().Returns(_ => store.AsQueryable());

        repository.InsertAsync(Arg.Any<TEntity>()).Returns(call =>
        {
            var entity = call.Arg<TEntity>();
            if (entity.Id == 0)
            {
                entity.Id = nextId++;
            }

            if (!store.Contains(entity))
            {
                store.Add(entity);
            }

            return Task.FromResult(entity);
        });

        repository.UpdateAsync(Arg.Any<TEntity>()).Returns(call => Task.FromResult(call.Arg<TEntity>()));

        repository.DeleteAsync(Arg.Any<int>()).Returns(call =>
        {
            var id = call.Arg<int>();
            store.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        });

        return repository;
    }

    private static T WithId<T>(T entity, int id) where T : Entity<int>
    {
        entity.Id = id;
        return entity;
    }

    private TimeRegisterDayContext NewContext(
        Duty duty = null,
        TimePolicy timePolicy = null,
        DutyPeriod dutyPeriod = null,
        Employee employee = null)
    {
        duty ??= WithId(new Duty
        {
            TenantId = 1,
            NameEn = "General",
            NameAr = "General",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        }, 100);

        timePolicy ??= WithId(new TimePolicy
        {
            TenantId = 1,
            NameEn = "Policy",
            NameAr = "Policy",
            TimeMgmtStatus = TimeMgmtStatus.ActualTime,
            ShouldSignIn = true,
            ShouldSignOut = true
        }, 200);

        employee ??= WithId(new Employee
        {
            TenantId = 1,
            FirstNameEn = "Test",
            LastNameEn = "Employee",
            FirstNameAr = "Test",
            LastNameAr = "Employee",
            FullNameEn = "Test Employee",
            FullNameAr = "Test Employee",
            JoiningDate = new DateTime(2020, 1, 1)
        }, 300);

        var timeRegister = new TimeRegister
        {
            TenantId = 1,
            EmployeeId = employee.Id,
            DayDate = Day,
            Duty = duty,
            DutyId = duty.Id,
            TimePolicy = timePolicy,
            TimePolicyId = timePolicy.Id,
            DutyPeriod = dutyPeriod,
            DutyPeriodId = dutyPeriod?.Id,
            ShiftStartTime1 = Day.Date.Add(duty.StartTime),
            ShiftEndTime1 = Day.Date.Add(duty.EndTime)
        };

        return new TimeRegisterDayContext
        {
            TimeRegister = timeRegister,
            DayDate = Day,
            Employee = employee,
            Duty = duty,
            DutyPeriod = dutyPeriod,
            TimePolicy = timePolicy,
            EmployeeContext = new TimeEngineEmployeeContext
            {
                Employee = employee,
                StartDate = Day,
                EndDate = Day,
                CutoffDate = Day
            },
            Settings = new TimeEngineSettings(),
            MasterData = new TimeEngineMasterData()
        };
    }

    private AbsenceType NewAbsenceType(int id, string code, bool isPaid = true,
        bool includePublicHoliday = false, bool includeWeekend = false, bool includeRestDay = false)
    {
        var absenceType = WithId(new AbsenceType
        {
            TenantId = 1,
            Code = code,
            NameEn = "Leave " + code,
            NameAr = "Leave " + code,
            IsPaid = isPaid,
            IncludePublicHoliday = includePublicHoliday,
            IncludeWeekend = includeWeekend,
            IncludeRestDay = includeRestDay
        }, id);

        _absenceTypes.Add(absenceType);
        return absenceType;
    }

    private static EmployeeAbsence NewAbsence(int employeeId, AbsenceType absenceType, DateTime from, DateTime to,
        double absenceHours = 0, double totalLeaveDays = 1,
        TimeSpan? startTime = null, TimeSpan? endTime = null)
    {
        return new EmployeeAbsence
        {
            TenantId = 1,
            EmployeeId = employeeId,
            AbsenceTypeId = absenceType.Id,
            AbsenceType = absenceType,
            StartDate = from,
            EndDate = to,
            AbsenceHours = absenceHours,
            TotalLeaveDays = totalLeaveDays,
            StartTime = from.Date.Add(startTime ?? TimeSpan.Zero),
            EndTime = from.Date.Add(endTime ?? TimeSpan.Zero)
        };
    }

    #endregion

    #region << General Calculation >>

    [Fact]
    public async Task GetActiveDay_Should_Be_1_When_A_Contract_Covers_The_Day()
    {
        var context = NewContext();
        context.EmployeeContext.Contracts.Add(new EmployeeContract
        {
            TenantId = 1,
            EmployeeId = context.Employee.Id,
            StartDate = Day.AddDays(-10),
            EndDate = Day.AddDays(10)
        });

        (await CreateCalculator().GetActiveDayAsync(context)).ShouldBe(1);
    }

    [Fact]
    public async Task GetActiveDay_Should_Be_0_When_No_Contract_Covers_The_Day()
    {
        var context = NewContext();
        context.EmployeeContext.Contracts.Add(new EmployeeContract
        {
            TenantId = 1,
            EmployeeId = context.Employee.Id,
            StartDate = Day.AddDays(-30),
            EndDate = Day.AddDays(-20)
        });

        (await CreateCalculator().GetActiveDayAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetPublicHoliday_Should_Be_1_On_A_Holiday_Date_Of_The_DutyPeriods_Calendar()
    {
        var context = ContextWithHolidayCalendar(out _, isHolidayToday: true);

        (await CreateCalculator().GetPublicHolidayAsync(context)).ShouldBe(1);
    }

    [Fact]
    public async Task GetPublicHoliday_Should_Be_0_When_The_Date_Is_Not_A_Holiday()
    {
        var context = ContextWithHolidayCalendar(out _, isHolidayToday: false);

        (await CreateCalculator().GetPublicHolidayAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetPublicHoliday_Should_Be_0_When_WeekOff_Wins_Over_Holiday_For_FixedDays_BasicPay()
    {
        var context = ContextWithHolidayCalendar(out var duty, isHolidayToday: true);
        duty.IsWeeklyOff = true;
        context.TimePolicy.MarkWeekoffOnHoliday = true;
        context.MasterData.WageTypes.Add(WithId(new WageType
        {
            TenantId = 1,
            Code = "1000",
            NameEn = "Basic",
            NameAr = "Basic",
            BillingMethod = BillingMethod.FixedDaysPerMonth
        }, 900));

        (await CreateCalculator().GetPublicHolidayAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetPublicHoliday_Should_Throw_When_DutyPeriod_Has_No_HolidayCalendar()
    {
        var context = NewContext(dutyPeriod: WithId(new DutyPeriod
        {
            TenantId = 1,
            NameEn = "DP",
            NameAr = "DP"
        }, 400));

        await Assert.ThrowsAsync<Exception>(() => CreateCalculator().GetPublicHolidayAsync(context));
    }

    private TimeRegisterDayContext ContextWithHolidayCalendar(out Duty duty, bool isHolidayToday)
    {
        var dutyPeriod = WithId(new DutyPeriod
        {
            TenantId = 1,
            NameEn = "DP",
            NameAr = "DP",
            HolidayCalendarId = 500
        }, 400);

        _holidays.Add(WithId(new Holiday
        {
            TenantId = 1,
            Code = "H1",
            NameEn = "National Day",
            NameAr = "National Day",
            HolidayCalendarId = 500,
            OvertimeTypeId = 777
        }, 600));

        _holidayPeriods.Add(WithId(new HolidayPeriod
        {
            TenantId = 1,
            HolidayId = 600,
            HolidayDate = isHolidayToday ? Day : Day.AddDays(5)
        }, 700));

        var context = NewContext(dutyPeriod: dutyPeriod);
        duty = context.Duty;
        return context;
    }

    #endregion

    #region << Planned Calculation >>

    [Fact]
    public async Task GetPlannedWorkingMinutes_Should_Be_Shift_Length_Without_A_BreakShift()
    {
        var context = NewContext();

        (await CreateCalculator().GetPlannedWorkingMinutesAsync(context)).ShouldBe(540); // 08:00-17:00
    }

    [Fact]
    public async Task GetPlannedWorkingMinutes_Should_Exclude_The_Break_When_A_BreakShift_Exists()
    {
        var context = NewContext();
        context.Duty.BreakShift = WithId(new BreakShift
        {
            TenantId = 1,
            NameEn = "Lunch",
            NameAr = "Lunch",
            StartTime = new TimeSpan(12, 0, 0),
            EndTime = new TimeSpan(13, 0, 0)
        }, 800);
        context.TimeRegister.BreakStartTime = Day.Date.AddHours(12);
        context.TimeRegister.BreakEndTime = Day.Date.AddHours(13);

        (await CreateCalculator().GetPlannedWorkingMinutesAsync(context)).ShouldBe(480);
    }

    [Fact]
    public async Task GetPlannedWorkingMinutes_Should_Be_0_On_A_Public_Holiday()
    {
        var context = NewContext();
        context.TimeRegister.PublicHoliday = 1;

        (await CreateCalculator().GetPlannedWorkingMinutesAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetPlannedWorkingDay_Should_Follow_PlannedWorkingMins()
    {
        var calculator = CreateCalculator();

        var context = NewContext();
        context.TimeRegister.PlannedWorkingMins = 540;
        (await calculator.GetPlannedWorkingDayAsync(context)).ShouldBe(1);

        context.TimeRegister.PlannedWorkingMins = 0;
        (await calculator.GetPlannedWorkingDayAsync(context)).ShouldBe(0);
    }

    #endregion

    #region << Leave Calculation >>

    [Fact]
    public async Task GetAnnualPaidLeaveDay_Should_Be_1_For_A_FullDay_Annual_Leave()
    {
        var context = NewContext();
        var annualLeave = NewAbsenceType(1, "1");
        context.MasterData.AnnualLeave = annualLeave;
        context.MasterData.AnnualLeaveTypes.Add(annualLeave);
        context.MasterData.AbsenceTypes.Add(annualLeave);
        context.EmployeeContext.Absences.Add(NewAbsence(context.Employee.Id, annualLeave, Day, Day));

        (await CreateCalculator().GetAnnualPaidLeaveDayAsync(context)).ShouldBe(1);
    }

    [Fact]
    public async Task GetAnnualPaidLeaveDay_Should_Be_0_When_The_Type_Excludes_Weekends_And_The_Day_Is_A_WeekOff()
    {
        var context = NewContext();
        var annualLeave = NewAbsenceType(1, "1", includeWeekend: false);
        context.MasterData.AnnualLeave = annualLeave;
        context.MasterData.AnnualLeaveTypes.Add(annualLeave);
        context.MasterData.AbsenceTypes.Add(annualLeave);
        context.TimeRegister.WeeklyOff = 1;
        context.EmployeeContext.Absences.Add(NewAbsence(context.Employee.Id, annualLeave, Day, Day));

        (await CreateCalculator().GetAnnualPaidLeaveDayAsync(context)).ShouldBe(0);
        context.TimeRegister.WeeklyOff.ShouldBe(1); // flag NOT cleared when the count was zeroed
    }

    [Fact]
    public async Task GetAnnualPaidLeaveDay_Should_Clear_WeeklyOff_When_The_Type_Includes_Weekends()
    {
        var context = NewContext();
        var annualLeave = NewAbsenceType(1, "1", includeWeekend: true);
        context.MasterData.AnnualLeave = annualLeave;
        context.MasterData.AnnualLeaveTypes.Add(annualLeave);
        context.MasterData.AbsenceTypes.Add(annualLeave);
        context.TimeRegister.WeeklyOff = 1;
        context.EmployeeContext.Absences.Add(NewAbsence(context.Employee.Id, annualLeave, Day, Day));

        (await CreateCalculator().GetAnnualPaidLeaveDayAsync(context)).ShouldBe(1);
        context.TimeRegister.WeeklyOff.ShouldBe(0);
    }

    [Fact]
    public async Task GetUnPaidLeaveDay_Should_Be_1_For_A_FullDay_Unpaid_Leave()
    {
        var context = NewContext();
        var unpaid = NewAbsenceType(2, "2", isPaid: false);
        context.MasterData.UnpaidLeave = unpaid;
        context.MasterData.AbsenceTypes.Add(unpaid);
        context.EmployeeContext.Absences.Add(NewAbsence(context.Employee.Id, unpaid, Day, Day));

        (await CreateCalculator().GetUnPaidLeaveDayAsync(context)).ShouldBe(1);
    }

    [Fact]
    public async Task GetSickLeavePaidDay_And_GetSickLeaveUnPaidDay_Should_Count_Their_Own_Types()
    {
        var context = NewContext();
        var sickPaid = NewAbsenceType(3, "3");
        var sickUnpaid = NewAbsenceType(5, "5", isPaid: false);
        context.MasterData.SickLeavePaid = sickPaid;
        context.MasterData.SickLeaveUnpaid = sickUnpaid;
        context.MasterData.AbsenceTypes.Add(sickPaid);
        context.MasterData.AbsenceTypes.Add(sickUnpaid);
        context.EmployeeContext.Absences.Add(NewAbsence(context.Employee.Id, sickPaid, Day, Day));

        var calculator = CreateCalculator();
        (await calculator.GetSickLeavePaidDayAsync(context)).ShouldBe(1);
        (await calculator.GetSickLeaveUnPaidDayAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetLeaveTypeId_Should_Return_The_Absence_Type_Covering_The_Day()
    {
        var context = NewContext();
        var annualLeave = NewAbsenceType(1, "1");
        context.MasterData.AbsenceTypes.Add(annualLeave);
        context.EmployeeContext.Absences.Add(NewAbsence(context.Employee.Id, annualLeave, Day.AddDays(-1), Day.AddDays(1)));

        (await CreateCalculator().GetLeaveTypeIdAsync(context)).ShouldBe(annualLeave.Id);
    }

    [Fact]
    public async Task GetLeaveTypeId_Should_Return_Null_When_No_Absence_Covers_The_Day()
    {
        var context = NewContext();

        (await CreateCalculator().GetLeaveTypeIdAsync(context)).ShouldBeNull();
    }

    [Fact]
    public async Task GetLeaveDay_Should_Reuse_The_Already_Calculated_AnnualLeaveDay()
    {
        var context = NewContext();
        var annualLeave = NewAbsenceType(1, "1");
        context.MasterData.AbsenceTypes.Add(annualLeave);
        context.TimeRegister.LeaveTypeId = annualLeave.Id;
        context.TimeRegister.AnnualLeaveDay = 1;

        (await CreateCalculator().GetLeaveDayAsync(context)).ShouldBe(1);
    }

    [Fact]
    public async Task GetLeaveMinutes_Should_Be_PlannedWorkingMins_For_A_FullDay_Leave()
    {
        var context = NewContext();
        var annualLeave = NewAbsenceType(1, "1");
        context.MasterData.AbsenceTypes.Add(annualLeave);
        context.TimeRegister.LeaveTypeId = annualLeave.Id;
        context.TimeRegister.PlannedWorkingMins = 540;
        context.EmployeeContext.Absences.Add(NewAbsence(context.Employee.Id, annualLeave, Day, Day));

        (await CreateCalculator().GetLeaveMinutesAsync(context)).ShouldBe(540);
    }

    [Fact]
    public async Task GetLeaveMinutes_Should_Sum_Hourly_Leave_Windows()
    {
        var context = NewContext();
        var hourly = NewAbsenceType(7, "7");
        context.MasterData.AbsenceTypes.Add(hourly);
        context.TimeRegister.LeaveTypeId = hourly.Id;
        context.TimeRegister.PlannedWorkingMins = 540;
        context.EmployeeContext.Absences.Add(NewAbsence(context.Employee.Id, hourly, Day, Day,
            absenceHours: 2, totalLeaveDays: 0.25,
            startTime: new TimeSpan(8, 0, 0), endTime: new TimeSpan(10, 0, 0)));

        (await CreateCalculator().GetLeaveMinutesAsync(context)).ShouldBe(120);
    }

    [Fact]
    public async Task GetIncludeInLeaveDay_Should_Be_1_For_A_Paid_Leave_On_An_Excluded_WeekOff_With_ActiveDays_Prorate()
    {
        var context = NewContext();
        var paidLeave = NewAbsenceType(1, "1", isPaid: true, includeWeekend: false);
        paidLeave.WageTypeId = 900;
        context.MasterData.AbsenceTypes.Add(paidLeave);
        context.MasterData.WageTypes.Add(WithId(new WageType
        {
            TenantId = 1,
            Code = "1000",
            NameEn = "Basic",
            NameAr = "Basic",
            ProrateMethod = ProrateMethod.ActiveDays
        }, 900));

        context.TimeRegister.LeaveTypeId = paidLeave.Id;
        context.TimeRegister.LeaveDay = 0;
        context.TimeRegister.WeeklyOff = 1;

        (await CreateCalculator().GetIncludeInLeaveDayAsync(context)).ShouldBe(1);
    }

    [Fact]
    public async Task GetIncludeInLeaveDay_Should_Be_0_When_The_WageType_Is_Not_ActiveDays_Prorated()
    {
        var context = NewContext();
        var paidLeave = NewAbsenceType(1, "1", isPaid: true, includeWeekend: false);
        paidLeave.WageTypeId = 900;
        context.MasterData.AbsenceTypes.Add(paidLeave);
        context.MasterData.WageTypes.Add(WithId(new WageType
        {
            TenantId = 1,
            Code = "1000",
            NameEn = "Basic",
            NameAr = "Basic",
            ProrateMethod = ProrateMethod.WorkingDays
        }, 900));

        context.TimeRegister.LeaveTypeId = paidLeave.Id;
        context.TimeRegister.WeeklyOff = 1;

        (await CreateCalculator().GetIncludeInLeaveDayAsync(context)).ShouldBe(0);
    }

    #endregion

    #region << Attendance Calculation >>

    [Fact]
    public async Task GetAttendanceDay_Should_Be_1_On_A_RestDay_With_An_Attendance_Row()
    {
        var context = NewContext();
        context.TimeRegister.RestDay = 1;
        AddAttendance(context, includePublicHoliday: false);

        (await CreateCalculator().GetAttendanceDayAsync(context)).ShouldBe(1);
    }

    [Fact]
    public async Task GetAttendanceDay_Should_Be_0_On_A_Public_Holiday_When_The_Type_Excludes_Holidays()
    {
        var context = NewContext();
        context.TimeRegister.PublicHoliday = 1;
        AddAttendance(context, includePublicHoliday: false);

        (await CreateCalculator().GetAttendanceDayAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetAttendanceDay_Should_Clear_PublicHoliday_When_The_Type_Includes_Holidays()
    {
        var context = NewContext();
        context.TimeRegister.PublicHoliday = 1;
        AddAttendance(context, includePublicHoliday: true);

        (await CreateCalculator().GetAttendanceDayAsync(context)).ShouldBe(1);
        context.TimeRegister.PublicHoliday.ShouldBe(0);
    }

    [Fact]
    public async Task GetAttendanceTypeId_Should_Be_Null_When_AttendanceDay_Is_0()
    {
        var context = NewContext();
        AddAttendance(context, includePublicHoliday: true);
        context.TimeRegister.AttendanceDay = 0;

        (await CreateCalculator().GetAttendanceTypeIdAsync(context)).ShouldBeNull();
    }

    [Fact]
    public async Task GetAttendanceMinutes_Should_Be_The_Attendance_Window_For_A_Partial_Day_Type()
    {
        var context = NewContext();
        var attendanceType = AddAttendance(context, includePublicHoliday: true, isFullDay: false);
        context.TimeRegister.AttendanceTypeId = attendanceType.Id;
        context.TimeRegister.PublicHoliday = 1;

        (await CreateCalculator().GetAttendanceMinutesAsync(context)).ShouldBe(180);
    }

    [Fact]
    public async Task GetAttendanceMinutes_Should_Be_PlannedWorkingMins_For_A_FullDay_Type()
    {
        var context = NewContext();
        var attendanceType = AddAttendance(context, includePublicHoliday: true, isFullDay: true);
        context.TimeRegister.AttendanceTypeId = attendanceType.Id;
        context.TimeRegister.PlannedWorkingMins = 540;

        (await CreateCalculator().GetAttendanceMinutesAsync(context)).ShouldBe(540);
    }

    private AttendanceType AddAttendance(TimeRegisterDayContext context, bool includePublicHoliday,
        bool isFullDay = false)
    {
        var attendanceType = WithId(new AttendanceType
        {
            TenantId = 1,
            Code = "1",
            NameEn = "Extra Working",
            NameAr = "Extra Working",
            IsPaid = true,
            IsFullDay = isFullDay,
            IncludePublicHoliday = includePublicHoliday,
            IncludeWeekend = true,
            IncludeRestDay = true
        }, 1000);

        _attendanceTypes.Add(attendanceType);

        context.EmployeeContext.Attendances.Add(new EmployeeAttendance
        {
            TenantId = 1,
            EmployeeId = context.Employee.Id,
            AttendanceTypeId = attendanceType.Id,
            AttendanceType = attendanceType,
            StartDate = Day,
            EndDate = Day,
            StartTime = Day.Date.AddHours(9),
            EndTime = Day.Date.AddHours(12)
        });

        return attendanceType;
    }

    [Fact]
    public async Task CalculateAutoAttendance_Should_Create_An_Attendance_Row_On_A_Public_Holiday_With_Punches()
    {
        var context = NewContext();
        context.TimeRegister.PublicHoliday = 1;
        context.TimeRegister.ActualIn = Day.Date.AddHours(9);
        context.TimeRegister.ActualOut = Day.Date.AddHours(15);

        var extraWorking = WithId(new AttendanceType
        {
            TenantId = 1,
            Code = "1",
            NameEn = "Extra Working",
            NameAr = "Extra Working"
        }, 1000);
        _attendanceTypes.Add(extraWorking);
        context.MasterData.ExtraWorkingAttendanceType = extraWorking;

        await CreateCalculator().CalculateAutoAttendanceAsync(context);

        _employeeAttendances.Count.ShouldBe(1);
        _employeeAttendances[0].AttendanceTypeId.ShouldBe(extraWorking.Id);
        _employeeAttendances[0].StartDate.ShouldBe(Day.Date);
        _employeeAttendances[0].StartTime.TimeOfDay.ShouldBe(TimeSpan.FromHours(9));
        _employeeAttendances[0].EndTime.TimeOfDay.ShouldBe(TimeSpan.FromHours(15));
        context.EmployeeContext.Attendances.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CalculateAutoAttendance_Should_Do_Nothing_When_The_Day_Is_Not_A_Public_Holiday()
    {
        var context = NewContext();
        context.TimeRegister.PublicHoliday = 0;
        context.TimeRegister.ActualIn = Day.Date.AddHours(9);
        context.TimeRegister.ActualOut = Day.Date.AddHours(15);

        await CreateCalculator().CalculateAutoAttendanceAsync(context);

        _employeeAttendances.ShouldBeEmpty();
    }

    [Fact]
    public async Task CalculateAutoAttendance_Should_Do_Nothing_Without_Both_Punches()
    {
        var context = NewContext();
        context.TimeRegister.PublicHoliday = 1;
        context.TimeRegister.ActualIn = Day.Date.AddHours(9);
        context.TimeRegister.ActualOut = default;

        await CreateCalculator().CalculateAutoAttendanceAsync(context);

        _employeeAttendances.ShouldBeEmpty();
    }

    #endregion

    #region << LateIn / EarlyOut Calculation >>

    [Fact]
    public async Task GetLateIn_Should_Be_The_Minutes_After_ShiftStart()
    {
        var context = NewContext();
        context.TimeRegister.ActualIn = Day.Date.AddHours(8).AddMinutes(15);
        context.TimeRegister.ActualStartTime1 = Day.Date.AddHours(8).AddMinutes(15);

        (await CreateCalculator().GetLateInAsync(context)).ShouldBe(15);
    }

    [Fact]
    public async Task GetLateIn_Should_Be_0_When_There_Is_No_Punch_In_And_The_Policy_Requires_One()
    {
        var context = NewContext();
        context.TimeRegister.ActualIn = default;
        context.TimeRegister.ActualStartTime1 = Day.Date.AddHours(8).AddMinutes(15);

        (await CreateCalculator().GetLateInAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetDeductionLateIn_Should_Be_0_Within_Tolerance()
    {
        var context = NewContext();
        context.TimePolicy.LateInTolerance = 20;
        context.TimeRegister.ActualStartTime1 = Day.Date.AddHours(8).AddMinutes(15);
        context.TimeRegister.LateIn = 15;

        (await CreateCalculator().GetDeductionLateInAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetDeductionLateIn_Should_Subtract_Tolerance_When_CalcAfterTolerance_Is_On()
    {
        var context = NewContext();
        context.TimePolicy.LateInTolerance = 10;
        context.TimePolicy.CalcLateInAfterTolerance = true;
        context.TimeRegister.ActualStartTime1 = Day.Date.AddHours(8).AddMinutes(25);
        context.TimeRegister.LateIn = 25;

        (await CreateCalculator().GetDeductionLateInAsync(context)).ShouldBe(15);
    }

    [Fact]
    public async Task GetDeductionLateIn_Should_Deduct_The_Full_LateIn_When_CalcAfterTolerance_Is_Off()
    {
        var context = NewContext();
        context.TimePolicy.LateInTolerance = 10;
        context.TimePolicy.CalcLateInAfterTolerance = false;
        context.TimeRegister.ActualStartTime1 = Day.Date.AddHours(8).AddMinutes(25);
        context.TimeRegister.LateIn = 25;

        (await CreateCalculator().GetDeductionLateInAsync(context)).ShouldBe(25);
    }

    [Fact]
    public async Task GetDeductionLateIn_Should_Be_0_On_A_WeeklyOff_Duty()
    {
        var context = NewContext();
        context.Duty.IsWeeklyOff = true;
        context.TimePolicy.LateInTolerance = 0;
        context.TimeRegister.ActualStartTime1 = Day.Date.AddHours(9);
        context.TimeRegister.LateIn = 60;

        (await CreateCalculator().GetDeductionLateInAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetEarlyOut_Should_Be_The_Minutes_Before_ShiftEnd()
    {
        var context = NewContext();
        context.TimeRegister.ActualOut = Day.Date.AddHours(16);
        context.TimeRegister.ActualEndTime1 = Day.Date.AddHours(16);

        (await CreateCalculator().GetEarlyOutAsync(context)).ShouldBe(60);
    }

    [Fact]
    public async Task GetDeductionEarlyOut_Should_Subtract_Tolerance_When_CalcAfterTolerance_Is_On()
    {
        var context = NewContext();
        context.TimePolicy.EarlyOutTolerance = 10;
        context.TimePolicy.CalcEarlyOutAfterTolerance = true;
        context.TimeRegister.ActualEndTime1 = Day.Date.AddHours(16).AddMinutes(30);
        context.TimeRegister.EarlyOut = 30;

        (await CreateCalculator().GetDeductionEarlyOutAsync(context)).ShouldBe(20);
    }

    [Fact]
    public async Task GetBreakEarlyOut_And_GetBreakLateIn_Should_Measure_Break_Overruns()
    {
        var context = NewContext();
        context.TimeRegister.BreakStartTime = Day.Date.AddHours(12);
        context.TimeRegister.BreakEndTime = Day.Date.AddHours(13);
        context.TimeRegister.ActualBreakStartTime = Day.Date.AddHours(11).AddMinutes(45);
        context.TimeRegister.ActualBreakEndTime = Day.Date.AddHours(13).AddMinutes(20);

        var calculator = CreateCalculator();
        (await calculator.GetBreakEarlyOutAsync(context)).ShouldBe(15);
        (await calculator.GetBreakLateInAsync(context)).ShouldBe(20);
    }

    [Fact]
    public async Task GetBreakDeductions_Should_Honour_Their_Tolerances()
    {
        var context = NewContext();
        context.TimePolicy.BreakEarlyOutTolerance = 5;
        context.TimePolicy.CalcBreakEarlyOutAfterTolerance = true;
        context.TimePolicy.BreakLateInTolerance = 30;
        context.TimeRegister.BreakStartTime = Day.Date.AddHours(12);
        context.TimeRegister.BreakEndTime = Day.Date.AddHours(13);
        context.TimeRegister.ActualBreakStartTime = Day.Date.AddHours(11).AddMinutes(45);
        context.TimeRegister.ActualBreakEndTime = Day.Date.AddHours(13).AddMinutes(20);

        var calculator = CreateCalculator();
        (await calculator.GetBreakDeductionEarlyOutAsync(context)).ShouldBe(10);
        (await calculator.GetBreakDeductionLateInAsync(context)).ShouldBe(0); // within tolerance
    }

    [Fact]
    public async Task GetDedLateInEarlyOutMinutes_And_Day_Should_Aggregate_And_Prorate()
    {
        var context = NewContext();
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.DeductionLateIn = 15;
        context.TimeRegister.DeductionEarlyOut = 20;
        context.TimeRegister.BreakDeductionEarlyOut = 5;
        context.TimeRegister.BreakDeductionLateIn = 8;

        var calculator = CreateCalculator();
        var minutes = await calculator.GetDedLateInEarlyOutMinutesAsync(context);
        minutes.ShouldBe(48);

        context.TimeRegister.DedLateInEarlyOutMins = minutes;
        (await calculator.GetDedLateInEarlyOutDayAsync(context)).ShouldBe(Math.Round(48d / 480, 3));
    }

    [Fact]
    public async Task GetDedLateInEarlyOutMinutes_Should_Be_0_When_There_Are_No_Planned_Minutes()
    {
        var context = NewContext();
        context.TimeRegister.PlannedWorkingMins = 0;
        context.TimeRegister.DeductionLateIn = 15;

        var calculator = CreateCalculator();
        (await calculator.GetDedLateInEarlyOutMinutesAsync(context)).ShouldBe(0);
        (await calculator.GetDedLateInEarlyOutDayAsync(context)).ShouldBe(0);
    }

    #endregion

    #region << Actual Calculation >>

    [Fact]
    public async Task GetTotalWorkingMinutes_Should_Be_The_Punch_Span_Plus_Attendance()
    {
        var context = NewContext();
        context.TimeRegister.ActualIn = Day.Date.AddHours(8);
        context.TimeRegister.ActualOut = Day.Date.AddHours(17);
        context.TimeRegister.AttendanceMinutes = 30;

        (await CreateCalculator().GetTotalWorkingMinutesAsync(context)).ShouldBe(570);
    }

    [Fact]
    public async Task GetTotalWorkingMinutes_Should_Be_0_When_A_Required_Punch_Is_Missing()
    {
        var context = NewContext();
        context.TimeRegister.ActualIn = Day.Date.AddHours(8);
        context.TimeRegister.ActualOut = default;

        (await CreateCalculator().GetTotalWorkingMinutesAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetActualAbsenceMinutes_Should_Be_PlannedWorkingMins_For_A_FullDay_Unpaid_Leave()
    {
        var context = NewContext();
        var unpaid = NewAbsenceType(2, "2", isPaid: false);
        context.MasterData.AbsenceTypes.Add(unpaid);
        context.TimeRegister.LeaveTypeId = unpaid.Id;
        context.TimeRegister.LeaveDay = 1;
        context.TimeRegister.PlannedWorkingMins = 480;

        (await CreateCalculator().GetActualAbsenceMinutesAsync(context)).ShouldBe(480);
    }

    [Fact]
    public async Task GetActualAbsenceMinutes_Should_Be_PlannedWorkingMins_When_Punches_Are_Missing()
    {
        var context = NewContext();
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.ActualIn = default;
        context.TimeRegister.ActualOut = default;

        (await CreateCalculator().GetActualAbsenceMinutesAsync(context)).ShouldBe(480);
    }

    [Fact]
    public async Task GetActualAbsenceMinutes_Should_Add_Deductions_When_MonthlyTolerance_Is_NotSelected()
    {
        var context = NewContext();
        context.TimePolicy.MonthlyToleranceType = MonthlyToleranceType.NotSelected;
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.ActualIn = Day.Date.AddHours(8).AddMinutes(30);
        context.TimeRegister.ActualOut = Day.Date.AddHours(17);
        context.TimeRegister.DeductionLateIn = 30;

        (await CreateCalculator().GetActualAbsenceMinutesAsync(context)).ShouldBe(30);
    }

    [Fact]
    public async Task GetActualAbsenceMinutes_Should_Never_Exceed_PlannedWorkingMins()
    {
        var context = NewContext();
        context.TimePolicy.MonthlyToleranceType = MonthlyToleranceType.NotSelected;
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.ActualIn = default;
        context.TimeRegister.ActualOut = default;
        context.TimeRegister.DeductionLateIn = 100;

        (await CreateCalculator().GetActualAbsenceMinutesAsync(context)).ShouldBe(480);
    }

    [Fact]
    public async Task GetActualWorkingMinutes_Should_Be_Planned_Minus_Absence_Plus_Attendance()
    {
        var context = NewContext();
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.ActualAbsenceMins = 60;
        context.TimeRegister.AttendanceMinutes = 30;

        (await CreateCalculator().GetActualWorkingMinutesAsync(context)).ShouldBe(450);
    }

    [Fact]
    public async Task GetActualWorkingMinutes_Should_Subtract_A_Fractional_AbsAdjustment()
    {
        var context = NewContext();
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.ActualAbsenceMins = 0;
        context.EmployeeContext.AbsAdjustments.Add(new EmployeeAbsAdjustment
        {
            TenantId = 1,
            EmployeeId = context.Employee.Id,
            AbsenceTypeId = 1,
            AdjustmentType = AdjustmentType.Fractional,
            EffectiveDate = Day.Date,
            QuotaAdjusted = 0.5
        });

        (await CreateCalculator().GetActualWorkingMinutesAsync(context)).ShouldBe(240);
    }

    [Fact]
    public async Task GetActualWorkingMinutes_Should_Ignore_NoImpactOnQuota_Adjustments()
    {
        var context = NewContext();
        context.TimeRegister.PlannedWorkingMins = 480;
        context.EmployeeContext.AbsAdjustments.Add(new EmployeeAbsAdjustment
        {
            TenantId = 1,
            EmployeeId = context.Employee.Id,
            AbsenceTypeId = 1,
            AdjustmentType = AdjustmentType.NoImpactOnQuota,
            EffectiveDate = Day.Date,
            QuotaAdjusted = 0.5
        });

        (await CreateCalculator().GetActualWorkingMinutesAsync(context)).ShouldBe(480);
    }

    [Fact]
    public async Task GetActualWorkingMinutes_Should_Use_The_Timesheet_For_A_TimeSheet_Policy()
    {
        var context = NewContext();
        context.TimePolicy.TimeMgmtStatus = TimeMgmtStatus.TimeSheet;
        context.TimeRegister.PlannedWorkingMins = 480;
        _employeeTimesheets.Add(WithId(new EmployeeTimesheet
        {
            TenantId = 1,
            EmployeeId = context.Employee.Id,
            TransDate = Day,
            TotalHours = 6
        }, 1));

        (await CreateCalculator().GetActualWorkingMinutesAsync(context)).ShouldBe(360);
    }

    [Fact]
    public async Task GetActualWorkingDay_Should_Be_One_Minus_LeaveDay_When_On_Leave()
    {
        var context = NewContext();
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.LeaveDay = 0.5;

        (await CreateCalculator().GetActualWorkingDayAsync(context)).ShouldBe(0.5);
    }

    [Fact]
    public async Task GetActualWorkingDay_Should_Prorate_ActualWorkingMins_Otherwise()
    {
        var context = NewContext();
        context.TimeRegister.PlannedWorkingMins = 480;
        context.TimeRegister.ActualWorkingMins = 240;

        (await CreateCalculator().GetActualWorkingDayAsync(context)).ShouldBe(0.5);
    }

    [Fact]
    public async Task GetAbsentDay_Should_Be_1_When_Planned_But_Nothing_Worked_And_No_Leave()
    {
        var context = NewContext();
        context.TimeRegister.PlannedWorkingDay = 1;
        context.TimeRegister.ActualWorkingMins = 0;
        context.TimeRegister.LeaveDay = 0;

        (await CreateCalculator().GetAbsentDayAsync(context)).ShouldBe(1);
    }

    [Fact]
    public async Task GetAbsentDay_Should_Be_0_When_On_Leave()
    {
        var context = NewContext();
        context.TimeRegister.PlannedWorkingDay = 1;
        context.TimeRegister.ActualWorkingMins = 0;
        context.TimeRegister.LeaveDay = 1;

        (await CreateCalculator().GetAbsentDayAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetActualAttendanceMinutes_Should_Be_PlannedWorkingMins_For_A_Paid_FullDay_Attendance()
    {
        var context = NewContext();
        var attendanceType = AddAttendance(context, includePublicHoliday: true, isFullDay: true);
        context.TimeRegister.AttendanceTypeId = attendanceType.Id;
        context.TimeRegister.AttendanceDay = 1;
        context.TimeRegister.PlannedWorkingMins = 480;

        (await CreateCalculator().GetActualAttendanceMinutesAsync(context)).ShouldBe(480);
    }

    #endregion

    #region << Overtime Calculation >>

    [Fact]
    public async Task GetOvertimeMinutes_Should_Be_The_Surplus_Over_Working_Plus_Absence()
    {
        var context = NewContext();
        context.TimeRegister.TotalWorkingMins = 600;
        context.TimeRegister.ActualWorkingMins = 480;
        context.TimeRegister.ActualAbsenceMins = 0;
        context.TimePolicy.OTMinLimit = 30;

        (await CreateCalculator().GetOvertimeMinutesAsync(context)).ShouldBe(120);
    }

    [Fact]
    public async Task GetOvertimeMinutes_Should_Be_0_Below_The_Policy_Minimum()
    {
        var context = NewContext();
        context.TimeRegister.TotalWorkingMins = 500;
        context.TimeRegister.ActualWorkingMins = 480;
        context.TimePolicy.OTMinLimit = 30;

        (await CreateCalculator().GetOvertimeMinutesAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetOvertimeTypeId_Should_Come_From_The_Duty_On_A_Normal_Day()
    {
        var context = NewContext();
        context.Duty.OvertimeTypeId = 55;

        (await CreateCalculator().GetOvertimeTypeIdAsync(context)).ShouldBe(55);
    }

    [Fact]
    public async Task GetOvertimeTypeId_Should_Come_From_The_Holiday_On_A_Public_Holiday()
    {
        var context = ContextWithHolidayCalendar(out _, isHolidayToday: true);
        context.TimeRegister.PublicHoliday = 1;

        (await CreateCalculator().GetOvertimeTypeIdAsync(context)).ShouldBe(777);
    }

    [Fact]
    public async Task CalculateOvertime_Should_Create_An_EmployeeOvertime_Row_When_The_SubGroup_Pays_Overtime()
    {
        var context = NewContext();
        context.TimeRegister.OvertimeMinutes = 90;
        context.TimeRegister.OvertimeTypeId = 55;
        context.EmployeeContext.EmployeeSubGroup = WithId(new EmployeeSubGroup
        {
            TenantId = 1,
            EmployeeGroupId = 1,
            NameEn = "Staff",
            NameAr = "Staff",
            PayOvertime = true
        }, 10);

        await CreateCalculator().CalculateOvertimeAsync(context);

        _employeeOvertimes.Count.ShouldBe(1);
        _employeeOvertimes[0].ActualOvertimeHours.ShouldBe(1.5);
        _employeeOvertimes[0].ApprovedOvertimeHours.ShouldBe(1.5);
        _employeeOvertimes[0].OvertimeTypeId.ShouldBe(55);
    }

    [Fact]
    public async Task CalculateOvertime_Should_Do_Nothing_For_A_PlannedTime_Policy()
    {
        var context = NewContext();
        context.TimePolicy.TimeMgmtStatus = TimeMgmtStatus.PlannedTime;
        context.TimeRegister.OvertimeMinutes = 90;
        context.TimeRegister.OvertimeTypeId = 55;
        context.EmployeeContext.EmployeeSubGroup = WithId(new EmployeeSubGroup
        {
            TenantId = 1,
            EmployeeGroupId = 1,
            NameEn = "Staff",
            NameAr = "Staff",
            PayOvertime = true
        }, 10);

        await CreateCalculator().CalculateOvertimeAsync(context);

        _employeeOvertimes.ShouldBeEmpty();
    }

    [Fact]
    public async Task CalculateOvertime_Should_Throw_When_Deleting_A_Row_With_Approved_Hours()
    {
        var context = NewContext();
        context.TimeRegister.OvertimeMinutes = 0;

        var existing = WithId(new EmployeeOvertime
        {
            TenantId = 1,
            EmployeeId = context.Employee.Id,
            EffectiveDate = Day,
            OvertimeTypeId = 55,
            ApprovedOvertimeHours = 2
        }, 1);
        _employeeOvertimes.Add(existing);
        context.EmployeeContext.Overtimes.Add(existing);

        await Assert.ThrowsAsync<Exception>(() => CreateCalculator().CalculateOvertimeAsync(context));
    }

    [Fact]
    public async Task CalculateOvertime_Should_Delete_An_Unapproved_Row_When_There_Is_No_Overtime()
    {
        var context = NewContext();
        context.TimeRegister.OvertimeMinutes = 0;

        var existing = WithId(new EmployeeOvertime
        {
            TenantId = 1,
            EmployeeId = context.Employee.Id,
            EffectiveDate = Day,
            OvertimeTypeId = 55
        }, 1);
        _employeeOvertimes.Add(existing);
        context.EmployeeContext.Overtimes.Add(existing);

        await CreateCalculator().CalculateOvertimeAsync(context);

        _employeeOvertimes.ShouldBeEmpty();
        context.EmployeeContext.Overtimes.ShouldBeEmpty();
    }

    #endregion

    #region << Accrual / AirTicket >>

    [Fact]
    public async Task GetAnnualLeaveAccrualPerDay_Should_Divide_The_SubGroup_Entitlement_Over_The_Calendar_Year()
    {
        var context = NewContext();
        context.MasterData.AnnualLeave = NewAbsenceType(1, "1");
        context.MasterData.AnnualLeave.AccrualPeriod = AccrualPeriod.CalendarYear;
        context.AnnualLeaveSubGroupAbsence = new EmployeeSubGroupAbsence
        {
            TenantId = 1,
            EmployeeSubGroupId = 10,
            AbsenceTypeId = 1,
            TotalDays = 30
        };

        // 2024 is a leap year -> 366.
        (await CreateCalculator().GetAnnualLeaveAccrualPerDayAsync(context))
            .ShouldBe(Math.Round(30d / 366, 6));
    }

    [Fact]
    public async Task GetAnnualLeaveAccrualPerDay_Should_Add_TimeTransfers_For_The_Day()
    {
        var context = NewContext();
        context.MasterData.AnnualLeave = NewAbsenceType(1, "1");
        context.MasterData.AnnualLeave.AccrualPeriod = AccrualPeriod.CalendarYear;
        context.AnnualLeaveSubGroupAbsence = new EmployeeSubGroupAbsence
        {
            TenantId = 1,
            EmployeeSubGroupId = 10,
            AbsenceTypeId = 1,
            TotalDays = 30
        };
        context.EmployeeContext.TimeTransfers.Add(new TimeTransfer
        {
            TenantId = 1,
            EmployeeId = context.Employee.Id,
            TransferDate = Day,
            TransferTypeId = 1,
            TransferCount = 0.5
        });

        (await CreateCalculator().GetAnnualLeaveAccrualPerDayAsync(context))
            .ShouldBe(Math.Round(30d / 366 + 0.5, 6));
    }

    [Fact]
    public async Task GetAnnualLeaveAccrualPerDay_Should_Only_Count_Transfers_On_An_Absent_Day_When_Excluded()
    {
        var context = NewContext();
        context.MasterData.AnnualLeave = NewAbsenceType(1, "1");
        context.MasterData.AnnualLeave.AccrualPeriod = AccrualPeriod.CalendarYear;
        context.Settings.ExcludePaidLeaveAccrualOnAbsent = true;
        context.TimeRegister.AbsentDay = 1;
        context.AnnualLeaveSubGroupAbsence = new EmployeeSubGroupAbsence
        {
            TenantId = 1,
            EmployeeSubGroupId = 10,
            AbsenceTypeId = 1,
            TotalDays = 30
        };

        (await CreateCalculator().GetAnnualLeaveAccrualPerDayAsync(context)).ShouldBe(0);
    }

    [Fact]
    public async Task GetTicketAccrualPerDay_Should_Divide_The_TicketCount_Over_365()
    {
        var context = NewContext();
        context.MasterData.AirTicketTypeId = 20;
        context.MasterData.TicketTypes.Add(WithId(new TicketType
        {
            TenantId = 1,
            Code = "1",
            NameEn = "Air Ticket",
            NameAr = "Air Ticket",
            QuotaEntitlement = QuotaEntitlement.Period
        }, 20));
        context.EmployeeContext.EmployeeSubGroup = WithId(new EmployeeSubGroup
        {
            TenantId = 1,
            EmployeeGroupId = 1,
            NameEn = "Staff",
            NameAr = "Staff"
        }, 10);
        _employeeSubGroupTickets.Add(WithId(new EmployeeSubGroupTicket
        {
            TenantId = 1,
            EmployeeSubGroupId = 10,
            TicketTypeId = 20,
            StartDate = Day.AddDays(-10),
            EndDate = Day.AddDays(10),
            TicketCount = 1,
            AccrualPeriod = AccrualPeriod.CalendarYear
        }, 1));

        (await CreateCalculator().GetTicketAccrualPerDayAsync(context)).ShouldBe(Math.Round(1d / 365, 3));
    }

    [Fact]
    public async Task GetAirTicketPaidDay_Should_Sum_Compensation_Across_The_Four_Legacy_Ticket_Codes()
    {
        var context = NewContext();
        context.MasterData.AirTicketTypeId = 20;
        context.MasterData.TicketTypes.Add(WithId(new TicketType
        {
            TenantId = 1, Code = "1", NameEn = "Self", NameAr = "Self"
        }, 20));
        context.MasterData.TicketTypes.Add(WithId(new TicketType
        {
            TenantId = 1, Code = "2", NameEn = "Spouse", NameAr = "Spouse"
        }, 21));

        context.EmployeeContext.Tickets.Add(new EmployeeTicket
        {
            TenantId = 1, EmployeeId = context.Employee.Id, TicketTypeId = 20, PaymentDate = Day, Compensate = 1
        });
        context.EmployeeContext.Tickets.Add(new EmployeeTicket
        {
            TenantId = 1, EmployeeId = context.Employee.Id, TicketTypeId = 21, PaymentDate = Day, Compensate = 0.5
        });

        (await CreateCalculator().GetAirTicketPaidDayAsync(context)).ShouldBe(1.5);
    }

    #endregion
}
