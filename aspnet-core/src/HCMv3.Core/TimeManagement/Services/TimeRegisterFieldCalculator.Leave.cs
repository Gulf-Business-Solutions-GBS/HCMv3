using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Legacy <c>GeneralTimeService</c> regions "Leave Calculation" (lines 90-374), "AccrualPerDay Calculation"
/// (lines 376-443) and "AirTicket Calculation" (lines 445-462).
/// </summary>
public partial class TimeRegisterFieldCalculator
{
    #region << Leave Calculation >>

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetAnnualPaidLeaveDay</c> (legacy lines 92-137).
    ///
    /// <para>Legacy loops <c>Program.AnnualLeaveList</c> and breaks on the FIRST type that produced a non-zero
    /// raw day count -- AFTER that iteration has possibly zeroed the count again for an
    /// Include(PublicHoliday|Weekend|RestDay) mismatch, and after it has possibly cleared the corresponding
    /// TimeRegister flag. Both the "compute then maybe zero then break anyway" ordering and the fact that the
    /// method returns the LAST iteration's value are preserved verbatim -- they are payroll-visible.</para>
    /// </summary>
    public Task<double> GetAnnualPaidLeaveDayAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        double dayCount = 0;

        // Legacy guard: `if (absenceType != null)` on the PASSED-IN AnnualLeave type (MasterData.AnnualLeave),
        // even though the loop below immediately reassigns the local to each AnnualLeaveList entry.
        if (context.MasterData?.AnnualLeave != null)
        {
            var annualLeaveTypes = context.MasterData.AnnualLeaveTypes ?? new System.Collections.Generic.List<AbsenceType>();

            foreach (var annualLeave in annualLeaveTypes)
            {
                var absenceType = annualLeave;
                dayCount = GetTotalLeaveDayCount(context, absenceType);

                var foundLeave = dayCount != 0;

                if (dayCount != 0
                    && ((absenceType.IncludePublicHoliday == false && timeRegister.PublicHoliday == 1)
                        || (absenceType.IncludeWeekend == false && timeRegister.WeeklyOff == 1)
                        || (absenceType.IncludeRestDay == false && timeRegister.RestDay == 1)))
                {
                    dayCount = 0;
                }

                if (dayCount != 0)
                {
                    if (absenceType.IncludePublicHoliday)
                    {
                        timeRegister.PublicHoliday = 0;
                    }

                    if (absenceType.IncludeWeekend)
                    {
                        timeRegister.WeeklyOff = 0;
                    }

                    if (absenceType.IncludeRestDay)
                    {
                        timeRegister.RestDay = 0;
                    }
                }

                if (foundLeave)
                {
                    break;
                }
            }
        }

        return Task.FromResult(dayCount);
    }

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetUnPaidLeaveDay</c> (legacy lines 139-174). Note the flag-clearing block
    /// is gated on <c>daycount == 1</c> here (not <c>!= 0</c> as in GetAnnualPaidLeaveDay) and sits OUTSIDE the
    /// <c>absenceType != null</c> guard in legacy -- both preserved.
    /// </summary>
    public Task<double> GetUnPaidLeaveDayAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var absenceType = context.MasterData?.UnpaidLeave;
        double dayCount = 0;

        if (absenceType != null)
        {
            dayCount = GetTotalLeaveDayCount(context, absenceType);

            if (dayCount != 0
                && ((absenceType.IncludePublicHoliday == false && timeRegister.PublicHoliday == 1)
                    || (absenceType.IncludeWeekend == false && timeRegister.WeeklyOff == 1)
                    || (absenceType.IncludeRestDay == false && timeRegister.RestDay == 1)))
            {
                dayCount = 0;
            }
        }

        if (dayCount == 1)
        {
            // Legacy dereferences absenceType here unguarded; dayCount can only be 1 when it was non-null.
            if (absenceType.IncludePublicHoliday)
            {
                timeRegister.PublicHoliday = 0;
            }

            if (absenceType.IncludeWeekend)
            {
                timeRegister.WeeklyOff = 0;
            }

            if (absenceType.IncludeRestDay)
            {
                timeRegister.RestDay = 0;
            }
        }

        return Task.FromResult(dayCount);
    }

    /// <summary>Legacy <c>GeneralTimeService.GetSickLeavePaidDay</c> (legacy lines 176-200). Unlike the annual
    /// and unpaid variants this one never clears the PublicHoliday/WeeklyOff/RestDay flags.</summary>
    public Task<double> GetSickLeavePaidDayAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(GetSickLeaveDayCount(context, context.MasterData?.SickLeavePaid));
    }

    /// <summary>Legacy <c>GeneralTimeService.GetSickLeaveUnPaidDay</c> (legacy lines 202-226). Body is
    /// identical to <see cref="GetSickLeavePaidDayAsync"/> in legacy; shared here.</summary>
    public Task<double> GetSickLeaveUnPaidDayAsync(TimeRegisterDayContext context)
    {
        return Task.FromResult(GetSickLeaveDayCount(context, context.MasterData?.SickLeaveUnpaid));
    }

    private double GetSickLeaveDayCount(TimeRegisterDayContext context, AbsenceType absenceType)
    {
        var timeRegister = context.TimeRegister;
        double dayCount = 0;

        if (absenceType != null)
        {
            dayCount = GetTotalLeaveDayCount(context, absenceType);

            if (dayCount != 0
                && ((absenceType.IncludePublicHoliday == false && timeRegister.PublicHoliday == 1)
                    || (absenceType.IncludeWeekend == false && timeRegister.WeeklyOff == 1)
                    || (absenceType.IncludeRestDay == false && timeRegister.RestDay == 1)))
            {
                dayCount = 0;
            }
        }

        return dayCount;
    }

    /// <summary>Legacy <c>GeneralTimeService.GetLeaveType</c> (legacy lines 228-234).</summary>
    public Task<int?> GetLeaveTypeIdAsync(TimeRegisterDayContext context)
    {
        var dayDate = context.TimeRegister.DayDate;

        var absenceTypeId = AbsencesOf(context)
            .Where(r => r.StartDate <= dayDate && r.EndDate >= dayDate)
            .Select(r => r.AbsenceTypeId)
            .FirstOrDefault();

        // Legacy wraps this in Program.GetAbsenceType(oid), which returns null when the oid is not in the
        // pre-loaded absenceTypeList (including oid 0 = "no absence today").
        var absenceType = ResolveAbsenceType(context, absenceTypeId);

        return Task.FromResult(absenceType?.Id);
    }

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetAnnualLeaveType</c> (legacy lines 236-242). NOT called by the
    /// orchestrator. Legacy's body is byte-for-byte identical to <c>GetLeaveType</c>, so it delegates.
    /// </summary>
    public Task<int?> GetAnnualLeaveTypeIdAsync(TimeRegisterDayContext context)
    {
        return GetLeaveTypeIdAsync(context);
    }

    /// <summary>Legacy <c>GeneralTimeService.GetLeaveDay</c> (legacy lines 244-274).</summary>
    public Task<double> GetLeaveDayAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var leaveType = LeaveTypeOf(context);

        double dayCount = 0;

        if (leaveType != null)
        {
            dayCount = (timeRegister.AnnualLeaveDay != 0) ? timeRegister.AnnualLeaveDay
                : (timeRegister.UnpaidLeaveDay != 0) ? timeRegister.UnpaidLeaveDay
                : (timeRegister.SickLeavePaidDay != 0) ? timeRegister.SickLeavePaidDay
                : (timeRegister.SickLeaveUnpaidDay != 0) ? timeRegister.SickLeaveUnpaidDay
                : GetTotalLeaveDayCount(context, leaveType);

            if (dayCount != 0
                && ((leaveType.IncludePublicHoliday == false && timeRegister.PublicHoliday == 1)
                    || (leaveType.IncludeWeekend == false && timeRegister.WeeklyOff == 1)
                    || (leaveType.IncludeRestDay == false && timeRegister.RestDay == 1)))
            {
                dayCount = 0;
            }

            if (dayCount == 1)
            {
                if (leaveType.IncludePublicHoliday)
                {
                    timeRegister.PublicHoliday = 0;
                }

                if (leaveType.IncludeWeekend)
                {
                    timeRegister.WeeklyOff = 0;
                }

                if (leaveType.IncludeRestDay)
                {
                    timeRegister.RestDay = 0;
                }
            }
        }

        return Task.FromResult(dayCount);
    }

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetLeaveMinutes</c> (legacy lines 276-314). Note the hourly branch uses
    /// <c>EndTime.TimeOfDay - StartTime.TimeOfDay</c> (time-of-day only, unlike
    /// <c>GetActualAbsenceMinutes</c>'s full-DateTime subtraction) -- preserved.
    /// </summary>
    public Task<double> GetLeaveMinutesAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        double leaveMinutes = 0;

        if (LeaveTypeOf(context) != null)
        {
            var dayDate = timeRegister.DayDate;
            var empAbsence = AbsencesOf(context)
                .Where(r => r.StartDate <= dayDate
                            && r.EndDate >= dayDate
                            && ResolveAbsenceType(context, r) != null)
                .ToList();

            // Check if any Fullday leave then consider it first.
            if (empAbsence.Any(r => r.AbsenceHours == 0))
            {
                leaveMinutes = timeRegister.PlannedWorkingMins;
            }
            else
            {
                var hourlyAbsences = empAbsence
                    .Where(r => r.AbsenceHours != 0)
                    .OrderBy(r => r.StartTime)
                    .ToList();

                foreach (var rec in hourlyAbsences)
                {
                    var absenceType = ResolveAbsenceType(context, rec);
                    var hourlyLeaveMinutes = rec.EndTime.TimeOfDay.Subtract(rec.StartTime.TimeOfDay).TotalMinutes;

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

                    leaveMinutes = leaveMinutes + hourlyLeaveMinutes;
                }
            }
        }

        return Task.FromResult(leaveMinutes);
    }

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetTotalLeaveDayCount</c> (legacy lines 316-353). Not called by the
    /// orchestrator directly, but it is the workhorse behind every leave-day method above.
    ///
    /// <para><b>Note on exact-precision leave maths:</b> the hourly branch sums the STORED
    /// <see cref="PersonnelAdministration.EmployeeAbsence.TotalLeaveDays"/> column -- it does not re-derive the
    /// fraction. That is exactly what legacy did, and it is also what keeps this consistent with
    /// <c>IEmployeeAbsenceCalculationService</c>, which is the service that computes and persists that column
    /// in the first place. No re-derivation, no approximation.</para>
    /// </summary>
    public Task<double> GetTotalLeaveDayCountAsync(TimeRegisterDayContext context)
    {
        // Legacy signature took the AbsenceType explicitly; the interface's single-parameter form has no way
        // to pass it, so the day's resolved LeaveType is used (the same value legacy's only "loose" call site,
        // GetLeaveDay, passes). Callers inside this class use the private overload below.
        var leaveType = LeaveTypeOf(context);
        return Task.FromResult(leaveType == null ? 0 : GetTotalLeaveDayCount(context, leaveType));
    }

    private double GetTotalLeaveDayCount(TimeRegisterDayContext context, AbsenceType absenceType)
    {
        double count = 0;

        var dayDate = context.TimeRegister.DayDate;
        var absenceTypeId = absenceType.Id;

        var recAbsenceList = AbsencesOf(context)
            .Where(r => r.AbsenceTypeId == absenceTypeId
                        && r.StartDate <= dayDate
                        && r.EndDate >= dayDate)
            .ToList();

        if (recAbsenceList.Any())
        {
            // For FullDay Leave.
            if (recAbsenceList.Any(r => r.AbsenceHours == 0))
            {
                count = 1;
            }
            else
            {
                // For Hourly Basis Leave.
                count = recAbsenceList.Where(r => r.AbsenceHours != 0).Sum(r => r.TotalLeaveDays);
            }
        }

        return count;
    }

    /// <summary>Legacy <c>GeneralTimeService.GetIncludeInLeaveDay</c> (legacy lines 355-372), assigned to
    /// <see cref="TimeRegister.IncludeInLeavePay"/>.</summary>
    public Task<double> GetIncludeInLeaveDayAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var leaveType = LeaveTypeOf(context);

        double dayCount = 0;

        if (leaveType != null && leaveType.IsPaid && timeRegister.LeaveDay == 0)
        {
            var flagsMatch = (leaveType.IncludeRestDay == false && timeRegister.RestDay == 1)
                             || (leaveType.IncludeWeekend == false && timeRegister.WeeklyOff == 1);

            if (flagsMatch)
            {
                // Legacy: timeRegister.LeaveType.WageType != null
                //         && timeRegister.LeaveType.WageType.ProrateMethod == ProrateMethod.ActiveDays
                var wageType = ResolveWageType(context, leaveType.WageTypeId);
                if (wageType != null && wageType.ProrateMethod == ProrateMethod.ActiveDays)
                {
                    dayCount = 1;
                }
            }
        }

        return Task.FromResult(dayCount);
    }

    #endregion

    #region << AccrualPerDay Calculation >>

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetAnnualLeaveAccrualPerDay</c> (legacy lines 378-414). The legacy
    /// <c>TotalDays</c> parameter is <c>context.AnnualLeaveSubGroupAbsence?.TotalDays ?? 0</c> and
    /// <c>empTimeTransfer</c> is <see cref="TimeEngineEmployeeContext.TimeTransfers"/>.
    /// </summary>
    public Task<double> GetAnnualLeaveAccrualPerDayAsync(TimeRegisterDayContext context)
    {
        var timeRegister = context.TimeRegister;
        var employee = EmployeeOf(context);

        if (employee == null)
        {
            throw new InvalidOperationException("Employee is required to calculate AnnualLeaveAccrualPerDay.");
        }

        var annualLeave = context.MasterData?.AnnualLeave;
        if (annualLeave == null)
        {
            // Legacy dereferenced Program.AnnualLeave unguarded (NullReferenceException). Made explicit.
            throw new InvalidOperationException(
                "Annual Leave AbsenceType (Code = \"1\") not found; cannot calculate AnnualLeaveAccrualPerDay.");
        }

        var totalDays = context.AnnualLeaveSubGroupAbsence?.TotalDays ?? 0;
        var joiningDate = employee.JoiningDate;
        var year = timeRegister.DayDate.Year;

        // Legacy leap-day guard: a 29-Feb joiner in a non-leap year anchors on 28 Feb.
        var start = (DateTime.IsLeapYear(year) == false && joiningDate.Month == 2 && joiningDate.Day == 29)
            ? new DateTime(year, joiningDate.Month, joiningDate.AddDays(-1).Day)
            : new DateTime(year, joiningDate.Month, joiningDate.Day);

        var accrualDivFactor = annualLeave.AccrualPeriod == AccrualPeriod.CalendarYear
            ? (DateTime.IsLeapYear(year) ? 366 : 365)
            : annualLeave.AccrualPeriod == AccrualPeriod.WorkYear
                ? ((timeRegister.DayDate >= start)
                    ? start.AddYears(1).Subtract(start).Days
                    : start.Subtract(start.AddYears(-1)).Days)
                : 365;

        var timeTransfers = context.EmployeeContext?.TimeTransfers
                            ?? new System.Collections.Generic.List<TimeTransfer>();
        var dayDate = timeRegister.DayDate;
        var transferCount = timeTransfers.Where(r => r.TransferDate == dayDate).Sum(r => r.TransferCount);

        // (12.09.2020) Maisam: Check if LeaveType has ExcludePaidLeaveAccrual flag true or
        // Exclude_Paidleaveaccrual_On_Absent is true, then dont calculate perday accrual.
        // Client Reference: Mashaan error reported, annual leave accrual was happening on Absent day also.
        var leaveType = LeaveTypeOf(context);
        var excluded = (leaveType != null && leaveType.ExcludePaidLeaveAccrual)
                       || (timeRegister.AbsentDay == 1
                           && context.Settings != null
                           && context.Settings.ExcludePaidLeaveAccrualOnAbsent);

        var result = excluded
            ? transferCount
            : (((double)totalDays) / accrualDivFactor + transferCount);

        return Task.FromResult(Math.Round(result, 6));
    }

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetTicketAccrualPerDay</c> (legacy lines 416-441). NOT called by the
    /// orchestrator -- the only legacy call site is inside a commented-out block. The legacy
    /// <c>empSubGroup</c> parameter is <see cref="TimeEngineEmployeeContext.EmployeeSubGroup"/>, and
    /// <c>Program.empSubGroupTicketList</c> becomes an <c>IRepository&lt;EmployeeSubGroupTicket&gt;</c> query.
    /// </summary>
    public Task<double> GetTicketAccrualPerDayAsync(TimeRegisterDayContext context)
    {
        double ticketAccrual = 0;

        var employeeSubGroup = context.EmployeeContext?.EmployeeSubGroup;
        var airTicketTypeId = context.MasterData?.AirTicketTypeId ?? 0;

        if (employeeSubGroup != null && airTicketTypeId != 0)
        {
            var dayDate = context.TimeRegister.DayDate;
            var subGroupId = employeeSubGroup.Id;

            var quota = _employeeSubGroupTicketRepository.GetAll()
                .Where(r => r.EmployeeSubGroupId == subGroupId
                            && r.TicketTypeId == airTicketTypeId
                            && r.StartDate <= dayDate
                            && r.EndDate >= dayDate)
                .FirstOrDefault();

            if (quota != null)
            {
                // Legacy walked quota.TicketType.QuotaEntitlement; resolved by id here (see class remarks).
                var ticketType = context.MasterData?.TicketTypes?.FirstOrDefault(r => r.Id == airTicketTypeId);

                if (ticketType != null && ticketType.QuotaEntitlement != QuotaEntitlement.Immediate)
                {
                    ticketAccrual = (quota.AccrualPeriod == AccrualPeriod.BiAnnual)
                        ? ((double)quota.TicketCount) / 730
                        : ((double)quota.TicketCount) / 365;
                }
            }
        }

        return Task.FromResult(Math.Round(ticketAccrual, 3));
    }

    #endregion

    #region << AirTicket Calculation >>

    /// <summary>
    /// Legacy <c>GeneralTimeService.GetAirTicketPaidDay</c> (legacy lines 448-460). NOT called by the
    /// orchestrator (commented-out legacy call site). Legacy comment preserved: "22.02.2018 Mustafa: to get
    /// ticket paid day. ticket type hard coding has to be removed." -- the "1"/"2"/"3"/"4" TicketType codes
    /// are legacy's, not this port's.
    /// </summary>
    public Task<double> GetAirTicketPaidDayAsync(TimeRegisterDayContext context)
    {
        var dayDate = context.TimeRegister.DayDate;
        var tickets = context.EmployeeContext?.Tickets
                      ?? new System.Collections.Generic.List<PersonnelAdministration.EmployeeTicket>();

        var airTicketTypeId = context.MasterData?.AirTicketTypeId ?? 0;
        var ticketTypes = context.MasterData?.TicketTypes ?? new System.Collections.Generic.List<TicketType>();

        double CompensateForTypeId(int ticketTypeId)
        {
            return ticketTypeId == 0
                ? 0
                : tickets.Where(r => r.TicketTypeId == ticketTypeId && r.PaymentDate == dayDate)
                    .Select(r => r.Compensate)
                    .FirstOrDefault();
        }

        double CompensateForCode(string code)
        {
            var ticketType = ticketTypes.FirstOrDefault(r => r.Code == code);
            return ticketType == null ? 0 : CompensateForTypeId(ticketType.Id);
        }

        var result = CompensateForTypeId(airTicketTypeId)
                     + CompensateForCode("2")
                     + CompensateForCode("3")
                     + CompensateForCode("4");

        return Task.FromResult(result);
    }

    #endregion
}
