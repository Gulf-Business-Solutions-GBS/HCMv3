using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Per-day half of the Time Engine orchestration: builds one <see cref="TimeRegister"/> row, in the exact
/// legacy field order, delegating every computed value to <see cref="ITimeRegisterFieldCalculator"/> (T2) and
/// <see cref="IPunchResolutionStrategy"/> (T3). See <see cref="TimeEngineService"/> for the deviation list.
/// </summary>
public partial class TimeEngineService
{
    private async Task CalculateDayAsync(
        DateTime dayDate,
        TimeEngineEmployeeContext employeeContext,
        TimeEngineSettings settings,
        TimeEngineMasterData masterData,
        int employeeCount)
    {
        var employee = employeeContext.Employee;

        #region << Check if Day By Day Pay time Policy then skip TE entry >>

        // Legacy (2020.09.22): a VariableTime policy means "no TimeRegister row for this day" -- expressed as
        // a throw so the day lands in TimeLog as failed. Preserved, including the message.
        var dayPolicies = ResolveTimePolicies(employeeContext, dayDate);
        if (dayPolicies.Any(p => p.TimeMgmtStatus == TimeMgmtStatus.VariableTime))
        {
            throw new Exception(string.Format("Employee has VariableTime Policy found on {0}", dayDate.ToString("dd/MM/yyyy")));
        }

        #endregion

        #region << Check if Any Not Return From Leave Found >>

        var isNotReturnFromLeave = settings.IsNotReturnFromLeaveLogicEnabled
                                   && employeeContext.NotReturnedAbsence != null
                                   && employeeContext.NotReturnedAbsence.EndDate < dayDate;

        #endregion

        #region << Create Or Fetch TimeRegister Object >>

        // Legacy (22.04.2018): "future date" == strictly after the cutoff date.
        var isFutureDate = dayDate > employeeContext.CutoffDate;

        var isNewTimeRegRecord = false;
        var timeRegister = employeeContext.TimeRegisters.FirstOrDefault(x => x.DayDate == dayDate);
        if (timeRegister == null)
        {
            timeRegister = new TimeRegister
            {
                EmployeeId = employee.Id,
                DayDate = dayDate.Date
            };
            isNewTimeRegRecord = true;
            employeeContext.TimeRegisters.Add(timeRegister);
        }

        var dutyPeriod = await GetEmployeeDutyPeriodAsync(employeeContext, dayDate, settings);
        var duty = await GetEmployeeDutyAsync(employeeContext, dayDate, dutyPeriod, settings);
        var timePolicy = GetEmployeeTimePolicy(employeeContext, dayDate);

        timeRegister.DutyPeriodId = dutyPeriod.Id;
        timeRegister.DutyPeriod = dutyPeriod;
        timeRegister.DutyId = duty.Id;
        timeRegister.Duty = duty;
        timeRegister.TimePolicyId = timePolicy.Id;
        timeRegister.TimePolicy = timePolicy;

        #endregion

        #region << Check OrgAssignment and SubGroup >>

        var orgAssignment = employeeContext.OrgAssignments
            .FirstOrDefault(r => r.StartDate <= dayDate && r.EndDate >= timeRegister.DayDate);
        if (orgAssignment == null)
        {
            throw new Exception(string.Format("Employee Org Assignment not found on {0}", timeRegister.DayDate.ToShortDateString()));
        }

        // See TimeEngineRunInput.EmployeeSubGroupIds: caller-supplied, because EmployeeOrgAssignment here has
        // no EmployeeSubGroupId FK. The legacy error message is preserved verbatim.
        var employeeSubGroup = employeeContext.EmployeeSubGroup;
        if (employeeSubGroup == null)
        {
            throw new Exception("Employee Sub Group not found for Employee.");
        }

        var annualLeaveSubGroupAbsence = GetEmployeeSubGroupLeave(
            masterData, masterData.AnnualLeaveTypeId, employee, dayDate, employeeSubGroup);

        #endregion

        var context = new TimeRegisterDayContext
        {
            TimeRegister = timeRegister,
            DayDate = dayDate,
            Employee = employee,
            Duty = duty,
            DutyPeriod = dutyPeriod,
            TimePolicy = timePolicy,
            IsNotReturnFromLeave = isNotReturnFromLeave,
            IsFutureDate = isFutureDate,
            IsNewTimeRegisterRecord = isNewTimeRegRecord,
            AnnualLeaveSubGroupAbsence = annualLeaveSubGroupAbsence,
            EmployeeContext = employeeContext,
            Settings = settings,
            MasterData = masterData
        };

        #region << Employee Leave Quota Record + accrual-leave quotas + gratuity quota >>

        // NOTE: this call site originally used `out` parameters, which C# forbids on an async method (CS1988).
        // The two legacy flags are returned on a result object instead; `newLeaveQuota` is per-EMPLOYEE sticky
        // in legacy, so it lives on TimeEngineEmployeeContext -- see AnnualLeaveQuotaCreated's remarks.
        var quotaResult = await EnsureAnnualLeaveQuotaAsync(context, employeeSubGroup);
        var annualLeaveQuota = quotaResult.Quota;
        var leaveQuotaFound = quotaResult.LeaveQuotaFound;
        var newLeaveQuota = employeeContext.AnnualLeaveQuotaCreated;
        context.AnnualLeaveQuota = annualLeaveQuota;

        await EnsureAccrualLeaveQuotasAsync(context, employeeSubGroup);
        await EnsureGratuityQuotaAsync(context, employeeSubGroup);

        // NOT PORTED: legacy's "Employee BasicPay Record" auto-creation block -- see TimeEngineService remarks.

        #endregion

        await ApplyDayFieldsAsync(context);

        #region << AnnualLeaveAccrualPerDay quota roll-up >>

        var oldAnnualLeaveAccrualPerDay = timeRegister.AnnualLeaveAccrualPerDay;
        timeRegister.AnnualLeaveAccrualPerDay = timeRegister.ActiveDay == 1
            ? await _fieldCalculator.GetAnnualLeaveAccrualPerDayAsync(context)
            : 0;

        if (annualLeaveQuota != null)
        {
            var oldQuotaAccrued = annualLeaveQuota.QuotaAccrued;

            annualLeaveQuota.QuotaAccrued = Math.Round(
                annualLeaveQuota.QuotaAccrued
                - ((oldAnnualLeaveAccrualPerDay != 0 && leaveQuotaFound && !newLeaveQuota) ? oldAnnualLeaveAccrualPerDay : 0)
                + timeRegister.AnnualLeaveAccrualPerDay,
                6);

            // Legacy inlined EmployeeAbsenceQuota's own OldQuotaAccured/ActualQuotaAccrued cascade here "to
            // reduce time"; this port keeps the same arithmetic (there is no such entity-level cascade here).
            annualLeaveQuota.ActualQuotaAccrued += annualLeaveQuota.QuotaAccrued - oldQuotaAccrued;

            var quotaBalance = GetQuotaBalance(annualLeaveQuota);
            if (annualLeaveSubGroupAbsence != null
                && annualLeaveSubGroupAbsence.MaxLimit != 0
                && quotaBalance > annualLeaveSubGroupAbsence.MaxLimit
                && settings.ForfeitRule == SystemSettings.ForfeitRule.None)
            {
                annualLeaveQuota.QuotaAccrued -= quotaBalance - annualLeaveSubGroupAbsence.MaxLimit;
            }

            if (settings.AnnualLeaveMaxLimitReachedNotification
                && annualLeaveQuota.NotificationSent
                && annualLeaveSubGroupAbsence != null
                && annualLeaveSubGroupAbsence.MaxLimitReachNotificationInDays != 0
                && GetQuotaBalance(annualLeaveQuota) < annualLeaveSubGroupAbsence.MaxLimitReachNotificationInDays)
            {
                annualLeaveQuota.NotificationSent = false;
            }

            await _employeeAbsenceQuotaRepository.UpdateAsync(annualLeaveQuota);
        }

        #endregion

        #region << Employee Auto AirTicket Pay >>

        if (employeeSubGroup.AutoPayAirTicket)
        {
            await AutoAirTicketPayAsync(context, employeeSubGroup);
        }

        #endregion

        #region << Approved fields >>

        if (settings.EnableTimeRegisterApproval)
        {
            await ApplyApprovalFieldsAsync(context);
        }

        #endregion

        if (isNewTimeRegRecord)
        {
            timeRegister.Id = await _timeRegisterRepository.InsertAndGetIdAsync(timeRegister);
        }
        else
        {
            await _timeRegisterRepository.UpdateAsync(timeRegister);
        }

        await UpdatePayrollStatusAfterDayAsync(context);
    }

    #region << Duty / DutyPeriod / TimePolicy resolution >>

    /// <summary>
    /// Ported from legacy <c>Program.GetEmployeeDutyPeriod</c>. Roster branch delegates to
    /// <see cref="IRosterService"/> rather than re-deriving it.
    /// </summary>
    private async Task<DutyPeriod> GetEmployeeDutyPeriodAsync(
        TimeEngineEmployeeContext employeeContext, DateTime dayDate, TimeEngineSettings settings)
    {
        DutyPeriod result;

        if (!settings.IsRosterActive)
        {
            var employeeDutyPeriod = employeeContext.DutyPeriods
                .FirstOrDefault(r => r.StartDate <= dayDate && r.EndDate >= dayDate);

            result = employeeDutyPeriod == null
                ? null
                : _dutyPeriodRepository.GetAll().FirstOrDefault(r => r.Id == employeeDutyPeriod.DutyPeriodId);
        }
        else
        {
            result = await _rosterService.GetRosterDutyPeriodAsync(employeeContext.Employee.Id, dayDate);
        }

        if (result == null)
        {
            throw new Exception(string.Format("Employee Duty Period not found for {0}", dayDate.ToShortDateString()));
        }

        return result;
    }

    /// <summary>
    /// Ported from legacy <c>Program.GetEmployeeDuty</c>. Weekly branch: substitute duty wins, otherwise the
    /// DutyPeriod's day-of-week duty. Roster branch delegates to <see cref="IRosterService"/>.
    /// </summary>
    private async Task<Duty> GetEmployeeDutyAsync(
        TimeEngineEmployeeContext employeeContext, DateTime dayDate, DutyPeriod dutyPeriod, TimeEngineSettings settings)
    {
        Duty duty = null;

        if (!settings.IsRosterActive)
        {
            // First priority: an EmployeeSubstituteDuty row for this exact day.
            var substituteDutyId = employeeContext.SubstituteDuties
                .Where(r => r.SubstituteDate == dayDate)
                .Select(r => (int?)r.DutyId)
                .FirstOrDefault();

            var dutyId = substituteDutyId ?? GetDayOfWeekDutyId(dutyPeriod, dayDate);

            if (dutyId != null)
            {
                duty = _dutyRepository.GetAll().FirstOrDefault(r => r.Id == dutyId.Value);
            }
        }
        else
        {
            duty = await _rosterService.GetRosterDutyAsync(
                employeeContext.Employee.Id, dayDate, employeeContext.SubstituteDuties);
        }

        if (duty == null)
        {
            throw new Exception("Duty is null.");
        }

        return duty;
    }

    private static int? GetDayOfWeekDutyId(DutyPeriod dutyPeriod, DateTime dayDate)
    {
        switch (dayDate.DayOfWeek)
        {
            case DayOfWeek.Sunday: return dutyPeriod.SundayDutyId;
            case DayOfWeek.Monday: return dutyPeriod.MondayDutyId;
            case DayOfWeek.Tuesday: return dutyPeriod.TuesdayDutyId;
            case DayOfWeek.Wednesday: return dutyPeriod.WednesdayDutyId;
            case DayOfWeek.Thursday: return dutyPeriod.ThursdayDutyId;
            case DayOfWeek.Friday: return dutyPeriod.FridayDutyId;
            default: return dutyPeriod.SaturdayDutyId;
        }
    }

    /// <summary>Ported from legacy <c>Program.GetEmployeeTimePolicy</c>.</summary>
    private TimePolicy GetEmployeeTimePolicy(TimeEngineEmployeeContext employeeContext, DateTime dayDate)
    {
        var policy = ResolveTimePolicies(employeeContext, dayDate).FirstOrDefault();

        if (policy == null)
        {
            throw new Exception(string.Format("Employee TimePolicy not found on: {0}", dayDate.ToShortDateString()));
        }

        return policy;
    }

    private List<TimePolicy> ResolveTimePolicies(TimeEngineEmployeeContext employeeContext, DateTime dayDate)
    {
        var policyIds = employeeContext.TimePolicies
            .Where(r => r.StartDate <= dayDate && r.EndDate >= dayDate)
            .Select(r => r.TimePolicyId)
            .ToList();

        return policyIds.Any()
            ? _timePolicyRepository.GetAll().Where(r => policyIds.Contains(r.Id)).ToList()
            : new List<TimePolicy>();
    }

    #endregion

    #region << Employee sub-group lookups >>

    /// <summary>
    /// Ported from legacy <c>Program.GetEmployeeSubGroupLeave</c>: prefer the lowest <c>ChangeYear</c> band
    /// strictly ABOVE the employee's years of service, falling back to any active band.
    /// </summary>
    private EmployeeSubGroupAbsence GetEmployeeSubGroupLeave(
        TimeEngineMasterData masterData, int absenceTypeId, Employee employee, DateTime dayDate, EmployeeSubGroup subGroup)
    {
        var totalYears = Math.Round((dayDate.Subtract(employee.JoiningDate).TotalDays + 1) / 365, 2);

        var match = masterData.EmployeeSubGroupAbsences
            .Where(r => r.EmployeeSubGroupId == subGroup.Id
                        && r.AbsenceTypeId == absenceTypeId
                        && r.ChangeYear > totalYears
                        && r.StartDate <= dayDate
                        && r.EndDate >= dayDate)
            .OrderBy(r => r.ChangeYear)
            .FirstOrDefault();

        return match ?? masterData.EmployeeSubGroupAbsences
            .FirstOrDefault(r => r.EmployeeSubGroupId == subGroup.Id
                                 && r.AbsenceTypeId == absenceTypeId
                                 && r.StartDate <= dayDate
                                 && r.EndDate >= dayDate);
    }

    /// <summary>Ported from legacy <c>Program.GetEmployeeSubGroupGratuity</c>.</summary>
    private bool HasEmployeeSubGroupGratuity(TimeEngineMasterData masterData, DateTime dayDate, EmployeeSubGroup subGroup)
    {
        return masterData.EmployeeSubGroupGratuities
            .Any(r => r.EmployeeSubGroupId == subGroup.Id && r.StartDate <= dayDate && r.EndDate >= dayDate);
    }

    /// <summary>Ported from legacy <c>Program.GetEmployeeSubGroupTicket</c>.</summary>
    private EmployeeSubGroupTicket GetEmployeeSubGroupTicket(DateTime dayDate, EmployeeSubGroup subGroup, int ticketTypeId)
    {
        return _employeeSubGroupTicketRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeSubGroupId == subGroup.Id
                                 && r.TicketTypeId == ticketTypeId
                                 && r.StartDate <= dayDate
                                 && r.EndDate >= dayDate);
    }

    /// <summary>
    /// PORT NOTE: <see cref="EmployeeAbsenceQuota"/> in this solution has no computed <c>QuotaBalance</c>
    /// property (legacy XPO exposed one). Reproduced here with the exact legacy formula recorded on that
    /// entity's own class remarks: <c>QuotaAccrued - (QuotaUsed + QuotaAdjusted + QuotaForfeited)</c>.
    /// </summary>
    private static double GetQuotaBalance(EmployeeAbsenceQuota quota)
    {
        return quota.QuotaAccrued - (quota.QuotaUsed + quota.QuotaAdjusted + quota.QuotaForfeited);
    }

    #endregion
}
