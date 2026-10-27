using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeAbsenceCalculationService_Tests : AppTestBase
{
    private readonly IEmployeeAbsenceCalculationService _service;

    public EmployeeAbsenceCalculationService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<IEmployeeAbsenceCalculationService>();
    }

    private int CreateEmployee(DateTime joiningDate)
    {
        return UsingDbContext(context =>
        {
            var nationality = new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "Local",
                NameAr = "محلي"
            };
            context.Set<Nationality>().Add(nationality);
            context.SaveChanges();

            var employee = new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "EMP1",
                FirstNameEn = "Test",
                LastNameEn = "Employee",
                FirstNameAr = "تجربة",
                LastNameAr = "موظف",
                FullNameEn = "Test Employee",
                FullNameAr = "تجربة موظف",
                NationalityId = nationality.Id,
                JoiningDate = joiningDate
            };
            context.Set<Employee>().Add(employee);
            context.SaveChanges();
            return employee.Id;
        });
    }

    private int CreateAbsenceType(string code, QuotaType quotaType = QuotaType.None, bool includeWeekend = false, bool includePublicHoliday = false, bool includeRestDay = false)
    {
        return UsingDbContext(context =>
        {
            var absenceType = new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = "Leave " + code,
                NameAr = "إجازة " + code,
                QuotaType = quotaType,
                IncludeWeekend = includeWeekend,
                IncludePublicHoliday = includePublicHoliday,
                IncludeRestDay = includeRestDay
            };
            context.Set<AbsenceType>().Add(absenceType);
            context.SaveChanges();
            return absenceType.Id;
        });
    }

    private (int workingDutyId, int weeklyOffDutyId, int dutyPeriodId) SetupStandardWeek(int employeeId, DateTime? start = null, DateTime? end = null, int? breakShiftId = null)
    {
        return UsingDbContext(context =>
        {
            var working = new Duty
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "WORK",
                NameEn = "Work",
                NameAr = "عمل",
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                BreakShiftId = breakShiftId
            };
            var weeklyOff = new Duty
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "WKOFF",
                NameEn = "WeeklyOff",
                NameAr = "عطلة",
                IsWeeklyOff = true
            };
            context.Set<Duty>().Add(working);
            context.Set<Duty>().Add(weeklyOff);
            context.SaveChanges();

            var dutyPeriod = new DutyPeriod
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "DP",
                NameAr = "DP",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                SundayDutyId = working.Id,
                MondayDutyId = working.Id,
                TuesdayDutyId = working.Id,
                WednesdayDutyId = working.Id,
                ThursdayDutyId = working.Id,
                FridayDutyId = weeklyOff.Id,
                SaturdayDutyId = working.Id
            };
            context.Set<DutyPeriod>().Add(dutyPeriod);
            context.SaveChanges();

            context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                DutyPeriodId = dutyPeriod.Id,
                StartDate = start ?? new DateTime(2020, 1, 1),
                EndDate = end ?? new DateTime(2030, 12, 31)
            });
            context.SaveChanges();

            return (working.Id, weeklyOff.Id, dutyPeriod.Id);
        });
    }

    #region GetRecoveryPeriods (pure computation)

    [Fact]
    public void GetRecoveryPeriods_Should_Return_Distinct_First_Of_Month_Dates_Spanning_Range()
    {
        var payDate = new DateTime(2024, 1, 15);
        var offCycleEndDate = new DateTime(2024, 3, 10);

        var result = _service.GetRecoveryPeriods(payDate, offCycleEndDate);

        result.ShouldBe(new[]
        {
            new DateTime(2024, 1, 1),
            new DateTime(2024, 2, 1),
            new DateTime(2024, 3, 1)
        });
    }

    #endregion

    #region ActualLeaveDaysAsync

    [Fact]
    public async Task ActualLeaveDaysAsync_Should_Exclude_Weekend_When_AbsenceType_Does_Not_Include_Weekend()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        SetupStandardWeek(employeeId);
        var absenceTypeId = CreateAbsenceType("2", includeWeekend: false);

        // Sunday 2026-02-01 through Saturday 2026-02-07: exactly one Friday (weekly-off).
        var excludedDays = await _service.ActualLeaveDaysAsync(employeeId, new DateTime(2026, 2, 1), new DateTime(2026, 2, 7), absenceTypeId);

        excludedDays.ShouldBe(1m);
    }

    [Fact]
    public async Task ActualLeaveDaysAsync_Should_Not_Exclude_Weekend_When_AbsenceType_Includes_Weekend()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        SetupStandardWeek(employeeId);
        var absenceTypeId = CreateAbsenceType("2", includeWeekend: true);

        var excludedDays = await _service.ActualLeaveDaysAsync(employeeId, new DateTime(2026, 2, 1), new DateTime(2026, 2, 7), absenceTypeId);

        excludedDays.ShouldBe(0m);
    }

    [Fact]
    public async Task ActualLeaveDaysAsync_Should_Apply_FullMonth_26Day_Adjustment_For_Annual_Leave_Full_Calendar_Month()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        SetupStandardWeek(employeeId);
        var absenceTypeId = CreateAbsenceType("1", QuotaType.AnnualLeave, includeWeekend: false);

        UsingDbContext(context =>
        {
            context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                FullMonthLeaveAdjForFixedDays = true
            });
        });

        // Full calendar month (Feb 2026, 28 days) of Annual Leave: 28 - 26 + publicHoliday/restDay exclusions (0) = 2.
        var excludedDays = await _service.ActualLeaveDaysAsync(employeeId, new DateTime(2026, 2, 1), new DateTime(2026, 2, 28), absenceTypeId);

        excludedDays.ShouldBe(2m);
    }

    #endregion

    #region GetEmployeeDutyAsync / GetEmployeeDutyPeriodAsync

    [Fact]
    public async Task GetEmployeeDutyAsync_Should_Resolve_DayOfWeek_Duty_From_DutyPeriod()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var (workingDutyId, weeklyOffDutyId, _) = SetupStandardWeek(employeeId);

        var mondayDuty = await _service.GetEmployeeDutyAsync(employeeId, new DateTime(2026, 2, 2)); // Monday
        var fridayDuty = await _service.GetEmployeeDutyAsync(employeeId, new DateTime(2026, 2, 6)); // Friday

        mondayDuty.Id.ShouldBe(workingDutyId);
        fridayDuty.Id.ShouldBe(weeklyOffDutyId);
    }

    [Fact]
    public async Task GetEmployeeDutyAsync_Should_Be_Overridden_By_EmployeeSubstituteDuty()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var (workingDutyId, _, _) = SetupStandardWeek(employeeId);

        UsingDbContext(context =>
        {
            context.Set<EmployeeSubstituteDuty>().Add(new EmployeeSubstituteDuty
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                DutyId = workingDutyId,
                SubstituteDate = new DateTime(2026, 2, 6) // Friday, normally weekly-off.
            });
        });

        var duty = await _service.GetEmployeeDutyAsync(employeeId, new DateTime(2026, 2, 6));

        duty.Id.ShouldBe(workingDutyId);
    }

    [Fact]
    public async Task GetEmployeeDutyPeriodAsync_Should_Throw_When_Not_Resolvable()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        await Should.ThrowAsync<InvalidOperationException>(() => _service.GetEmployeeDutyPeriodAsync(employeeId, new DateTime(2026, 2, 2)));
    }

    #endregion

    #region GetHoulyLeaveDayAsync

    [Fact]
    public async Task GetHoulyLeaveDayAsync_Should_Compute_Fraction_Of_PlannedMinutes_With_Break()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        var breakShiftId = UsingDbContext(context =>
        {
            var breakShift = new BreakShift
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "Lunch",
                NameAr = "غداء",
                StartTime = new TimeSpan(12, 0, 0),
                EndTime = new TimeSpan(13, 0, 0)
            };
            context.Set<BreakShift>().Add(breakShift);
            context.SaveChanges();
            return breakShift.Id;
        });

        SetupStandardWeek(employeeId, breakShiftId: breakShiftId);

        // Monday 08:00-17:00 minus 12:00-13:00 break = 8 planned hours = 480 minutes.
        // 4 hours of absence -> 240 / 480 = 0.5.
        var leaveDay = await _service.GetHoulyLeaveDayAsync(employeeId, new DateTime(2026, 2, 2), 4);

        leaveDay.ShouldBe(0.5);
    }

    [Fact]
    public async Task GetHoulyLeaveDayAsync_Should_Fallback_To_DefaultWorkingHours_When_Duty_Times_Unset()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));

        UsingDbContext(context =>
        {
            var duty = new Duty
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = "ZERO",
                NameEn = "Zero",
                NameAr = "صفر",
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.Zero
            };
            context.Set<Duty>().Add(duty);
            context.SaveChanges();

            var dutyPeriod = new DutyPeriod
            {
                TenantId = AbpSession.TenantId ?? 1,
                NameEn = "ZDP",
                NameAr = "ZDP",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                SundayDutyId = duty.Id,
                MondayDutyId = duty.Id,
                TuesdayDutyId = duty.Id,
                WednesdayDutyId = duty.Id,
                ThursdayDutyId = duty.Id,
                FridayDutyId = duty.Id,
                SaturdayDutyId = duty.Id
            };
            context.Set<DutyPeriod>().Add(dutyPeriod);
            context.SaveChanges();

            context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                DutyPeriodId = dutyPeriod.Id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31)
            });

            context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                DefaultWorkingHours = 8
            });
        });

        // DefaultWorkingHours = 8 -> 480 planned minutes. 4 hours absence -> 240/480 = 0.5.
        var leaveDay = await _service.GetHoulyLeaveDayAsync(employeeId, new DateTime(2026, 2, 2), 4);

        leaveDay.ShouldBe(0.5);
    }

    #endregion

    #region CalculateTotalLeaveDaysAsync

    [Fact]
    public async Task CalculateTotalLeaveDaysAsync_Should_Compute_FullDay_Leave_Excluding_Weekend()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        SetupStandardWeek(employeeId);
        var absenceTypeId = CreateAbsenceType("2", includeWeekend: false);

        // Sun 2026-02-01 .. Sat 2026-02-07 (7 calendar days), minus 1 Friday weekly-off = 6.
        var totalLeaveDays = await _service.CalculateTotalLeaveDaysAsync(employeeId, absenceTypeId, new DateTime(2026, 2, 1), new DateTime(2026, 2, 7), absenceHours: 0);

        totalLeaveDays.ShouldBe(6d);
    }

    [Fact]
    public async Task CalculateTotalLeaveDaysAsync_Should_Return_Zero_When_Dates_Not_Set()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        var absenceTypeId = CreateAbsenceType("2");

        var totalLeaveDays = await _service.CalculateTotalLeaveDaysAsync(employeeId, absenceTypeId, default, default, absenceHours: 0);

        totalLeaveDays.ShouldBe(0d);
    }

    [Fact]
    public async Task CalculateTotalLeaveDaysAsync_EmployeeAbsence_Overload_Should_Match_Explicit_Overload()
    {
        var employeeId = CreateEmployee(new DateTime(2020, 1, 1));
        SetupStandardWeek(employeeId);
        var absenceTypeId = CreateAbsenceType("2", includeWeekend: false);

        var employeeAbsence = new EmployeeAbsence
        {
            EmployeeId = employeeId,
            AbsenceTypeId = absenceTypeId,
            StartDate = new DateTime(2026, 2, 1),
            EndDate = new DateTime(2026, 2, 7),
            AbsenceHours = 0
        };

        var viaEntity = await _service.CalculateTotalLeaveDaysAsync(employeeAbsence);
        var viaExplicit = await _service.CalculateTotalLeaveDaysAsync(employeeId, absenceTypeId, new DateTime(2026, 2, 1), new DateTime(2026, 2, 7), 0);

        viaEntity.ShouldBe(viaExplicit);
    }

    #endregion
}
