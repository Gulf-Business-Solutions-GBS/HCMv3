using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Legacy <c>GeneralTimeService</c> region "Attendance Calculation" (lines 464-618).
/// </summary>
public partial class TimeRegisterFieldCalculator
{
    #region << Attendance Calculation >>

    /// <summary>
    /// Legacy <c>GeneralTimeService.CalculateAutoAttendance</c> (legacy lines 466-527). Side-effecting: it
    /// creates or updates the day's <see cref="EmployeeAttendance"/> row and mutates the pre-loaded
    /// <see cref="TimeEngineEmployeeContext.Attendances"/> list, exactly as legacy did.
    ///
    /// <para><b>PlannedTime branch.</b> Legacy re-derives ActualIn/ActualOut through
    /// <c>ActualTimeService</c> (NOT the employee's own PlannedTime service) using a FRESH TimeEvent query
    /// windowed to DayDate±1 and a deliberately EMPTY absence list (<c>empAbsenceListTemp</c>). This port
    /// reproduces that by resolving the <see cref="TimeMgmtStatus.ActualTime"/> punch strategy from
    /// <see cref="IPunchResolutionStrategyResolver"/> and handing it a shallow-copied context whose
    /// <see cref="TimeEngineEmployeeContext.TimeEvents"/> is that window and whose
    /// <see cref="TimeEngineEmployeeContext.Absences"/> is empty. The legacy <c>empAnnualLeaveQuota</c>
    /// parameter is accepted but unused by the legacy body -- nothing to port.</para>
    /// </summary>
    public async Task CalculateAutoAttendanceAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        if (timeRegister.PublicHoliday == 0)
        {
            return;
        }

        var dayDate = timeRegister.DayDate;
        var attendances = AttendancesOf(context);
        var empAttendance = attendances.FirstOrDefault(r => r.StartDate == dayDate);

        var actualIn = timeRegister.ActualIn;
        var actualOut = timeRegister.ActualOut;

        var timePolicy = TimePolicyOf(context);
        if (timePolicy != null && timePolicy.TimeMgmtStatus == TimeMgmtStatus.PlannedTime)
        {
            var actualTimeContext = BuildActualTimeProbeContext(context);
            var actualTimeStrategy = _punchStrategyResolver.Resolve(TimeMgmtStatus.ActualTime);

            actualIn = await actualTimeStrategy.GetActualInAsync(actualTimeContext);
            actualOut = await actualTimeStrategy.GetActualOutAsync(actualTimeContext);
        }

        // Check if PunchIn and PunchOut is there or not (legacy: != DateTimeExtension.DBDtNull).
        if (actualIn == default || actualOut == default)
        {
            return;
        }

        var extraWorkingAttendanceType = context.MasterData?.ExtraWorkingAttendanceType;
        if (extraWorkingAttendanceType == null)
        {
            // Legacy assigned objectSpace.GetObject(Program.extraWorkingAttType) unguarded; a null there
            // produced an unsaveable row. Made explicit rather than writing a 0 FK.
            throw new InvalidOperationException(
                "Extra-working AttendanceType (Code = \"1\") not found; cannot auto-create attendance on a public holiday.");
        }

        var isNew = empAttendance == null;
        if (isNew)
        {
            empAttendance = new EmployeeAttendance
            {
                TenantId = EmployeeOf(context)?.TenantId ?? timeRegister.TenantId,
                EmployeeId = timeRegister.EmployeeId
            };
        }

        empAttendance.AttendanceTypeId = extraWorkingAttendanceType.Id;
        empAttendance.AttendanceType = extraWorkingAttendanceType;
        empAttendance.StartDate = actualIn.Date;
        empAttendance.EndDate = actualIn.Date; // legacy comment: //ActualOut.Date;
        empAttendance.StartTime = TimeOnly(actualIn);
        empAttendance.EndTime = TimeOnly(actualOut);

        if (isNew)
        {
            await _employeeAttendanceRepository.InsertAsync(empAttendance);
        }
        else
        {
            await _employeeAttendanceRepository.UpdateAsync(empAttendance);
        }

        // Legacy: empAttendanceList.Remove(empAttendance); empAttendanceList.Add(empAttendance);
        attendances.Remove(empAttendance);
        attendances.Add(empAttendance);
    }

    /// <summary>
    /// Builds the throwaway context legacy's <c>CalculateAutoAttendance</c> PlannedTime branch used: same day,
    /// same TimeRegister, but with a DayDate±1 TimeEvent window and an EMPTY absence list.
    /// </summary>
    private TimeRegisterDayContext BuildActualTimeProbeContext(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var employeeId = timeRegister.EmployeeId;
        var from = timeRegister.DayDate.AddDays(-1);
        var to = timeRegister.DayDate.AddDays(1);

        var transTypeCodes = _transTypeRepository.GetAll().ToList().ToDictionary(r => r.Id, r => r.Code);

        var timeEvents = _timeEventRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.TransDate >= from && r.TransDate <= to)
            .ToList()
            .Select(r => new TimePunchEvent
            {
                EmployeeId = r.EmployeeId,
                TransTypeId = r.TransTypeId,
                TransTypeCode = transTypeCodes.ContainsKey(r.TransTypeId) ? transTypeCodes[r.TransTypeId] : null,
                TransDate = r.TransDate,
                TransTime = r.TransTime,
                TransDateTime = r.TransDate.Date
                    .AddHours(r.TransTime.Hour).AddMinutes(r.TransTime.Minute).AddSeconds(r.TransTime.Second)
            })
            .ToList();

        var source = context.EmployeeContext;
        var probeEmployeeContext = new TimeEngineEmployeeContext
        {
            Employee = source?.Employee,
            StartDate = source?.StartDate ?? timeRegister.DayDate,
            EndDate = source?.EndDate ?? timeRegister.DayDate,
            PayrollStatus = source?.PayrollStatus,
            EmployeeSubGroup = source?.EmployeeSubGroup,
            CutoffDate = source?.CutoffDate ?? timeRegister.DayDate,
            TimeRegisters = source?.TimeRegisters ?? new List<TimeRegister>(),
            Contracts = source?.Contracts ?? new List<EmployeeContract>(),
            OrgAssignments = source?.OrgAssignments ?? new List<EmployeeOrgAssignment>(),
            DutyPeriods = source?.DutyPeriods ?? new List<EmployeeDutyPeriod>(),
            TimePolicies = source?.TimePolicies ?? new List<EmployeeTimePolicy>(),
            TimeEvents = timeEvents,
            SubstituteDuties = source?.SubstituteDuties ?? new List<EmployeeSubstituteDuty>(),
            TimeTransfers = source?.TimeTransfers ?? new List<TimeTransfer>(),

            // Legacy `List<EmployeeAbsence> empAbsenceListTemp = new List<EmployeeAbsence>();` -- deliberately
            // empty, so the ActualTime probe ignores the employee's leave for this one calculation.
            Absences = new List<EmployeeAbsence>(),

            NotReturnedAbsence = source?.NotReturnedAbsence,
            Attendances = source?.Attendances ?? new List<EmployeeAttendance>(),
            Overtimes = source?.Overtimes ?? new List<EmployeeOvertime>(),
            Tickets = source?.Tickets ?? new List<EmployeeTicket>(),
            AbsAdjustments = source?.AbsAdjustments ?? new List<EmployeeAbsAdjustment>(),
            AnnualLeaveQuotas = source?.AnnualLeaveQuotas ?? new List<EmployeeAbsenceQuota>(),
            OtherLeaveQuotas = source?.OtherLeaveQuotas ?? new List<EmployeeAbsenceQuota>(),
            TicketQuotas = source?.TicketQuotas ?? new List<EmployeeTicketQuota>(),
            GratuityQuotas = source?.GratuityQuotas ?? new List<EmployeeGratuityQuota>(),
            BasicPays = source?.BasicPays ?? new List<EmployeeBasicPay>(),
            AnnualLeaveQuotaCreated = source?.AnnualLeaveQuotaCreated ?? false
        };

        return new TimeRegisterDayContext
        {
            TimeRegister = timeRegister,
            DayDate = context.DayDate,
            Employee = context.Employee,
            Duty = context.Duty,
            DutyPeriod = context.DutyPeriod,
            TimePolicy = context.TimePolicy,
            IsNotReturnFromLeave = context.IsNotReturnFromLeave,
            IsFutureDate = context.IsFutureDate,
            IsNewTimeRegisterRecord = context.IsNewTimeRegisterRecord,
            AnnualLeaveSubGroupAbsence = context.AnnualLeaveSubGroupAbsence,
            AnnualLeaveQuota = context.AnnualLeaveQuota,
            PreviousValues = context.PreviousValues,
            EmployeeContext = probeEmployeeContext,
            Settings = context.Settings,
            MasterData = context.MasterData
        };
    }

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetAttendanceDay</c> (legacy lines 528-578). The IncludeWeekend /
    /// IncludeRestDay checks are commented out in the legacy source (twice over) -- only IncludePublicHoliday
    /// is live. Preserved as-is.
    /// </summary>
    public Task<double> GetAttendanceDayAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        double count = 0;

        if (timeRegister.PublicHoliday == 1 || timeRegister.WeeklyOff == 1 || timeRegister.RestDay == 1)
        {
            var dayDate = timeRegister.DayDate;
            var attendance = AttendancesOf(context)
                .FirstOrDefault(r => r.StartDate <= dayDate && r.EndDate >= dayDate);
            var attendanceType = ResolveAttendanceType(attendance);

            if (attendanceType != null)
            {
                count = 1;

                if (attendanceType.IncludePublicHoliday == false && timeRegister.PublicHoliday == 1)
                {
                    count = 0;
                }

                if (count == 1 && attendanceType.IncludePublicHoliday)
                {
                    timeRegister.PublicHoliday = 0;
                }
            }
        }

        return Task.FromResult(count);
    }

    /// <summary>Legacy <c>GeneralTimeService.GetAttendanceType</c> (legacy lines 579-587).</summary>
    public Task<int?> GetAttendanceTypeIdAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;

        if (timeRegister.AttendanceDay == 0)
        {
            return Task.FromResult<int?>(null);
        }

        var dayDate = timeRegister.DayDate;
        var attendance = AttendancesOf(context)
            .FirstOrDefault(r => r.StartDate <= dayDate && r.EndDate >= dayDate);

        return Task.FromResult(attendance == null ? (int?)null : attendance.AttendanceTypeId);
    }

    /// <summary>Legacy <c>GeneralTimeService.GetAttendanceMinutes</c> (legacy lines 588-616).</summary>
    public Task<double> GetAttendanceMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var attendanceType = AttendanceTypeOf(context);

        double attendanceMinutes = 0;

        if (attendanceType != null)
        {
            var dayDate = timeRegister.DayDate;
            var rec = AttendancesOf(context)
                .FirstOrDefault(r => r.StartDate <= dayDate && r.EndDate >= dayDate);

            if (attendanceType.IsFullDay)
            {
                attendanceMinutes = timeRegister.PlannedWorkingMins;
            }
            else
            {
                // Legacy dereferenced `rec` unguarded here; rec cannot be null when AttendanceType is set,
                // because AttendanceTypeId is only assigned from that very row.
                attendanceMinutes = rec.EndTime.Subtract(rec.StartTime).TotalMinutes;
            }

            if (attendanceType.IncludePublicHoliday == false && timeRegister.PublicHoliday == 1)
            {
                attendanceMinutes = 0;
            }

            if (attendanceType.IncludeWeekend == false && timeRegister.WeeklyOff == 1)
            {
                attendanceMinutes = 0;
            }

            if (attendanceType.IncludeRestDay == false && timeRegister.RestDay == 1)
            {
                attendanceMinutes = 0;
            }
        }

        return Task.FromResult(attendanceMinutes);
    }

    #endregion
}
