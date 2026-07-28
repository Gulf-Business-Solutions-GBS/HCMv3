using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Per-employee half of the Time Engine orchestration: run-window resolution (joining date / payroll status /
/// contract / cutoff), the pre-load of every list the day loop needs, the day loop itself, and the post-loop
/// monthly passes. See <see cref="TimeEngineService"/> for the full deviation list.
/// </summary>
public partial class TimeEngineService
{
    /// <summary>
    /// Ported from the body of legacy <c>CreateTimeRegister</c>'s employee <c>foreach</c>.
    /// Returns <c>false</c> when the employee was SKIPPED (legacy's <c>NoofEmp--; continue;</c> path),
    /// <c>true</c> otherwise.
    /// </summary>
    private async Task<bool> ProcessEmployeeAsync(
        TimeEngineRunInput input,
        Employee employee,
        DateTime runStartDate,
        DateTime runEndDate,
        TimeEngineSettings settings,
        TimeEngineMasterData masterData,
        RunCursor cursor,
        int employeeCount)
    {
        #region << Check JoiningDate >>

        if (employee.JoiningDate == default(DateTime))
        {
            throw new Exception("JoiningDate not found.");
        }

        #endregion

        #region << Validate StartDate and EndDate >>

        // Legacy resets the (mutating) loop bounds from the run-level values for every employee.
        var timeStartDate = runStartDate;
        var timeEndDate = runEndDate.Date;

        var payrollStatus = await GetOrCreatePayrollStatusAsync(employee, settings);

        if (settings.EnableTimeEngineAutorun)
        {
            // Legacy: only NextTimeEvaluationFrom is honoured when autorun is on.
            if (payrollStatus.NextTimeEvaluationFrom != default(DateTime) &&
                timeStartDate > payrollStatus.NextTimeEvaluationFrom)
            {
                timeStartDate = payrollStatus.NextTimeEvaluationFrom;
            }
        }
        else
        {
            // Legacy "old method": pull the start date back to the earliest master-data change.
            if (payrollStatus.EarliestMasterDataChange != default(DateTime) &&
                timeStartDate > payrollStatus.EarliestMasterDataChange)
            {
                timeStartDate = payrollStatus.EarliestMasterDataChange;
            }
        }

        // Legacy (11.03.2019): StartDate must not go behind EarliestRetroAccountingDate. The ONLY tolerated
        // case is when that date IS the joining date; otherwise legacy throws and skips the employee entirely.
        // Ported faithfully, including the throw.
        if (payrollStatus.EarliestRetroAccountingDate != default(DateTime) &&
            timeStartDate < payrollStatus.EarliestRetroAccountingDate)
        {
            if (employee.JoiningDate == payrollStatus.EarliestRetroAccountingDate)
            {
                timeStartDate = payrollStatus.EarliestRetroAccountingDate;
            }
            else
            {
                throw new Exception(string.Format(
                    "EarliestRetroAccountingDate set to '{0}' for employee : {1}",
                    payrollStatus.EarliestRetroAccountingDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    employee.Code));
            }
        }

        // Legacy (11.03.2019): clamp to the global earliest evaluation date.
        if (settings.EarliestEvaluationDate != default(DateTime) && timeStartDate < settings.EarliestEvaluationDate)
        {
            timeStartDate = settings.EarliestEvaluationDate;
        }

        // Legacy (17.01.2018): narrow the window to the employee's contract span.
        var contracts = _employeeContractRepository.GetAll().Where(r => r.EmployeeId == employee.Id).ToList();
        var contractStart = contracts.OrderBy(r => r.StartDate).FirstOrDefault();
        var contractEnd = contracts.OrderByDescending(r => r.EndDate).FirstOrDefault();

        if (contractStart == null)
        {
            throw new Exception(string.Format("Contract not found for Employee : {0}", employee.Code));
        }

        if (timeStartDate < contractStart.StartDate)
        {
            timeStartDate = contractStart.StartDate;
        }

        // Legacy (Ticket #3283): stop at the last day in service, NOT the end of that month.
        if (timeEndDate > contractEnd.EndDate)
        {
            timeEndDate = contractEnd.EndDate;
        }

        timeStartDate = timeStartDate < employee.JoiningDate ? employee.JoiningDate : timeStartDate;

        if (timeStartDate > timeEndDate)
        {
            // Legacy: silently skip when running for everyone or from the payroll engine; otherwise throw so a
            // user who explicitly named this employee sees why nothing happened.
            if (string.IsNullOrEmpty(input.EmployeeCodes) || input.IsRanFromPayrollEngine)
            {
                return false;
            }

            throw new Exception(string.Format(
                "JoiningDate : '{0}' > EndDate : {1}.",
                employee.JoiningDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                timeEndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
        }

        #endregion

        #region << Check Cutoff Date >>

        // Legacy recomputes only when the payroll area differs from the previous employee's (see RunCursor).
        var payrollAreaId = GetCurrentPayrollAreaId(employee, timeEndDate);
        if (payrollAreaId != null && cursor.OldPayrollAreaId != payrollAreaId.Value)
        {
            cursor.OldPayrollAreaId = payrollAreaId.Value;

            var cutOffDay = _payrollAreaRepository.GetAll()
                .Where(r => r.Id == payrollAreaId.Value).Select(r => r.CutOffDay).FirstOrDefault();

            if (cutOffDay != 0)
            {
                var payrollControlDate = _payrollControlRecordRepository.GetAll()
                    .Where(r => r.PayrollAreaId == cursor.OldPayrollAreaId)
                    .Select(r => r.CalendarMonthStart)
                    .FirstOrDefault();

                if (payrollControlDate != default(DateTime))
                {
                    var daysInMonth = DateTime.DaysInMonth(payrollControlDate.Year, payrollControlDate.Month);
                    var cutoffDay = daysInMonth < cutOffDay ? daysInMonth : cutOffDay;
                    cursor.CutoffDate = new DateTime(payrollControlDate.Year, payrollControlDate.Month, cutoffDay);
                }
            }
            else
            {
                cursor.CutoffDate = Clock.Now.AddDays(-1).Date;
            }
        }

        #endregion

        var context = BuildEmployeeContext(
            employee, timeStartDate, timeEndDate, payrollStatus, contracts, settings, masterData,
            cursor.CutoffDate, input);

        #region << While Loop timeStartDate <= timeEndDate >>

        var dayDate = timeStartDate;
        while (dayDate <= timeEndDate)
        {
            try
            {
                await CalculateDayAsync(dayDate, context, settings, masterData, employeeCount);
                await AddTimeLogAsync(employee.Id, dayDate, "Successfull", 0, employeeCount);
            }
            catch (Exception ex)
            {
                // Legacy logs the FAILED day against timeStartDate.AddDays(1).AddDays(-1), i.e. the day itself.
                await AddTimeLogAsync(employee.Id, dayDate, ex.Message, 1, employeeCount);
            }

            dayDate = dayDate.AddDays(1);
        }

        #endregion

        #region << MonthlyToleranceDeduction && AnnualLeaveAdjust Logic >>

        // Legacy walks the ORIGINAL run window (f_timeStartDate/f_timeEndDate) month by month, not the
        // employee-narrowed one. Preserved.
        var monthCursor = runStartDate;
        while (monthCursor <= runEndDate)
        {
            var monthStart = new DateTime(monthCursor.Year, monthCursor.Month, 1);
            var monthEnd = new DateTime(monthCursor.Year, monthCursor.Month,
                DateTime.DaysInMonth(monthCursor.Year, monthCursor.Month));

            await CalcMonthlyToleranceDeductionAsync(employee, context, monthStart, monthEnd);

            if (settings.FullMonthLeaveAdjForFixedDays)
            {
                await CalculateAnnualLeaveAdjustAsync(employee, context, masterData, monthStart, monthEnd);
            }

            if (settings.ForfeitRule == SystemSettings.ForfeitRule.Monthly)
            {
                // Delegated, not re-derived: legacy called AccrualService.CalculateMonthlyForfeit here.
                await _accrualService.CalculateMonthlyForfeitAsync(
                    employee.Id, context.EmployeeSubGroup?.Id, monthStart, monthEnd);
            }

            monthCursor = monthCursor.AddMonths(1);
        }

        #endregion

        // NOT PORTED: legacy's "ProbationDate Update" block -- see TimeEngineService class remarks.

        #region << Set NextTimeEvaluationFrom >>

        if (!settings.EnableTimeEngineAutorun)
        {
            // Legacy assigns the post-loop value of timeStartDate (== timeEndDate + 1 day).
            payrollStatus.NextTimeEvaluationFrom = dayDate;
            await _employeePayrollStatusRepository.UpdateAsync(payrollStatus);
        }

        #endregion

        return true;
    }

    /// <summary>
    /// Ported from legacy <c>CreateTimeRegister</c> lines ~309-324. Legacy read
    /// <c>emp.PayrollStatus.FirstOrDefault()</c> (an XPO back-reference collection this port does not have) and
    /// created the row when missing.
    /// </summary>
    private async Task<EmployeePayrollStatus> GetOrCreatePayrollStatusAsync(Employee employee, TimeEngineSettings settings)
    {
        var payrollStatus = _employeePayrollStatusRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == employee.Id);

        if (payrollStatus != null)
        {
            return payrollStatus;
        }

        payrollStatus = new EmployeePayrollStatus
        {
            EmployeeId = employee.Id,
            EarliestMasterDataChange = new DateTime(1, 1, 1),
            NextTimeEvaluationFrom = employee.JoiningDate < settings.EarliestEvaluationDate
                ? settings.EarliestEvaluationDate
                : employee.JoiningDate,
            EarliestRetroAccountingDate = employee.JoiningDate
        };

        payrollStatus.Id = await _employeePayrollStatusRepository.InsertAndGetIdAsync(payrollStatus);
        return payrollStatus;
    }

    /// <summary>
    /// PORT ADAPTATION for legacy <c>emp.CurrentPayrollArea</c> (a computed XPO property this port does not
    /// have -- <see cref="EmployeeOrgAssignment"/> carries no PayrollArea FK either, same gap
    /// <c>AccrualService.CalculateForfeitForPublicHolidayAsync</c> works around by taking
    /// <c>payrollAreaId</c> explicitly). Resolved here from the <see cref="PayrollControlRecord"/> covering
    /// <paramref name="asOnDate"/>; returns null when it cannot be determined, which makes the caller keep the
    /// previous cutoff date -- exactly what legacy did when <c>CurrentPayrollArea</c> was null.
    /// </summary>
    private int? GetCurrentPayrollAreaId(Employee employee, DateTime asOnDate)
    {
        return _payrollControlRecordRepository.GetAll()
            .Where(r => r.CalendarMonthStart <= asOnDate && r.CalendarMonthEnd >= asOnDate)
            .Select(r => (int?)r.PayrollAreaId)
            .FirstOrDefault();
    }

    /// <summary>
    /// Ported from legacy <c>CreateTimeRegister</c>'s "Load All Common Objects" (lines ~383-455) and
    /// "Get Quota information" (lines ~457-466) regions -- one pre-load per employee, reused by every day.
    /// </summary>
    private TimeEngineEmployeeContext BuildEmployeeContext(
        Employee employee,
        DateTime timeStartDate,
        DateTime timeEndDate,
        EmployeePayrollStatus payrollStatus,
        List<EmployeeContract> contracts,
        TimeEngineSettings settings,
        TimeEngineMasterData masterData,
        DateTime cutoffDate,
        TimeEngineRunInput input)
    {
        var employeeId = employee.Id;

        var timePolicies = _employeeTimePolicyRepository.GetAll().Where(r => r.EmployeeId == employeeId).ToList();
        var timePolicyIds = timePolicies.Select(r => r.TimePolicyId).Distinct().ToList();
        var policiesById = _timePolicyRepository.GetAll()
            .Where(r => timePolicyIds.Contains(r.Id)).ToList().ToDictionary(r => r.Id);

        // Legacy loadTimeEvent guard: skip the (expensive) punch load when every policy is PlannedTime and
        // UseActualTransInPlannedTimePolicy is off. Null vs empty is meaningful to T3 -- see the context model.
        var loadTimeEvent = settings.UseActualTransInPlannedTimePolicy ||
                            timePolicies.Any(r => policiesById.ContainsKey(r.TimePolicyId) &&
                                                  policiesById[r.TimePolicyId].TimeMgmtStatus != TimeMgmtStatus.PlannedTime);

        List<TimePunchEvent> timeEvents = null;
        if (loadTimeEvent)
        {
            var eventFrom = timeStartDate.AddDays(-1);
            var eventTo = timeEndDate.AddDays(1);

            var transTypeCodes = _transTypeRepository.GetAll().ToList().ToDictionary(r => r.Id, r => r.Code);

            timeEvents = _timeEventRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.TransDate >= eventFrom && r.TransDate <= eventTo)
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
        }

        var orgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId).ToList();

        // Legacy read EmployeeOrgAssignment.EmployeeSubGroup date-effectively; that FK now exists, so the
        // engine resolves the sub-group ITSELF instead of demanding it from the caller (see
        // TimeEngineRunInput.EmployeeSubGroupIds, now obsolete). The caller-supplied map is honoured only as an
        // explicit override for the legacy console entry point, and only when the FK has nothing to say.
        int? subGroupId = orgAssignments
            .Where(r => r.StartDate <= timeEndDate && r.EndDate >= timeStartDate && r.EmployeeSubGroupId != null)
            .OrderByDescending(r => r.StartDate)
            .Select(r => r.EmployeeSubGroupId)
            .FirstOrDefault();

        if (subGroupId == null && input.EmployeeSubGroupIds != null &&
            input.EmployeeSubGroupIds.ContainsKey(employeeId))
        {
            subGroupId = input.EmployeeSubGroupIds[employeeId];
        }

        return new TimeEngineEmployeeContext
        {
            Employee = employee,
            StartDate = timeStartDate,
            EndDate = timeEndDate,
            PayrollStatus = payrollStatus,
            CutoffDate = cutoffDate,
            EmployeeSubGroup = subGroupId == null
                ? null
                : masterData.EmployeeSubGroups.FirstOrDefault(r => r.Id == subGroupId.Value),
            Contracts = contracts,
            TimeRegisters = _timeRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.DayDate >= timeStartDate && r.DayDate <= timeEndDate)
                .ToList(),
            OrgAssignments = orgAssignments,
            DutyPeriods = _employeeDutyPeriodRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId).ToList(),
            TimePolicies = timePolicies,
            TimeEvents = timeEvents,
            SubstituteDuties = _employeeSubstituteDutyRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId &&
                            r.SubstituteDate >= timeStartDate && r.SubstituteDate <= timeEndDate).ToList(),
            TimeTransfers = _timeTransferRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId &&
                            r.TransferTypeId == masterData.TimeTransferTypeId &&
                            r.TransferDate >= timeStartDate && r.TransferDate <= timeEndDate).ToList(),
            Absences = _employeeAbsenceRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId &&
                            ((r.StartDate <= timeStartDate && timeStartDate <= r.EndDate)
                             || (r.StartDate <= timeEndDate && timeEndDate <= r.EndDate)
                             || (timeStartDate <= r.StartDate && r.StartDate <= timeEndDate)
                             || (timeStartDate <= r.EndDate && r.EndDate <= timeEndDate))).ToList(),
            NotReturnedAbsence = GetNotReturnedAbsence(employeeId, timeEndDate),
            Attendances = _employeeAttendanceRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId &&
                            ((r.StartDate <= timeStartDate && timeStartDate <= r.EndDate)
                             || (r.StartDate <= timeEndDate && timeEndDate <= r.EndDate)
                             || (timeStartDate <= r.StartDate && r.StartDate <= timeEndDate)
                             || (timeStartDate <= r.EndDate && r.EndDate <= timeEndDate))).ToList(),
            Overtimes = _employeeOvertimeRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId &&
                            r.EffectiveDate >= timeStartDate && r.EffectiveDate <= timeEndDate).ToList(),
            Tickets = _employeeTicketRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId &&
                            r.PaymentDate >= timeStartDate && r.PaymentDate <= timeEndDate).ToList(),
            AbsAdjustments = _employeeAbsAdjustmentRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId &&
                            r.AdjustmentType == AdjustmentType.Fractional &&
                            r.EffectiveDate >= timeStartDate && r.EffectiveDate <= timeEndDate).ToList(),
            AnnualLeaveQuotas = _employeeAbsenceQuotaRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.AbsenceTypeId == masterData.AnnualLeaveTypeId &&
                            !(r.EntitlementFrom > timeEndDate || r.EntitlementTo < timeStartDate)).ToList(),
            OtherLeaveQuotas = _employeeAbsenceQuotaRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.AbsenceTypeId != masterData.AnnualLeaveTypeId &&
                            !(r.EntitlementFrom > timeEndDate || r.EntitlementTo < timeStartDate)).ToList(),
            TicketQuotas = _employeeTicketQuotaRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.TicketTypeId == masterData.AirTicketTypeId &&
                            !(r.EntitlementFrom > timeEndDate || r.EntitlementTo < timeStartDate)).ToList(),
            GratuityQuotas = _employeeGratuityQuotaRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId &&
                            !(r.EntitlementFrom > timeEndDate || r.EntitlementTo < timeStartDate)).ToList(),
            BasicPays = _employeeBasicPayRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId &&
                            !(r.StartDate > timeEndDate || r.EndDate < timeStartDate)).ToList()
        };
    }

    /// <summary>
    /// Ported from legacy <c>empAbsenceNotReturnList</c> (lines ~422-429): the earliest still-open absence of a
    /// return-date-required type that ended before the run's end date, excluding hourly-basis leave.
    /// </summary>
    private EmployeeAbsence GetNotReturnedAbsence(int employeeId, DateTime timeEndDate)
    {
        var returnRequiredTypeIds = _absenceTypeRepository.GetAll()
            .Where(r => r.IsReturnDateRequired).Select(r => r.Id).ToList();

        if (!returnRequiredTypeIds.Any())
        {
            return null;
        }

        return _employeeAbsenceRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && returnRequiredTypeIds.Contains(r.AbsenceTypeId)
                        && r.ReturnDate == default(DateTime)
                        && !r.IsReturned
                        && r.EndDate < timeEndDate
                        && r.AbsenceHours == 0)
            .OrderBy(r => r.StartDate)
            .FirstOrDefault();
    }
}
