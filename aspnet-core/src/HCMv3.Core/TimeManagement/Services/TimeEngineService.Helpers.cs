using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Per-day/per-month helper half of <see cref="TimeEngineService"/> (see that file's remarks for the
/// overall porting contract). Split out purely for file size; this is the same partial class.
/// </summary>
public partial class TimeEngineService
{
    #region << Quota creation (delegates to IAccrualService) >>

    /// <summary>
    /// Result of <see cref="EnsureAnnualLeaveQuotaAsync"/>. Legacy used two <c>out</c>-style locals
    /// (<c>leaveQuotaFound</c>/<c>newLeaveQuota</c>, legacy <c>Program.cs</c> lines ~467-468); <c>out</c>
    /// parameters are illegal on async methods (CS1988), so they are returned on this object instead.
    /// <c>newLeaveQuota</c> is per-EMPLOYEE sticky and therefore lives on
    /// <see cref="TimeEngineEmployeeContext.AnnualLeaveQuotaCreated"/>, not here.
    /// </summary>
    private sealed class AnnualLeaveQuotaResult
    {
        public EmployeeAbsenceQuota Quota { get; set; }

        /// <summary>Legacy <c>leaveQuotaFound</c>: true when a quota row already covered the day.</summary>
        public bool LeaveQuotaFound { get; set; }
    }

    /// <summary>Legacy's open-ended deduction window: <c>new DateTime(9999, 12, 31)</c>.</summary>
    private static readonly DateTime QuotaDeductionWindowEnd = new DateTime(9999, 12, 31);

    /// <summary>
    /// Ported from legacy <c>Program.CreateTimeRegister</c>'s "Employee Leave Quota Record" block
    /// (legacy <c>Program.cs</c> lines ~567-620).
    ///
    /// <para>Legacy looks for an <see cref="EmployeeAbsenceQuota"/> row for the annual-leave type whose
    /// <c>EntitlementFrom</c>..<c>EntitlementTo</c> window contains the day. If none exists it creates one
    /// (seeded from <see cref="IAccrualService"/>'s accrual-period resolution), and throws when a
    /// pre-existing out-of-window quota is found -- that throw is a data-integrity guard, preserved.</para>
    /// </summary>
    private async Task<AnnualLeaveQuotaResult> EnsureAnnualLeaveQuotaAsync(
        TimeRegisterDayContext context, EmployeeSubGroup employeeSubGroup)
    {
        var employeeContext = context.EmployeeContext;
        var dayDate = context.DayDate;

        var existing = employeeContext.AnnualLeaveQuotas
            .FirstOrDefault(r => r.EntitlementFrom <= dayDate && r.EntitlementTo >= dayDate);

        if (existing != null)
        {
            return new AnnualLeaveQuotaResult { Quota = existing, LeaveQuotaFound = true };
        }

        // No quota covers this day. Legacy only creates one when the sub-group actually grants annual
        // leave (empSubGroupLeave != null); otherwise it leaves the quota null and carries on.
        if (context.AnnualLeaveSubGroupAbsence == null)
        {
            return new AnnualLeaveQuotaResult { Quota = null, LeaveQuotaFound = false };
        }

        // Legacy guard (Program.cs ~578-581): an annual-leave quota exists for the employee but does NOT
        // cover this day -- that is a data problem the engine refuses to paper over.
        var outOfWindow = employeeContext.AnnualLeaveQuotas.FirstOrDefault();
        if (outOfWindow != null)
        {
            throw new Exception(string.Format(
                "AbsenceQuota exists for Employee : {0}, LeaveType : {1}, EntitlementFrom : {2}",
                context.Employee.Code,
                context.MasterData.AnnualLeave?.NameEn,
                outOfWindow.EntitlementFrom.ToString("dd/MM/yyyy")));
        }

        var (entitlementFrom, entitlementTo) = await _accrualService.GetLeaveAccrualStartEndAsync(
            context.Employee.Id, context.MasterData.AnnualLeaveTypeId, dayDate);

        var quota = new EmployeeAbsenceQuota
        {
            TenantId = context.Employee.TenantId,
            EmployeeId = context.Employee.Id,
            AbsenceTypeId = context.MasterData.AnnualLeaveTypeId,
            EntitlementFrom = entitlementFrom,
            EntitlementTo = entitlementTo,

            // Legacy Program.cs 596-597: annual leave can be taken from the entitlement date plus the
            // tenant's "leave deduction start month", and the window never closes. Left unset, the quota
            // exists but no leave can ever be booked against it -- "No leave quota covering these dates".
            DeductionFrom = entitlementFrom.AddMonths(context.Settings.LeaveDeductionStartMonth),
            DeductionTo = QuotaDeductionWindowEnd
        };

        quota.Id = await _employeeAbsenceQuotaRepository.InsertAndGetIdAsync(quota);
        employeeContext.AnnualLeaveQuotas.Add(quota);

        // Sticky for the remainder of this employee's run -- see AnnualLeaveQuotaCreated's remarks.
        employeeContext.AnnualLeaveQuotaCreated = true;

        return new AnnualLeaveQuotaResult { Quota = quota, LeaveQuotaFound = false };
    }

    /// <summary>
    /// Ported from legacy <c>Program.CreateTimeRegister</c>'s non-annual accrual-leave quota block.
    /// Creates a covering <see cref="EmployeeAbsenceQuota"/> for every OTHER accrual-bearing absence type
    /// (legacy queries these as <c>empleaveQuotaList</c>, <c>Program.cs</c> line ~461).
    /// </summary>
    private async Task EnsureAccrualLeaveQuotasAsync(
        TimeRegisterDayContext context, EmployeeSubGroup employeeSubGroup)
    {
        var employeeContext = context.EmployeeContext;
        var dayDate = context.DayDate;

        foreach (var absenceType in context.MasterData.AnnualLeaveTypes
                     .Where(t => t.Id != context.MasterData.AnnualLeaveTypeId))
        {
            var covered = employeeContext.OtherLeaveQuotas.Any(
                r => r.AbsenceTypeId == absenceType.Id
                     && r.EntitlementFrom <= dayDate
                     && r.EntitlementTo >= dayDate);

            if (covered)
            {
                continue;
            }

            var subGroupAbsence = GetEmployeeSubGroupLeave(
                context.MasterData, absenceType.Id, context.Employee, dayDate, employeeSubGroup);

            if (subGroupAbsence == null)
            {
                continue;
            }

            var (entitlementFrom, entitlementTo) = await _accrualService.GetLeaveAccrualStartEndAsync(
                context.Employee.Id, absenceType.Id, dayDate);

            var quota = new EmployeeAbsenceQuota
            {
                TenantId = context.Employee.TenantId,
                EmployeeId = context.Employee.Id,
                AbsenceTypeId = absenceType.Id,
                EntitlementFrom = entitlementFrom,
                EntitlementTo = entitlementTo,

                // Legacy Program.cs 696-697: every other accrual leave is deductible from its entitlement
                // date, with no delay and no end.
                DeductionFrom = entitlementFrom,
                DeductionTo = QuotaDeductionWindowEnd
            };

            quota.Id = await _employeeAbsenceQuotaRepository.InsertAndGetIdAsync(quota);
            employeeContext.OtherLeaveQuotas.Add(quota);
        }
    }

    /// <summary>
    /// Ported from legacy <c>Program.CreateTimeRegister</c>'s gratuity-quota block (legacy
    /// <c>empIndemnityQuotaList</c>, <c>Program.cs</c> line ~464). Only creates a row when the employee's
    /// sub-group actually carries a gratuity rule for the day.
    /// </summary>
    private async Task EnsureGratuityQuotaAsync(
        TimeRegisterDayContext context, EmployeeSubGroup employeeSubGroup)
    {
        var employeeContext = context.EmployeeContext;
        var dayDate = context.DayDate;

        var covered = employeeContext.GratuityQuotas.Any(
            r => r.EntitlementFrom <= dayDate && r.EntitlementTo >= dayDate);

        if (covered || !HasEmployeeSubGroupGratuity(context.MasterData, dayDate, employeeSubGroup))
        {
            return;
        }

        // Legacy seeds the gratuity entitlement window from the same work-year boundaries the annual-leave
        // accrual uses; delegated rather than re-derived.
        var (entitlementFrom, entitlementTo) = await _accrualService.GetLeaveAccrualStartEndAsync(
            context.Employee.Id, context.MasterData.AnnualLeaveTypeId, dayDate);

        var quota = new EmployeeGratuityQuota
        {
            TenantId = context.Employee.TenantId,
            EmployeeId = context.Employee.Id,
            EntitlementFrom = entitlementFrom,
            EntitlementTo = entitlementTo,

            // Legacy Program.cs 1124-1125.
            DeductionFrom = entitlementFrom,
            DeductionTo = QuotaDeductionWindowEnd
        };

        quota.Id = await _employeeGratuityQuotaRepository.InsertAndGetIdAsync(quota);
        employeeContext.GratuityQuotas.Add(quota);
    }

    #endregion

    #region << Per-day field calculation (the T2/T3 call sequence) >>

    /// <summary>
    /// Ported from legacy <c>Program.CreateTimeRegister</c>'s "Day Calculation Logic" through
    /// "OverTime Logic" regions (legacy <c>Program.cs</c> lines 1180-1386).
    ///
    /// <para><b>Order is load-bearing.</b> Later calculators read fields written by earlier ones (e.g.
    /// <c>GetAbsentDay</c> reads <c>ActualWorkingDay</c>). This method assigns each result onto
    /// <see cref="TimeRegisterDayContext.TimeRegister"/> before the next call, exactly as legacy did --
    /// see <see cref="ITimeRegisterFieldCalculator"/>'s ordering contract. Do NOT reorder.</para>
    ///
    /// <para><b>ActiveDay gating stays here</b>, not in the calculators: legacy wraps nearly every call in
    /// <c>ActiveDay == 1 ? X : 0</c>, and the calculators are documented as never being called when it is 0.</para>
    /// </summary>
    private async Task ApplyDayFieldsAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var settings = context.Settings;
        var duty = context.Duty;
        var isNotReturnFromLeave = context.IsNotReturnFromLeave;
        var isFlexi = context.TimePolicy != null && context.TimePolicy.TimeMgmtStatus == TimeMgmtStatus.FlexiTime;

        #region << ActiveDay / PublicHoliday / WeeklyOff / RestDay (legacy 1184-1218) >>

        timeRegister.ActiveDay = await _fieldCalculator.GetActiveDayAsync(context);
        timeRegister.InactiveDay = timeRegister.ActiveDay == 1 ? 0 : 1;

        timeRegister.PublicHoliday = (timeRegister.ActiveDay == 1 && !isNotReturnFromLeave)
            ? await _fieldCalculator.GetPublicHolidayAsync(context)
            : 0;

        timeRegister.WeeklyOff = duty.IsWeeklyOff ? 1 : 0;

        // Legacy CountWeekoffOnPH (1192-1203): the two flags are mutually exclusive per-day; which one
        // survives a collision is a payroll-visible setting.
        if (settings.CountWeekoffOnPH)
        {
            if (timeRegister.WeeklyOff == 1)
            {
                timeRegister.PublicHoliday = 0;
            }
        }
        else if (timeRegister.PublicHoliday == 1)
        {
            timeRegister.WeeklyOff = 0;
        }

        timeRegister.RestDay = timeRegister.ActiveDay == 1 && duty.IsRestDay ? 1 : 0;

        // Legacy CountRestDayOnPH (1207-1218), same shape as above.
        if (settings.CountRestDayOnPH)
        {
            if (timeRegister.RestDay == 1)
            {
                timeRegister.PublicHoliday = 0;
            }
        }
        else if (timeRegister.PublicHoliday == 1)
        {
            timeRegister.RestDay = 0;
        }

        #endregion

        #region << Shift/break DateTime composition (legacy 1222-1244) >>

        timeRegister.ShiftStartTime1 = CombineDateAndTime(timeRegister.DayDate, duty.StartTime);
        timeRegister.ShiftEndTime1 = duty.IsNightShift
            ? CombineDateAndTime(timeRegister.DayDate.AddDays(1), duty.EndTime)
            : CombineDateAndTime(timeRegister.DayDate, duty.EndTime);

        // Legacy data-integrity guard (1228-1231) -- a misconfigured Duty would otherwise silently produce
        // negative planned minutes for every employee on it.
        if (timeRegister.ShiftStartTime1 > timeRegister.ShiftEndTime1)
        {
            throw new Exception(
                "Duty : " + duty.NameEn + " has StartTime > EndTime. Check IsNightshift or not.");
        }

        var breakShift = duty.BreakShift;
        timeRegister.BreakStartTime = breakShift == null
            ? default
            : CombineDateAndTime(
                breakShift.IsBreakStartNextDay ? timeRegister.DayDate.AddDays(1) : timeRegister.DayDate,
                breakShift.StartTime);

        timeRegister.BreakEndTime = breakShift == null
            ? default
            : CombineDateAndTime(
                (breakShift.IsBreakStartNextDay || breakShift.IsBreakEndNextDay)
                    ? timeRegister.DayDate.AddDays(1)
                    : timeRegister.DayDate,
                breakShift.EndTime);

        #endregion

        #region << Planned (legacy 1246-1249) >>

        timeRegister.PlannedWorkingMins = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetPlannedWorkingMinutesAsync(context)
            : 0;
        timeRegister.PlannedWorkingDay = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetPlannedWorkingDayAsync(context)
            : 0;

        #endregion

        #region << Leave days (legacy 1251-1261) >>

        timeRegister.AnnualLeaveDay = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetAnnualPaidLeaveDayAsync(context)
            : 0;
        timeRegister.UnpaidLeaveDay = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetUnPaidLeaveDayAsync(context)
            : 0;
        timeRegister.SickLeavePaidDay = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetSickLeavePaidDayAsync(context)
            : 0;
        timeRegister.SickLeaveUnpaidDay = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetSickLeaveUnPaidDayAsync(context)
            : 0;
        timeRegister.LeaveTypeId = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetLeaveTypeIdAsync(context)
            : null;
        timeRegister.LeaveDay = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetLeaveDayAsync(context)
            : 0;
        timeRegister.LeaveMinutes = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetLeaveMinutesAsync(context)
            : 0;
        timeRegister.IncludeInLeavePay = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetIncludeInLeaveDayAsync(context)
            : 0;
        timeRegister.LeaveAdjDay = 0;

        #endregion

        #region << Actual punch resolution (legacy 1263-1316) >>

        var punch = _punchStrategyResolver.Resolve(
            context.TimePolicy?.TimeMgmtStatus ?? TimeMgmtStatus.TimeSheet);

        // Legacy gates the four "in/out" punches on this compound condition; the four "ActualXxxTime"
        // fields below are NOT gated (legacy 1296-1315 computes them unconditionally).
        var resolvePunches = timeRegister.ActiveDay == 1 && !isNotReturnFromLeave && timeRegister.LeaveDay != 1;

        timeRegister.ActualIn = resolvePunches ? await punch.GetActualInAsync(context) : default;
        timeRegister.BreakOut = resolvePunches ? await punch.GetBreakOutAsync(context) : default;
        timeRegister.BreakIn = resolvePunches ? await punch.GetBreakInAsync(context) : default;
        timeRegister.ActualOut = resolvePunches ? await punch.GetActualOutAsync(context) : default;

        timeRegister.ActualStartTime1 = await punch.GetActualStartTimeAsync(context);
        timeRegister.ActualBreakStartTime = await punch.GetActualBreakStartTimeAsync(context);
        timeRegister.ActualBreakEndTime = await punch.GetActualBreakEndTimeAsync(context);
        timeRegister.ActualEndTime1 = await punch.GetActualEndTimeAsync(context);

        #endregion

        #region << Attendance (legacy 1318-1325) >>

        if (settings.IsAutoAttendanceEnabled)
        {
            await _fieldCalculator.CalculateAutoAttendanceAsync(context);
        }

        timeRegister.AttendanceDay = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetAttendanceDayAsync(context)
            : 0;
        timeRegister.AttendanceTypeId = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetAttendanceTypeIdAsync(context)
            : null;
        timeRegister.AttendanceMinutes = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetAttendanceMinutesAsync(context)
            : 0;

        #endregion

        #region << Late-in / early-out deductions (legacy 1327-1341) >>

        // NOTE: FlexiTime zeroes ALL of these in legacy -- it is not an IFlexiTimeCalculationOverride case,
        // the value is literally 0 (legacy 1328-1340).
        timeRegister.LateIn = timeRegister.ActiveDay == 1 && !isFlexi
            ? await _fieldCalculator.GetLateInAsync(context) : 0;
        timeRegister.DeductionLateIn = timeRegister.ActiveDay == 1 && !isFlexi
            ? await _fieldCalculator.GetDeductionLateInAsync(context) : 0;
        timeRegister.EarlyOut = timeRegister.ActiveDay == 1 && !isFlexi
            ? await _fieldCalculator.GetEarlyOutAsync(context) : 0;
        timeRegister.DeductionEarlyOut = timeRegister.ActiveDay == 1 && !isFlexi
            ? await _fieldCalculator.GetDeductionEarlyOutAsync(context) : 0;

        timeRegister.BreakEarlyOut = timeRegister.ActiveDay == 1 && !isFlexi
            ? await _fieldCalculator.GetBreakEarlyOutAsync(context) : 0;
        timeRegister.BreakDeductionEarlyOut = timeRegister.ActiveDay == 1 && !isFlexi
            ? await _fieldCalculator.GetBreakDeductionEarlyOutAsync(context) : 0;
        timeRegister.BreakLateIn = timeRegister.ActiveDay == 1 && !isFlexi
            ? await _fieldCalculator.GetBreakLateInAsync(context) : 0;
        timeRegister.BreakDeductionLateIn = timeRegister.ActiveDay == 1 && !isFlexi
            ? await _fieldCalculator.GetBreakDeductionLateInAsync(context) : 0;

        context.PreviousValues.DedLateInEarlyOutMins = timeRegister.DedLateInEarlyOutMins;
        timeRegister.DedLateInEarlyOutMins = timeRegister.ActiveDay == 1 && !isFlexi
            ? await _fieldCalculator.GetDedLateInEarlyOutMinutesAsync(context) : 0;
        timeRegister.DedLateInEarlyOutDay = timeRegister.ActiveDay == 1 && !isFlexi
            ? await _fieldCalculator.GetDedLateInEarlyOutDayAsync(context) : 0;

        #endregion

        #region << Working minutes / days (legacy 1343-1360) >>

        var flexiOverride = punch as IFlexiTimeCalculationOverride;

        timeRegister.TotalWorkingMins = timeRegister.ActiveDay == 1
            ? (isFlexi && flexiOverride != null
                ? await flexiOverride.GetTotalWorkingMinutesAsync(context)
                : await _fieldCalculator.GetTotalWorkingMinutesAsync(context))
            : 0;

        // Legacy 1352: deliberately convoluted condition covering future-dated runs where the employee is
        // nonetheless on leave. Preserved verbatim rather than simplified.
        var computeAbsenceMins =
            (timeRegister.ActiveDay == 1 && !context.IsFutureDate)
            || (timeRegister.ActiveDay == 1 && timeRegister.LeaveDay > 0)
            || (timeRegister.ActiveDay == 1 && timeRegister.LeaveMinutes != 0)
            || isNotReturnFromLeave;

        timeRegister.ActualAbsenceMins = computeAbsenceMins
            ? (isFlexi && flexiOverride != null
                ? await flexiOverride.GetActualAbsenceMinutesAsync(context)
                : await _fieldCalculator.GetActualAbsenceMinutesAsync(context))
            : 0;

        timeRegister.ActualWorkingMins = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetActualWorkingMinutesAsync(context)
            : 0;

        context.PreviousValues.ActualWorkingDay = timeRegister.ActualWorkingDay;
        timeRegister.ActualWorkingDay = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetActualWorkingDayAsync(context)
            : 0;

        #endregion

        #region << Absent day (legacy 1362-1370) >>

        context.PreviousValues.AbsentDay = timeRegister.AbsentDay;

        var basicPayWageType = context.MasterData.CalendarDaysBasicPayWageType;
        var notReturnedCountsAsAbsent =
            isNotReturnFromLeave
            && (timeRegister.RestDay == 1
                || (timeRegister.WeeklyOff == 1
                    && basicPayWageType != null
                    && basicPayWageType.BillingMethod == BillingMethod.CalendarDaysPerMonth));

        timeRegister.AbsentDay = timeRegister.ActiveDay == 1
            ? (notReturnedCountsAsAbsent ? 1 : await _fieldCalculator.GetAbsentDayAsync(context))
            : 0;

        #endregion

        #region << Overtime (legacy 1372-1386) >>

        context.PreviousValues.OvertimeMinutes = timeRegister.OvertimeMinutes;

        if (settings.EnableOvertimeInTimeEngine)
        {
            timeRegister.OvertimeMinutes = timeRegister.ActiveDay == 1
                ? (isFlexi && flexiOverride != null
                    ? await flexiOverride.GetOvertimeMinutesAsync(context)
                    : await _fieldCalculator.GetOvertimeMinutesAsync(context))
                : 0;

            timeRegister.OvertimeTypeId = timeRegister.ActiveDay == 1
                ? await _fieldCalculator.GetOvertimeTypeIdAsync(context)
                : null;

            // Legacy calls this unconditionally inside the enableOT block (not ActiveDay-gated).
            await _fieldCalculator.CalculateOvertimeAsync(context);
        }

        #endregion
    }

    /// <summary>
    /// Legacy composed a shift <c>DateTime</c> from a date plus a time-of-day via
    /// <c>DateTimeExtension.GetDateTime</c>. This port's <see cref="Duty"/> exposes times as
    /// <see cref="TimeSpan"/>, so composition is a plain add.
    /// </summary>
    private static DateTime CombineDateAndTime(DateTime date, TimeSpan timeOfDay)
    {
        return date.Date.Add(timeOfDay);
    }

    #endregion

    #region << Approval fields >>

    /// <summary>
    /// Ported from legacy <c>Program.CreateTimeRegister</c>'s "Approved fields" region (legacy
    /// <c>Program.cs</c> lines 1457-1513). Only invoked when
    /// <see cref="TimeEngineSettings.EnableTimeRegisterApproval"/> is on.
    ///
    /// <para>Legacy shape: a row that is NEW / <see cref="TimeRegisterStatus.Opened"/> /
    /// <see cref="TimeRegisterStatus.Reopened"/> always has its Approved* columns mirrored from the freshly
    /// calculated originals, and is additionally flipped to <see cref="TimeRegisterStatus.Approved"/> when
    /// <see cref="TimeEngineSettings.AutoApproveTimeLog"/> is on AND the day has nothing worth approving
    /// (<c>AbsentDay == 0 &amp;&amp; DedLateInEarlyOutMins == 0</c>). An already-<c>Approved</c> row whose
    /// AbsentDay / ActualWorkingDay / DedLateInEarlyOutMins changed is re-opened and then also re-mirrored.</para>
    ///
    /// <para><b>T4 FIX.</b> The T1 version of this method mirrored the Approved* columns ONLY when
    /// <c>AutoApproveTimeLog</c> was on and zeroed them on new rows otherwise -- neither is legacy behaviour,
    /// and both are payroll-visible (the approval workflow reads Approved*). Corrected to the legacy shape
    /// above.</para>
    ///
    /// <para><b>APPROVED-OVERTIME FINDING (T4).</b> Legacy's approval block persists exactly three approved
    /// figures -- <c>ApprovedAbsentDay</c>, <c>ApprovedActualWorkingDay</c>,
    /// <c>ApprovedDedLateInEarlyOutMins</c> (legacy lines 1463-1465, 1508-1510) -- and NO approved overtime.
    /// Legacy <c>TimeLogHistory</c> mirrors the same three (lines 1490-1495). This port is therefore NOT
    /// missing a column; no entity or migration change is required.</para>
    ///
    /// <para><b>THE REOPEN PATH (completed here).</b> The T1/T4 version of this method collapsed legacy's
    /// Reopened-vs-Opened branch to an unconditional <see cref="TimeRegisterStatus.Reopened"/> and wrote no
    /// history, because this port had neither the <c>WfRequest</c> FK nor the <c>TimeLogHistory</c> entity.
    /// Both now exist (<see cref="TimeRegister.TimeLogApprovalId"/> and <see cref="TimeLogHistory"/>), so the
    /// branch is restored in full: an approved day whose figures move records what was approved, is set back
    /// to Reopened, has its approval link CLEARED, and has its Approved* triple reset to the freshly
    /// calculated raw figures. The clear is the load-bearing part -- without it the day stays permanently
    /// attached to a stale approval and can never be corrected again, because
    /// <c>TimeLogApprovalWorkflowTransactionHandler</c> refuses days that are still settled.</para>
    ///
    /// <para>A day that reached Approved WITHOUT an approval batch behind it -- the
    /// <see cref="TimeEngineSettings.AutoApproveTimeLog"/> shortcut above -- goes back to
    /// <see cref="TimeRegisterStatus.Opened"/> instead and writes no history, since there is no human
    /// decision to record. Legacy did the same (<c>Program.cs</c> lines 1503-1507).</para>
    /// </summary>
    private async Task ApplyApprovalFieldsAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var previous = context.PreviousValues;

        if (context.IsNewTimeRegisterRecord
            || timeRegister.Status == TimeRegisterStatus.Opened
            || timeRegister.Status == TimeRegisterStatus.Reopened)
        {
            timeRegister.ApprovedAbsentDay = timeRegister.AbsentDay;
            timeRegister.ApprovedActualWorkingDay = timeRegister.ActualWorkingDay;
            timeRegister.ApprovedDedLateInEarlyOutMins = timeRegister.DedLateInEarlyOutMins;

            // Legacy (2024.11.27): nothing to approve -> approve straight away so the day never surfaces as
            // a time-log request.
            if (context.Settings.AutoApproveTimeLog
                && timeRegister.AbsentDay == 0
                && timeRegister.DedLateInEarlyOutMins == 0)
            {
                timeRegister.Status = TimeRegisterStatus.Approved;
            }
        }
        else if (timeRegister.Status == TimeRegisterStatus.Approved
                 && (previous.AbsentDay != timeRegister.AbsentDay
                     || previous.ActualWorkingDay != timeRegister.ActualWorkingDay
                     || previous.DedLateInEarlyOutMins != timeRegister.DedLateInEarlyOutMins))
        {
            if (timeRegister.TimeLogApprovalId.HasValue)
            {
                timeRegister.Status = TimeRegisterStatus.Reopened;

                // Both triples, captured BEFORE anything below overwrites them: the raw figures as they stood
                // when the approval was granted, and the approved figures that are about to be discarded.
                await _timeLogHistoryRepository.InsertAsync(new TimeLogHistory
                {
                    TenantId = timeRegister.TenantId,
                    TimeLogApprovalId = timeRegister.TimeLogApprovalId.Value,
                    TimeRegisterId = timeRegister.Id,
                    DateOfAction = DateTime.Now,

                    AbsentDay = previous.AbsentDay,
                    ActualWorkingDay = previous.ActualWorkingDay,
                    DedLateInEarlyOutMins = previous.DedLateInEarlyOutMins,

                    ApprovedAbsentDay = timeRegister.ApprovedAbsentDay,
                    ApprovedActualWorkingDay = timeRegister.ApprovedActualWorkingDay,
                    ApprovedDedLateInEarlyOutMins = timeRegister.ApprovedDedLateInEarlyOutMins
                });

                // Released, so the day is eligible for a fresh TimeLogApproval.
                timeRegister.TimeLogApprovalId = null;
            }
            else
            {
                timeRegister.Status = TimeRegisterStatus.Opened;
            }

            timeRegister.ApprovedAbsentDay = timeRegister.AbsentDay;
            timeRegister.ApprovedActualWorkingDay = timeRegister.ActualWorkingDay;
            timeRegister.ApprovedDedLateInEarlyOutMins = timeRegister.DedLateInEarlyOutMins;
        }
    }

    #endregion

    #region << Payroll status retro trigger >>

    /// <summary>
    /// Ported from legacy <c>Program.CreateTimeRegister</c>'s post-day <c>EmployeePayrollStatus</c> update.
    /// Advances <c>NextTimeEvaluationFrom</c> past the day just evaluated, and raises
    /// <c>EarliestMasterDataChange</c> as a retro-accounting trigger when a payroll-visible field changed
    /// versus its pre-calculation snapshot (see <see cref="TimeRegisterPreviousValues"/>).
    /// </summary>
    private async Task UpdatePayrollStatusAfterDayAsync(TimeRegisterDayContext context)
    {
        var payrollStatus = context.EmployeeContext.PayrollStatus;
        if (payrollStatus == null)
        {
            return;
        }

        var timeRegister = context.TimeRegister;
        var previous = context.PreviousValues;

        var payrollVisibleChange =
            previous.AbsentDay != timeRegister.AbsentDay
            || previous.ActualWorkingDay != timeRegister.ActualWorkingDay
            || previous.DedLateInEarlyOutMins != timeRegister.DedLateInEarlyOutMins
            || previous.OvertimeMinutes != timeRegister.OvertimeMinutes;

        if (payrollVisibleChange
            && (payrollStatus.EarliestMasterDataChange == default
                || payrollStatus.EarliestMasterDataChange > context.DayDate))
        {
            payrollStatus.EarliestMasterDataChange = context.DayDate;
        }

        var nextEvaluationFrom = context.DayDate.AddDays(1);
        if (payrollStatus.NextTimeEvaluationFrom == default
            || payrollStatus.NextTimeEvaluationFrom < nextEvaluationFrom)
        {
            payrollStatus.NextTimeEvaluationFrom = nextEvaluationFrom;
        }

        await _employeePayrollStatusRepository.UpdateAsync(payrollStatus);
    }

    #endregion

    #region << Auto air-ticket pay (legacy Program.AutoAirTicketPay) >>

    /// <summary>
    /// Ported from legacy <c>Program.AutoAirTicketPay</c> (legacy <c>Program.cs</c> lines 2142-2331), called
    /// from the per-day loop when <see cref="EmployeeSubGroup.AutoPayAirTicket"/> is set (legacy line 1453).
    ///
    /// <para>Walks every <see cref="TicketType"/>. For each, the employee's entitlement (<c>TicketCount</c>)
    /// and <see cref="AccrualPeriod"/> come from the covering <see cref="EmployeeSubGroupTicket"/>; when the
    /// sub-group carries no rule for that ticket type, legacy falls back to a single WorkYear ticket that is
    /// only payable if an explicit exception row exists (<c>allowOnlyException</c>). A row is written only on
    /// the exact day the ticket's accrual window STARTS (<c>deductionStartDate == DayDate</c>).</para>
    ///
    /// <para><b>Legacy control flow preserved verbatim, including the <c>break</c>s</b> -- legacy exits the
    /// WHOLE ticket-type loop (not just the current iteration) when a zero-amount exception or an
    /// exception-only ticket type without an exception is hit. That is almost certainly a latent legacy
    /// defect (a `continue` would be the sane reading), but it is money-affecting behaviour and is reproduced
    /// exactly rather than "fixed".</para>
    ///
    /// <para>PORT DEVIATIONS: (a) legacy's nested <c>objSpace2</c> / <c>AuditTrailService</c> bracketing
    /// around the insert is dropped -- ABP owns the unit of work and auditing (see class remarks);
    /// (b) <c>IsSystemGenerated</c>/<c>SysCreator</c>/<c>SysCreatedOn</c> are not ported (no such columns);
    /// (c) legacy's <c>catch (Exception ex) { throw ex; }</c> wrappers are dropped -- they only destroy the
    /// stack trace.</para>
    /// </summary>
    private async Task AutoAirTicketPayAsync(TimeRegisterDayContext context, EmployeeSubGroup employeeSubGroup)
    {
        var employee = context.Employee;
        var dayDate = context.DayDate;

        double maxDepTicketCount = employeeSubGroup.MaxDependantAirTicketCount;
        double totalDepTicketCount = 0;

        foreach (var ticketType in context.MasterData.TicketTypes)
        {
            // Legacy 2152: stop once the dependant air-ticket cap has been consumed.
            if (ticketType.Code != "1" && maxDepTicketCount != 0 && totalDepTicketCount == maxDepTicketCount)
            {
                break;
            }

            #region << Get AccrualPeriod (legacy 2155-2176) >>

            double balanceQuota;
            AccrualPeriod accrualPeriod;
            var allowOnlyException = false;

            var empSubGroupTicket = GetEmployeeSubGroupTicket(dayDate, employeeSubGroup, ticketType.Id);

            if (empSubGroupTicket != null)
            {
                balanceQuota = empSubGroupTicket.TicketCount;
                accrualPeriod = empSubGroupTicket.AccrualPeriod;
            }
            else
            {
                balanceQuota = 1;
                accrualPeriod = AccrualPeriod.WorkYear;
                allowOnlyException = true;
            }

            #endregion

            #region << Calculate Ded Start and End (legacy 2178-2182) >>

            var (deductionStartDate, deductionEndDate) = await _accrualService.GetAirTicketAccrualStartEndAsync(
                employee.Id, ticketType.Id, accrualPeriod, dayDate);

            #endregion

            // Legacy 2185: only pay on the first day of the accrual window.
            if (deductionStartDate != dayDate)
            {
                continue;
            }

            var addTicket = false;
            double exceptionAmount = 0;

            #region << Check EmployeeBenefitException (legacy 2190-2206) >>

            EmployeeBenefitException empBenefitExc = null;
            if (ticketType.Code == "1")
            {
                empBenefitExc = _employeeBenefitExceptionRepository.GetAll()
                    .FirstOrDefault(r => r.EmployeeId == employee.Id
                                         && r.StartDate <= dayDate
                                         && r.EndDate >= dayDate
                                         && r.ExceptionBenefitType == ExceptionBenefitType.Ticket);

                // Exception amount of 0 means "explicitly not entitled" -- legacy aborts the whole loop.
                if (empBenefitExc != null && empBenefitExc.Amount == 0)
                {
                    break;
                }

                if (empBenefitExc != null && empBenefitExc.Amount != 0)
                {
                    exceptionAmount = empBenefitExc.Amount;
                }
            }

            #endregion

            #region << Check EmployeeDependantException (legacy 2208-2228) >>

            EmployeeDependantException empDependantExc = null;
            var empDependant = _employeeDependantRepository.GetAll()
                .FirstOrDefault(r => r.EmployeeId == employee.Id
                                     && r.TicketTypeId == ticketType.Id
                                     && r.StartDate <= dayDate
                                     && r.EndDate >= dayDate);

            if (empDependant != null)
            {
                // Legacy navigated empDependant.Exception (an XPO back-reference collection this port does
                // not have); resolved through the repository by EmployeeDependantId instead.
                empDependantExc = _employeeDependantExceptionRepository.GetAll()
                    .FirstOrDefault(r => r.EmployeeDependantId == empDependant.Id
                                         && r.ExceptionBenefitType == ExceptionBenefitType.Ticket
                                         && r.StartDate <= dayDate
                                         && r.EndDate >= dayDate);

                if (empDependantExc != null && empDependantExc.Amount == 0)
                {
                    break;
                }

                if (empDependantExc != null && empDependantExc.Amount != 0)
                {
                    exceptionAmount = empDependantExc.Amount;
                }
            }

            #endregion

            #region << Check if AllowOnlyException, then break process (legacy 2230-2239) >>

            if (allowOnlyException)
            {
                if (empBenefitExc == null && empDependantExc == null)
                {
                    break;
                }

                if ((empBenefitExc != null && empBenefitExc.Amount == 0)
                    || (empDependantExc != null && empDependantExc.Amount == 0))
                {
                    break;
                }
            }

            #endregion

            #region << Calculate Used AirTickets (legacy 2241-2252) >>

            var usedTicketCount = _employeeTicketRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id
                            && r.TicketTypeId == ticketType.Id
                            && r.PaymentDate >= deductionStartDate
                            && r.PaymentDate <= deductionEndDate)
                .Sum(r => (double?)r.Compensate) ?? 0;

            balanceQuota = balanceQuota - usedTicketCount;

            if (ticketType.Code != "1")
            {
                totalDepTicketCount = totalDepTicketCount + usedTicketCount;
            }

            #endregion

            #region << Check BalanceQuota (legacy 2254-2323) >>

            if (balanceQuota <= 0)
            {
                continue;
            }

            if (ticketType.Code == "1")
            {
                addTicket = true;
            }
            else if (ticketType.Code != "1"
                     && (IsDependantValid(context, ticketType, empSubGroupTicket) || exceptionAmount != 0))
            {
                addTicket = true;
            }

            if (!addTicket)
            {
                continue;
            }

            var empTicket = new EmployeeTicket
            {
                TenantId = employee.TenantId,
                EmployeeId = employee.Id,
                TicketTypeId = ticketType.Id,
                PaymentDate = dayDate,
                TicketDate = dayDate,
                TicketPaymentType = TicketPaymentType.PayWithPayroll,
                Compensate = balanceQuota
            };

            // Legacy: capture the exception amount if one was entered, and flag the override.
            if (exceptionAmount != 0)
            {
                empTicket.Amount = exceptionAmount;
                empTicket.OverrideAmount = true;
            }

            empTicket.Id = await _employeeTicketRepository.InsertAndGetIdAsync(empTicket);
            context.EmployeeContext.Tickets.Add(empTicket);

            if (ticketType.Code != "1")
            {
                totalDepTicketCount = totalDepTicketCount + 1;
            }

            #endregion
        }
    }

    /// <summary>
    /// Ported from legacy <c>Program.IsDependantValid</c> (legacy <c>Program.cs</c> lines 2332-2397).
    /// Age-limit check applied to dependant ticket types only; the employee's own ticket type
    /// (<c>Code == "1"</c>) always returns <c>false</c> here in legacy, which is harmless because the caller
    /// never asks about it. Legacy's commented-out throws are left commented-out (i.e. it returns false).
    /// </summary>
    private bool IsDependantValid(
        TimeRegisterDayContext context, TicketType airTicketType, EmployeeSubGroupTicket empSubGroupTicket)
    {
        var isDependantValid = false;

        if (airTicketType.Code == "1")
        {
            return isDependantValid;
        }

        var dayDate = context.DayDate;
        var ageLimit = empSubGroupTicket != null ? empSubGroupTicket.AgeLimit : 0;

        var empDependant = _employeeDependantRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == context.Employee.Id
                                 && r.TicketTypeId == airTicketType.Id
                                 && r.StartDate <= dayDate
                                 && r.EndDate >= dayDate);

        if (empDependant == null)
        {
            return isDependantValid;
        }

        var birthDate = empDependant.BirthDate;
        if (birthDate == default(DateTime))
        {
            return isDependantValid;
        }

        var dependantAge = Math.Round(dayDate.Subtract(birthDate.Date).TotalDays / 365, 2);

        return dependantAge > ageLimit ? isDependantValid : true;
    }

    #endregion

    #region << Monthly tolerance deduction (legacy Program.CalcMonthlyToleranceDeduction) >>

    /// <summary>
    /// Ported from legacy <c>Program.CalcMonthlyToleranceDeduction</c> (legacy <c>Program.cs</c> lines
    /// 2089-2141), called once per calendar month of the run window (legacy line 1639).
    ///
    /// <para>Finds the employee's <see cref="TimePolicy"/> covering either end of the month that has a
    /// <see cref="MonthlyToleranceType"/> selected, totals the month's late-in / early-out figures according
    /// to that type, and stamps the excess over <see cref="TimePolicy.MonthlyAfterTolerance"/> onto
    /// <see cref="TimeRegister.DedAfterMonthlyTol"/> of ONE row in the month.</para>
    ///
    /// <para><b>Legacy quirks preserved, deliberately:</b>
    /// (1) the target row is <c>OrderByDescending(DedAfterMonthlyTol).FirstOrDefault()</c>, i.e. the row that
    /// already carries the largest value -- on a first run every row is 0 so this is effectively arbitrary,
    /// and on re-runs it re-uses the row it previously stamped. It does NOT clear the value from any other
    /// row. (2) <c>CalcMonthlyToleranceOnActual</c> adds the per-occurrence tolerance BACK ON to each
    /// deducted occurrence (reconstructing the actual late/early minutes from the post-tolerance deduction),
    /// whereas <c>CalcMonthlyTolearnceOnDed</c> totals the post-tolerance deductions as-is. (3) the tolerance
    /// added back is <c>LateInTolerance</c>/<c>EarlyOutTolerance</c> -- NOT
    /// <c>MonthlyLateInTolerance</c>/<c>MonthlyEarlyOutTolerance</c>, which legacy never reads here.
    /// (4) the deduction is written as MINUTES, not a day.</para>
    /// </summary>
    private async Task CalcMonthlyToleranceDeductionAsync(
        Employee employee, TimeEngineEmployeeContext context, DateTime monthStart, DateTime monthEnd)
    {
        // Legacy resolved TimePolicy through the EmployeeTimePolicy navigation property; this port has no
        // eager loading in Core, so the policy ids are collected first and resolved in one query.
        var candidatePolicyIds = context.TimePolicies
            .Where(r => (r.StartDate <= monthStart && r.EndDate >= monthStart)
                        || (r.StartDate <= monthEnd && r.EndDate >= monthEnd))
            .Select(r => r.TimePolicyId)
            .ToList();

        if (!candidatePolicyIds.Any())
        {
            return;
        }

        var timePolicy = _timePolicyRepository.GetAll()
            .Where(r => candidatePolicyIds.Contains(r.Id)
                        && r.MonthlyToleranceType != MonthlyToleranceType.NotSelected)
            .FirstOrDefault();

        if (timePolicy == null)
        {
            return;
        }

        double monthlyAfterAllowanceTotal = timePolicy.MonthlyAfterTolerance;

        var monthRegisters = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id && r.DayDate >= monthStart && r.DayDate <= monthEnd)
            .Select(r => new { r.DeductionLateIn, r.DeductionEarlyOut })
            .ToList();

        var lateInTolerance = timePolicy.CalcLateInAfterTolerance ? timePolicy.LateInTolerance : 0;
        var earlyOutTolerance = timePolicy.CalcEarlyOutAfterTolerance ? timePolicy.EarlyOutTolerance : 0;

        var deductionLateInTotal = monthRegisters.Sum(r => r.DeductionLateIn);
        var deductionEarlyOutTotal = monthRegisters.Sum(r => r.DeductionEarlyOut);
        var lateInAllowanceTotalWithTolerance = monthRegisters
            .Where(r => r.DeductionLateIn > 0).Sum(r => r.DeductionLateIn + lateInTolerance);
        var earlyOutAllowanceTotalWithTolerance = monthRegisters
            .Where(r => r.DeductionEarlyOut > 0).Sum(r => r.DeductionEarlyOut + earlyOutTolerance);

        var lateInEarlyOutAllowanceTotal =
            timePolicy.MonthlyToleranceType == MonthlyToleranceType.CalcMonthlyToleranceOnActual
                ? lateInAllowanceTotalWithTolerance + earlyOutAllowanceTotalWithTolerance
                : timePolicy.MonthlyToleranceType == MonthlyToleranceType.CalcMonthlyTolearnceOnDed
                    ? deductionLateInTotal + deductionEarlyOutTotal
                    : 0;

        var target = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id && r.DayDate >= monthStart && r.DayDate <= monthEnd)
            .OrderByDescending(r => r.DedAfterMonthlyTol)
            .FirstOrDefault();

        if (target == null)
        {
            return;
        }

        double dedAfterMonthlyTol = 0;
        if (lateInEarlyOutAllowanceTotal > monthlyAfterAllowanceTotal)
        {
            dedAfterMonthlyTol = Math.Round(lateInEarlyOutAllowanceTotal - monthlyAfterAllowanceTotal, 3);
        }

        target.DedAfterMonthlyTol = dedAfterMonthlyTol;

        await _timeRegisterRepository.UpdateAsync(target);
    }

    #endregion

    #region << Full-month annual-leave adjustment (legacy Program.CalculateAnnualLeaveAdjust) >>

    /// <summary>
    /// Ported from legacy <c>Program.CalculateAnnualLeaveAdjust</c> (legacy <c>Program.cs</c> lines
    /// 2398-2433). Runs once per calendar month, only when
    /// <see cref="TimeEngineSettings.FullMonthLeaveAdjForFixedDays"/> is on (legacy line 1641).
    ///
    /// <para>When ONE annual-leave <see cref="EmployeeAbsence"/> spans the entire calendar month, the month is
    /// paid as a fixed 26 working days rather than its calendar length. The shortfall
    /// <c>26 - annualLeaveDays - publicHolidays</c> is stamped onto <see cref="TimeRegister.LeaveAdjDay"/> of
    /// the month's FIRST annual-leave day.</para>
    ///
    /// <para><b>Reconciliation with <c>IEmployeeAbsenceCalculationService.ActualLeaveDaysAsync</c></b> (as
    /// instructed): the two share only the literal 26 and the same enabling setting -- they are NOT the same
    /// calculation and neither can delegate to the other. <c>ActualLeaveDaysAsync</c> answers "how many
    /// non-working days fall inside an absence span" from duty/holiday CALENDARS
    /// (<c>daysInMonth - 26 + publicHoliday/restDay</c>), and is used to size the absence itself.
    /// <c>CalculateAnnualLeaveAdjust</c> answers "by how much does this already-calculated month's
    /// TimeRegister fall short of 26" from the POSTED <c>TimeRegister.AnnualLeaveDay</c>/<c>PublicHoliday</c>
    /// columns, after the day loop has run. Sharing code would mean recomputing calendars the engine has
    /// already resolved per-day, and would change the result whenever the two disagree. Ported standalone,
    /// verbatim.</para>
    ///
    /// <para>PORT NOTE: the bare literal <c>26</c> is legacy's; despite the "FixedDays" name there is no
    /// sub-group fixed-days field consulted -- <c>EmployeeSubGroup</c> has none in either codebase.</para>
    /// </summary>
    private async Task CalculateAnnualLeaveAdjustAsync(
        Employee employee,
        TimeEngineEmployeeContext context,
        TimeEngineMasterData masterData,
        DateTime monthStart,
        DateTime monthEnd)
    {
        // Legacy: a single annual-leave absence must cover the WHOLE month (StartDate <= monthStart AND
        // EndDate >= monthEnd) -- a mid-month leave gets no adjustment.
        var empAbsence = context.Absences.FirstOrDefault(
            r => r.StartDate <= monthStart
                 && r.EndDate >= monthEnd
                 && r.AbsenceTypeId == masterData.AnnualLeaveTypeId);

        if (empAbsence == null)
        {
            return;
        }

        var monthRegisters = _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id && r.DayDate >= monthStart && r.DayDate <= monthEnd)
            .ToList();

        var target = monthRegisters.FirstOrDefault(r => r.AnnualLeaveDay == 1);
        if (target == null)
        {
            return;
        }

        var totalAnnualLeaveDays = monthRegisters.Where(r => r.AnnualLeaveDay == 1).Sum(r => r.AnnualLeaveDay);
        var totalPublicHolidayDays = monthRegisters.Where(r => r.PublicHoliday == 1).Sum(r => r.PublicHoliday);

        target.LeaveAdjDay = FullMonthFixedWorkingDays - totalAnnualLeaveDays - totalPublicHolidayDays;

        await _timeRegisterRepository.UpdateAsync(target);
    }

    /// <summary>
    /// Legacy's hard-coded full-month fixed working-day count (legacy <c>Program.cs</c> line 2422, and the
    /// same literal in <c>EmployeeAbsenceCalculationService.ActualLeaveDaysAsync</c>'s
    /// <c>FullMonthLeaveAdjForFixedDays</c> branch). Named rather than repeated as a bare literal.
    /// </summary>
    private const double FullMonthFixedWorkingDays = 26;

    #endregion
}
