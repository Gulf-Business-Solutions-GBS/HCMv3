using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Legacy <c>GeneralTimeService</c> region "Overtime Calculation" (lines 1115-1196).
/// </summary>
public partial class TimeRegisterFieldCalculator
{
    #region << Overtime Calculation >>

    /// <summary>Legacy <c>GeneralTimeService.GetOvertimeMinutes</c> (legacy lines 1117-1125).
    /// NOT called when the day's policy is FlexiTime.</summary>
    public Task<double> GetOvertimeMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var timePolicy = TimePolicyOf(context);

        var overtimeMins = timeRegister.TotalWorkingMins
                           - (timeRegister.ActualWorkingMins + timeRegister.ActualAbsenceMins);

        var otMinLimit = timePolicy?.OTMinLimit ?? 0;

        var result = (overtimeMins > 0 && overtimeMins < otMinLimit) ? 0
            : (overtimeMins < 0) ? 0
            : overtimeMins;

        return Task.FromResult(Math.Round(result, 3));
    }

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetOvertimeType</c> (legacy lines 1127-1134). Note that the public-holiday
    /// branch searches <c>Program.holidayPeriodList</c> by DATE ONLY -- it does NOT restrict to the employee's
    /// own HolidayCalendar the way <c>GetPublicHoliday</c> does. Preserved verbatim.
    /// </summary>
    public Task<int?> GetOvertimeTypeIdAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        if (timeRegister.PublicHoliday != 1)
        {
            var duty = DutyOf(context);
            return Task.FromResult(duty?.OvertimeTypeId);
        }

        var dayDate = timeRegister.DayDate;

        var holidayId = _holidayPeriodRepository.GetAll()
            .Where(r => r.HolidayDate == dayDate)
            .Select(r => r.HolidayId)
            .FirstOrDefault();

        if (holidayId == 0)
        {
            return Task.FromResult<int?>(null);
        }

        var overtimeTypeId = _holidayRepository.GetAll()
            .Where(r => r.Id == holidayId)
            .Select(r => r.OvertimeTypeId)
            .FirstOrDefault();

        return Task.FromResult(overtimeTypeId);
    }

    /// <summary>
    /// Legacy <c>GeneralTimeService.CalculateOvertime</c> (legacy lines 1136-1194). Side-effecting: it
    /// creates, updates or deletes the day's <see cref="EmployeeOvertime"/> row. The legacy
    /// <c>empSubGroup</c> parameter is <see cref="TimeEngineEmployeeContext.EmployeeSubGroup"/> and
    /// <c>empOvertimeList</c> is <see cref="TimeEngineEmployeeContext.Overtimes"/>.
    ///
    /// <para><b>Throwing on an unresolved OvertimeType is FAITHFUL, not a divergence -- verified.</b>
    /// Although legacy's <c>EmployeeOvertime.OvertimeType</c> was a nullable XPO reference and legacy assigned
    /// <c>timeRegister.OvertimeType</c> to it unconditionally, legacy's own
    /// <c>EmployeeOvertime.PerformValidations()</c> (legacy
    /// <c>BusinessObjects/PersonnelAdministration/EmployeeOvertime.cs</c> lines 156-157) throws
    /// <c>"OvertimeType Cannot be blank."</c> on save. Legacy therefore could never PERSIST a null
    /// OvertimeType either -- it failed at commit instead of at assignment. This port simply fails earlier,
    /// with a more actionable message.</para>
    ///
    /// <para>Consequently this port's NON-nullable <see cref="EmployeeOvertime.OvertimeTypeId"/> correctly
    /// models legacy's real constraint. <b>Do NOT "fix" it to <c>int?</c></b> -- that would weaken a
    /// validation legacy actually enforced. (<see cref="TimeRegister.OvertimeTypeId"/> stays nullable because
    /// a day legitimately may have no overtime type; only the persisted EmployeeOvertime row requires one.)</para>
    /// </summary>
    public async Task CalculateOvertimeAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var timePolicy = TimePolicyOf(context);

        if (timePolicy != null && timePolicy.TimeMgmtStatus == TimeMgmtStatus.PlannedTime)
        {
            return;
        }

        var employeeSubGroup = context.EmployeeContext?.EmployeeSubGroup;
        var overtimes = context.EmployeeContext?.Overtimes
                        ?? new System.Collections.Generic.List<EmployeeOvertime>();

        var dayDate = timeRegister.DayDate;

        // get employee overtime and if found then update ActualOvertimeMinutes and if not found then create one.
        var empOvertime = overtimes.FirstOrDefault(r => r.EffectiveDate == dayDate);

        if (timeRegister.OvertimeMinutes > 0)
        {
            if (employeeSubGroup != null && employeeSubGroup.PayOvertime)
            {
                if (!timeRegister.OvertimeTypeId.HasValue || timeRegister.OvertimeTypeId.Value == 0)
                {
                    throw new InvalidOperationException(string.Format(
                        "No OvertimeType could be resolved for Date '{0}', but EmployeeOvertime.OvertimeTypeId is required. "
                        + "Set an OvertimeType on the Duty (or on the Holiday for a public holiday).",
                        dayDate.ToShortDateString()));
                }

                var isNew = empOvertime == null;
                if (isNew)
                {
                    empOvertime = new EmployeeOvertime
                    {
                        TenantId = EmployeeOf(context)?.TenantId ?? timeRegister.TenantId,
                        EmployeeId = timeRegister.EmployeeId,
                        EffectiveDate = dayDate,
                        OvertimeHours = Math.Round(timeRegister.OvertimeMinutes / 60, 3),
                        ActualOvertimeHours = Math.Round(timeRegister.OvertimeMinutes / 60, 3),
                        ApprovedOvertimeHours = Math.Round(timeRegister.OvertimeMinutes / 60, 3)
                    };
                }

                empOvertime.OvertimeTypeId = timeRegister.OvertimeTypeId.Value;
                empOvertime.ActualOvertimeHours = Math.Round(timeRegister.OvertimeMinutes / 60, 3);

                if (empOvertime.ApprovedOvertimeHours == 0)
                {
                    empOvertime.ApprovedOvertimeHours = Math.Round(empOvertime.ActualOvertimeHours, 3);
                }

                if (isNew)
                {
                    await _employeeOvertimeRepository.InsertAsync(empOvertime);
                    overtimes.Add(empOvertime);
                }
                else
                {
                    await _employeeOvertimeRepository.UpdateAsync(empOvertime);
                }
            }
            else if (empOvertime != null)
            {
                await _employeeOvertimeRepository.DeleteAsync(empOvertime.Id);
                overtimes.Remove(empOvertime);
            }
        }
        else if (timeRegister.OvertimeMinutes <= 0 && empOvertime != null)
        {
            if (empOvertime.ApprovedOvertimeHours > 0)
            {
                throw new Exception(string.Format(
                    "Approved Overtime found for Date '{0}'. Please contact to admin.",
                    dayDate.ToShortDateString()));
            }

            await _employeeOvertimeRepository.DeleteAsync(empOvertime.Id);
            overtimes.Remove(empOvertime);
        }
    }

    #endregion
}
