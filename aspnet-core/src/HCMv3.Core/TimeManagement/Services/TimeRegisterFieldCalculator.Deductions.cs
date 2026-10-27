using System;
using System.Threading.Tasks;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Legacy <c>GeneralTimeService</c> region "LateInEarlyOut Calculation" (lines 620-907).
/// </summary>
public partial class TimeRegisterFieldCalculator
{
    #region << LateInEarlyOut Calculation >>

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetLateIn</c> (legacy lines 622-674).
    ///
    /// <para>Note the hourly-leave branch: when <see cref="TimeEngineSettings.IsHourlyLeaveLogicEnabled"/> is
    /// ON and the day IS fully covered by hourly leave, legacy falls through with <c>lateInMinutes</c> still 0
    /// (there is no <c>else</c>) -- i.e. no late-in on a fully-covered day. Preserved.</para>
    /// </summary>
    public Task<double> GetLateInAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        double lateInMinutes = 0;

        if (timeRegister.ActualStartTime1 > timeRegister.ShiftStartTime1 && HasPunchIn(context))
        {
            if (context.Settings != null && context.Settings.IsHourlyLeaveLogicEnabled)
            {
                if (IsFullDayHours(context) == false)
                {
                    var punchInTime = timeRegister.ShiftStartTime1;
                    var absenceAtPunchIn = LeaveAtPunchIn(context, punchInTime);

                    if (absenceAtPunchIn != null)
                    {
                        var datePart = absenceAtPunchIn.StartDate;
                        var timePart = absenceAtPunchIn.EndTime;
                        var leaveEndTimeAtPunchIn = new DateTime(datePart.Year, datePart.Month, datePart.Day,
                            timePart.Hour, timePart.Minute, timePart.Second);

                        lateInMinutes = timeRegister.ActualStartTime1.Subtract(leaveEndTimeAtPunchIn).TotalMinutes;
                    }
                    else
                    {
                        lateInMinutes = timeRegister.ActualStartTime1.Subtract(timeRegister.ShiftStartTime1)
                            .TotalMinutes;
                    }
                }
            }
            else
            {
                lateInMinutes = timeRegister.ActualStartTime1.Subtract(timeRegister.ShiftStartTime1).TotalMinutes;
            }
        }

        return Task.FromResult(Math.Round(lateInMinutes < 0 ? 0 : lateInMinutes, 3));
    }

    /// <summary>Legacy <c>GeneralTimeService.GetDeductionLateIn</c> (legacy lines 676-706).</summary>
    public Task<double> GetDeductionLateInAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var duty = DutyOf(context);
        var timePolicy = TimePolicyOf(context);

        double dedLateInMinutes = 0;

        if (duty != null && !(duty.IsWeeklyOff || duty.IsRestDay)
                         && timeRegister.ActualStartTime1 > timeRegister.ShiftStartTime1)
        {
            dedLateInMinutes = timeRegister.LateIn;

            if (dedLateInMinutes > 0) // if you came 15 mins late
            {
                if (dedLateInMinutes > timePolicy.LateInTolerance) // if tolerance is 10 mins
                {
                    if (timePolicy.CalcLateInAfterTolerance) // if calculate deduction after tolerance
                    {
                        dedLateInMinutes = dedLateInMinutes - timePolicy.LateInTolerance;
                    }
                }
                else
                {
                    dedLateInMinutes = 0;
                }
            }
        }

        return Task.FromResult(Math.Round(dedLateInMinutes < 0 ? 0 : dedLateInMinutes, 3));
    }

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetEarlyOut</c> (legacy lines 708-757). Note the leave branch subtracts
    /// <c>TimeOfDay</c>s while the no-leave branch subtracts full <c>DateTime</c>s -- legacy asymmetry,
    /// preserved.
    /// </summary>
    public Task<double> GetEarlyOutAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        double earlyOutMinutes = 0;

        if (timeRegister.ActualEndTime1 < timeRegister.ShiftEndTime1 && HasPunchOut(context))
        {
            if (context.Settings != null && context.Settings.IsHourlyLeaveLogicEnabled)
            {
                if (IsFullDayHours(context) == false)
                {
                    var punchOutTime = timeRegister.ShiftEndTime1;
                    var absenceAtPunchOut = LeaveAtPunchOut(context, punchOutTime);

                    if (absenceAtPunchOut != null)
                    {
                        var datePart = absenceAtPunchOut.StartDate;
                        var timePart = absenceAtPunchOut.StartTime; // Take StartTime to Check deduction
                        var leaveStartTimeAtPunchOut = new DateTime(datePart.Year, datePart.Month, datePart.Day,
                            timePart.Hour, timePart.Minute, timePart.Second);

                        earlyOutMinutes = leaveStartTimeAtPunchOut.TimeOfDay
                            .Subtract(timeRegister.ActualEndTime1.TimeOfDay).TotalMinutes;
                    }
                    else
                    {
                        earlyOutMinutes = timeRegister.ShiftEndTime1.Subtract(timeRegister.ActualEndTime1)
                            .TotalMinutes;
                    }
                }
            }
            else
            {
                earlyOutMinutes = timeRegister.ShiftEndTime1.Subtract(timeRegister.ActualEndTime1).TotalMinutes;
            }
        }

        return Task.FromResult(Math.Round(earlyOutMinutes < 0 ? 0 : earlyOutMinutes, 3));
    }

    /// <summary>Legacy <c>GeneralTimeService.GetDeductionEarlyOut</c> (legacy lines 759-789).</summary>
    public Task<double> GetDeductionEarlyOutAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var duty = DutyOf(context);
        var timePolicy = TimePolicyOf(context);

        double dedEarlyOutMinutes = 0;

        if (duty != null && !(duty.IsWeeklyOff || duty.IsRestDay)
                         && timeRegister.ActualEndTime1 < timeRegister.ShiftEndTime1)
        {
            dedEarlyOutMinutes = timeRegister.EarlyOut;

            if (dedEarlyOutMinutes > 0) // if you leave 15 mins early
            {
                if (dedEarlyOutMinutes > timePolicy.EarlyOutTolerance) // if tolerance is 10 mins
                {
                    if (timePolicy.CalcEarlyOutAfterTolerance) // if calculate deduction after tolerance
                    {
                        dedEarlyOutMinutes = dedEarlyOutMinutes - timePolicy.EarlyOutTolerance;
                    }
                }
                else
                {
                    dedEarlyOutMinutes = 0;
                }
            }
        }

        return Task.FromResult(Math.Round(dedEarlyOutMinutes < 0 ? 0 : dedEarlyOutMinutes, 3));
    }

    /// <summary>Legacy <c>GeneralTimeService.GetBreakEarlyOut</c> (legacy lines 791-804).</summary>
    public Task<double> GetBreakEarlyOutAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        var breakEarlyOutMinutes = (timeRegister.ActualBreakStartTime < timeRegister.BreakStartTime)
            ? timeRegister.BreakStartTime.Subtract(timeRegister.ActualBreakStartTime).TotalMinutes
            : 0;

        return Task.FromResult(Math.Round(breakEarlyOutMinutes < 0 ? 0 : breakEarlyOutMinutes, 3));
    }

    /// <summary>Legacy <c>GeneralTimeService.GetBreakDeductionEarlyOut</c> (legacy lines 806-838). Note that
    /// unlike <see cref="GetDeductionEarlyOutAsync"/> this recomputes the raw difference rather than reading
    /// <see cref="TimeRegister.BreakEarlyOut"/>, and has no IsWeeklyOff/IsRestDay guard -- preserved.</summary>
    public Task<double> GetBreakDeductionEarlyOutAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var timePolicy = TimePolicyOf(context);

        double dedBreakEarlyOutMinutes = 0;

        if (timeRegister.ActualBreakStartTime < timeRegister.BreakStartTime)
        {
            dedBreakEarlyOutMinutes = timeRegister.BreakStartTime.Subtract(timeRegister.ActualBreakStartTime)
                .TotalMinutes;

            if (dedBreakEarlyOutMinutes > 0)
            {
                if (dedBreakEarlyOutMinutes > timePolicy.BreakEarlyOutTolerance)
                {
                    if (timePolicy.CalcBreakEarlyOutAfterTolerance)
                    {
                        dedBreakEarlyOutMinutes = dedBreakEarlyOutMinutes - timePolicy.BreakEarlyOutTolerance;
                    }
                }
                else
                {
                    dedBreakEarlyOutMinutes = 0;
                }
            }
        }

        return Task.FromResult(Math.Round(dedBreakEarlyOutMinutes < 0 ? 0 : dedBreakEarlyOutMinutes, 3));
    }

    /// <summary>Legacy <c>GeneralTimeService.GetBreakLateIn</c> (legacy lines 840-854).</summary>
    public Task<double> GetBreakLateInAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        var breakLateInMinutes = (timeRegister.ActualBreakEndTime > timeRegister.BreakEndTime)
            ? timeRegister.ActualBreakEndTime.Subtract(timeRegister.BreakEndTime).TotalMinutes
            : 0;

        return Task.FromResult(Math.Round(breakLateInMinutes < 0 ? 0 : breakLateInMinutes, 3));
    }

    /// <summary>Legacy <c>GeneralTimeService.GetBreakDeductionLateIn</c> (legacy lines 856-888).</summary>
    public Task<double> GetBreakDeductionLateInAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var timePolicy = TimePolicyOf(context);

        double dedBreakLateInMinutes = 0;

        if (timeRegister.ActualBreakEndTime > timeRegister.BreakEndTime)
        {
            dedBreakLateInMinutes = timeRegister.ActualBreakEndTime.Subtract(timeRegister.BreakEndTime)
                .TotalMinutes;

            if (dedBreakLateInMinutes > 0)
            {
                if (dedBreakLateInMinutes > timePolicy.BreakLateInTolerance)
                {
                    if (timePolicy.CalcBreakLateInAfterTolerance)
                    {
                        dedBreakLateInMinutes = dedBreakLateInMinutes - timePolicy.BreakLateInTolerance;
                    }
                }
                else
                {
                    dedBreakLateInMinutes = 0;
                }
            }
        }

        return Task.FromResult(Math.Round(dedBreakLateInMinutes < 0 ? 0 : dedBreakLateInMinutes, 3));
    }

    /// <summary>Legacy <c>GeneralTimeService.GetDedLateInEarlyOutMinutes</c> (legacy lines 890-898).</summary>
    public Task<double> GetDedLateInEarlyOutMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        var result = (timeRegister.PlannedWorkingMins != 0)
            ? Math.Round(timeRegister.DeductionLateIn
                         + timeRegister.DeductionEarlyOut
                         + timeRegister.BreakDeductionEarlyOut
                         + timeRegister.BreakDeductionLateIn, 3)
            : 0;

        return Task.FromResult(result);
    }

    /// <summary>Legacy <c>GeneralTimeService.GetDedLateInEarlyOutDay</c> (legacy lines 900-905).</summary>
    public Task<double> GetDedLateInEarlyOutDayAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        var result = (timeRegister.PlannedWorkingMins != 0)
            ? Math.Round(timeRegister.DedLateInEarlyOutMins / timeRegister.PlannedWorkingMins, 3)
            : 0;

        return Task.FromResult(result);
    }

    #endregion
}
