using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Legacy <c>GeneralTimeService</c> regions "Actual Calculation" (lines 909-1101) and "AbsentDay Calculation"
/// (lines 1103-1113).
/// </summary>
public partial class TimeRegisterFieldCalculator
{
    #region << Actual Calculation >>

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetActualAbsenceMinutes</c> (legacy lines 911-1004). The legacy
    /// <c>IsNotReturnFromLeave</c> parameter is <see cref="TimeRegisterDayContext.IsNotReturnFromLeave"/>.
    /// NOT called when the day's policy is FlexiTime.
    ///
    /// <para>Note the hourly branch here subtracts full <c>DateTime</c>s (<c>rec.EndTime - rec.StartTime</c>),
    /// unlike <c>GetLeaveMinutes</c> which subtracts <c>TimeOfDay</c>s. Legacy asymmetry, preserved.</para>
    /// </summary>
    public Task<double> GetActualAbsenceMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var timePolicy = TimePolicyOf(context);
        var leaveType = LeaveTypeOf(context);

        double absMinutes = 0;

        // Logic for FullDay Leave.
        if (timeRegister.LeaveDay == 1 && leaveType != null && leaveType.IsPaid == false)
        {
            absMinutes = absMinutes + timeRegister.PlannedWorkingMins;
        }

        // Logic for Hourly Bases Leave.
        else if (timeRegister.LeaveDay != 0)
        {
            if (timeRegister.PlannedWorkingMins > 0
                && IsFullDayHours(context) == false
                && (HasPunchIn(context) == false || HasPunchOut(context) == false
                                                 || HasBreakPunchOut(context) == false
                                                 || HasBreakPunchIn(context) == false))
            {
                absMinutes = timeRegister.PlannedWorkingMins;
            }
            else
            {
                var dayDate = timeRegister.DayDate.Date;
                var hourlyAbsences = AbsencesOf(context)
                    .Where(r => r.StartDate.Date <= dayDate
                                && r.EndDate.Date >= dayDate
                                && ResolveAbsenceType(context, r) != null)
                    .OrderBy(r => r.StartTime)
                    .ToList();

                foreach (var rec in hourlyAbsences)
                {
                    var absenceType = ResolveAbsenceType(context, rec);
                    var hourlyLeaveMinutes = rec.EndTime.Subtract(rec.StartTime).TotalMinutes;

                    if (absenceType.IncludePublicHoliday == false && timeRegister.PublicHoliday == 1)
                    {
                        hourlyLeaveMinutes = 0;
                    }

                    if (absenceType.IncludeWeekend == false && timeRegister.WeeklyOff == 1)
                    {
                        hourlyLeaveMinutes = 0;
                    }

                    if (absenceType.IncludeRestDay == false && timeRegister.RestDay == 1)
                    {
                        hourlyLeaveMinutes = 0;
                    }

                    absMinutes = absMinutes + hourlyLeaveMinutes;
                }
            }
        }

        // Logic for AbsentDay.
        else if (timeRegister.PlannedWorkingMins > 0
                 && (HasPunchIn(context) == false || HasPunchOut(context) == false
                                                  || HasBreakPunchOut(context) == false
                                                  || HasBreakPunchIn(context) == false))
        {
            absMinutes = timeRegister.PlannedWorkingMins;
        }

        // Logic for IsNotReturnFromLeave.
        if (timeRegister.PlannedWorkingMins > 0 && context.IsNotReturnFromLeave)
        {
            absMinutes = timeRegister.PlannedWorkingMins;
        }

        // Check Deduction Minutes.
        if (timePolicy != null && timePolicy.MonthlyToleranceType == MonthlyToleranceType.NotSelected)
        {
            absMinutes = absMinutes
                         + timeRegister.DeductionLateIn
                         + timeRegister.DeductionEarlyOut
                         + timeRegister.BreakDeductionEarlyOut
                         + timeRegister.BreakDeductionLateIn;
        }

        if (absMinutes > timeRegister.PlannedWorkingMins)
        {
            absMinutes = timeRegister.PlannedWorkingMins;
        }

        return Task.FromResult(Math.Round(absMinutes, 3));
    }

    /// <summary>Legacy <c>GeneralTimeService.GetActualAttendanceMinutes</c> (legacy lines 1006-1013).
    /// Not called by the orchestrator.</summary>
    public Task<double> GetActualAttendanceMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var attendanceType = AttendanceTypeOf(context);

        var result = (timeRegister.AttendanceDay == 1 && attendanceType != null && attendanceType.IsPaid)
            ? (attendanceType.IsFullDay ? timeRegister.PlannedWorkingMins : timeRegister.LeaveMinutes)
            : 0;

        return Task.FromResult(result);
    }

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetActualWorkingMinutes</c> (legacy lines 1015-1064). The legacy
    /// <c>empAbsAdjustmentList</c> parameter is <see cref="TimeEngineEmployeeContext.AbsAdjustments"/>; the
    /// <c>EmployeeTimesheet</c> lookup becomes an <c>IRepository&lt;EmployeeTimesheet&gt;</c> query.
    /// </summary>
    public Task<double> GetActualWorkingMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var timePolicy = TimePolicyOf(context);

        double actualWorkingMins = 0;

        if (timePolicy == null || timePolicy.TimeMgmtStatus != TimeMgmtStatus.TimeSheet)
        {
            actualWorkingMins = timeRegister.PlannedWorkingMins - timeRegister.ActualAbsenceMins;
            actualWorkingMins = (actualWorkingMins < 0) ? 0 : actualWorkingMins;
        }
        else
        {
            var employeeId = timeRegister.EmployeeId;
            var dayDate = timeRegister.DayDate;

            var totalHours = _employeeTimesheetRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.TransDate == dayDate)
                .Select(r => r.TotalHours)
                .FirstOrDefault();

            if (totalHours != 0)
            {
                var totalMinutes = totalHours * 60;
                if (timeRegister.PlannedWorkingMins == 0)
                {
                    actualWorkingMins = 0;
                }
                else if (timeRegister.PlannedWorkingMins < totalMinutes)
                {
                    actualWorkingMins = timeRegister.PlannedWorkingMins;
                }
                else
                {
                    actualWorkingMins = totalMinutes;
                }
            }
        }

        // 2. check for any attendance day.
        actualWorkingMins = actualWorkingMins
                            + ((timeRegister.AttendanceMinutes > 0) ? timeRegister.AttendanceMinutes : 0);

        // 3. Check if AbsAdjustment entry done for fractional leave or not; if yes then subtract that fraction
        //    from actual working min.
        if (timeRegister.PlannedWorkingMins != 0)
        {
            var effectiveDate = timeRegister.DayDate.Date;
            var adjustments = context.EmployeeContext?.AbsAdjustments
                              ?? new System.Collections.Generic.List<EmployeeAbsAdjustment>();

            var empQuotaAdjusted = adjustments
                .Where(r => r.EffectiveDate == effectiveDate && r.AdjustmentType != AdjustmentType.NoImpactOnQuota)
                .Sum(r => r.QuotaAdjusted);

            if (empQuotaAdjusted != 0)
            {
                actualWorkingMins = actualWorkingMins - (timeRegister.PlannedWorkingMins * empQuotaAdjusted);
            }
        }

        return Task.FromResult(Math.Round(actualWorkingMins < 0 ? 0 : actualWorkingMins, 3));
    }

    /// <summary>Legacy <c>GeneralTimeService.GetTotalWorkingMinutes</c> (legacy lines 1066-1088).
    /// NOT called when the day's policy is FlexiTime.</summary>
    public Task<double> GetTotalWorkingMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var timePolicy = TimePolicyOf(context);
        var duty = DutyOf(context);

        double totalWorkingMins = 0;

        if (timePolicy != null
            && (timePolicy.ShouldSignIn && timeRegister.ActualIn != default)
            && (timePolicy.ShouldSignOut && timeRegister.ActualOut != default))
        {
            totalWorkingMins = (duty == null || duty.BreakShift == null)
                ? timeRegister.ActualOut.Subtract(timeRegister.ActualIn).TotalMinutes
                : (timeRegister.BreakOut.Subtract(timeRegister.ActualIn).TotalMinutes
                   + timeRegister.ActualOut.Subtract(timeRegister.BreakIn).TotalMinutes);

            totalWorkingMins = totalWorkingMins
                               + ((timeRegister.AttendanceMinutes > 0) ? timeRegister.AttendanceMinutes : 0);
        }

        return Task.FromResult(Math.Round(totalWorkingMins < 0 ? 0 : totalWorkingMins, 3));
    }

    /// <summary>Legacy <c>GeneralTimeService.GetActualWorkingDay</c> (legacy lines 1090-1099).</summary>
    public Task<double> GetActualWorkingDayAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        var actualWorkingDay = (timeRegister.PlannedWorkingMins != 0 && timeRegister.LeaveDay != 0)
            ? Math.Round(1 - timeRegister.LeaveDay, 3)
            : (timeRegister.PlannedWorkingMins != 0 && timeRegister.ActualWorkingMins != 0)
                ? Math.Round(timeRegister.ActualWorkingMins / timeRegister.PlannedWorkingMins, 3)
                : 0;

        return Task.FromResult(Math.Round(actualWorkingDay < 0 ? 0 : actualWorkingDay, 3));
    }

    #endregion

    #region << AbsentDay Calculation >>

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetAbsentDay</c> (legacy lines 1105-1111). Reads
    /// <see cref="TimeRegister.ActualWorkingMins"/>, which the orchestrator has already assigned -- see the
    /// ordering contract on <see cref="ITimeRegisterFieldCalculator"/>.
    /// </summary>
    public Task<double> GetAbsentDayAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        var result = (timeRegister.PlannedWorkingDay > 0
                      && timeRegister.ActualWorkingMins == 0
                      && timeRegister.LeaveDay == 0)
            ? 1d
            : 0d;

        return Task.FromResult(result);
    }

    #endregion
}
