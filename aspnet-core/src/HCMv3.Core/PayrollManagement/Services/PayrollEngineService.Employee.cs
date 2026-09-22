using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Per-employee half of <see cref="PayrollEngineService"/> -- the port of legacy
/// <c>HCM10.PayrollEngine/Program.cs</c> lines 756-1469 (the body of the employee loop inside
/// <c>CreateSalaryRegister</c>).
/// </summary>
public partial class PayrollEngineService
{
    /// <summary>
    /// Processes one employee for one run.
    /// </summary>
    /// <returns>
    /// <c>false</c> when the employee was SKIPPED before any register work (legacy's
    /// <c>noofEmp = noofEmp - 1; continue;</c> at lines 794-795) -- the caller decrements the employee count
    /// and writes no <see cref="PayrollLog"/> row. <c>true</c> when the employee was processed.
    ///
    /// <para><b>Faithful legacy asymmetry:</b> the JoiningDate skip (line 794) decrements the count, while the
    /// two contract skips (lines 810, 822) do a bare <c>continue</c> and do NOT. Both are reproduced -- the
    /// contract paths return <c>false</c> too, but through <see cref="SkipWithoutDecrement"/>, which the
    /// caller distinguishes.</para>
    /// </returns>
    private async Task<bool> ProcessEmployeeAsync(
        PayrollEngineRunInput input,
        Employee employee,
        DateTime payInDate,
        PayrollEngineSettings settings,
        PayrollEmployeeRunResult employeeResult)
    {
        var hasEmployeeCodeFilter = !string.IsNullOrWhiteSpace(input.EmployeeCodes);

        // Legacy "Load All Common Objects" (lines 780-785).
        var orgAssignments = _employeeOrgAssignmentRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id).ToList();

        var contracts = _employeeContractRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id).ToList();

        #region << Check JoiningDate (legacy lines 787-800) >>

        if (employee.JoiningDate == default(DateTime))
        {
            throw new UserFriendlyException("JoiningDate not found.");
        }

        if (employee.JoiningDate > payInDate)
        {
            if (!hasEmployeeCodeFilter)
            {
                return false;
            }

            throw new UserFriendlyException(string.Format(
                "JoiningDate : '{0}' > PayInDate : {1}.",
                employee.JoiningDate.ToString("dd/MM/yyyy"),
                payInDate.ToString("dd/MM/yyyy")));
        }

        #endregion

        #region << Check Contract (legacy lines 802-826) >>

        var contractStart = contracts.OrderBy(r => r.StartDate).FirstOrDefault();
        if (contractStart == null)
        {
            throw new UserFriendlyException(string.Format("Employee Contract not found for Employee {0}", employee.Code));
        }

        if (contractStart.StartDate > payInDate)
        {
            if (!hasEmployeeCodeFilter)
            {
                SkipWithoutDecrement = true;
                return false;
            }

            throw new UserFriendlyException(string.Format(
                "Contract Started on '{0}'. Can't run Payroll for Date : '{1}'",
                contractStart.StartDate.ToString("dd/MM/yyyy"),
                payInDate.ToString("dd/MM/yyyy")));
        }

        var contractEnd = contracts.OrderByDescending(r => r.EndDate).FirstOrDefault();

        if (contractEnd.EndDate < new DateTime(payInDate.Year, payInDate.Month, 1, 0, 0, 0)
            && input.OffCycleReason != PayrollOffCycleReasons.EndOfService)
        {
            if (!hasEmployeeCodeFilter)
            {
                SkipWithoutDecrement = true;
                return false;
            }

            throw new UserFriendlyException(string.Format(
                "Contract EndDate on '{0}'. Can't run Payroll for Date : '{1}'",
                contractEnd.EndDate.ToString("dd/MM/yyyy"),
                payInDate.ToString("dd/MM/yyyy")));
        }

        #endregion

        #region << OrgAssignment / PayrollArea / ControlRecord (legacy lines 828-850) >>

        if (!orgAssignments.Any())
        {
            throw new UserFriendlyException(string.Format("Org Assignment not found for Employee {0}", employee.Code));
        }

        var employeePayrollArea = await _payrollAreaResolver.GetEmployeePayrollAreaAsync(employee.Id, payInDate);
        if (employeePayrollArea == null)
        {
            throw new UserFriendlyException("Payroll area not found in OrgAssignment.");
        }

        var employeeControlRecord = _payrollControlRecordRepository.GetAll()
            .FirstOrDefault(r => r.PayrollAreaId == employeePayrollArea.Id);
        if (employeeControlRecord == null)
        {
            throw new UserFriendlyException(string.Format(
                "Payroll Control Record not found for PayrollArea {0}", employeePayrollArea.Code));
        }

        // Legacy lines 840-849, extracted verbatim into the pure guard helper.
        PayrollControlRecordGuard.EnsureEmployeeControlRecordMatches(
            employeeControlRecord, employeePayrollArea, input.PayrollRunType, payInDate);

        #endregion

        #region << Check PayrollStatus (legacy lines 852-858) >>

        var payrollStatus = _employeePayrollStatusRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == employee.Id);
        if (payrollStatus == null)
        {
            throw new UserFriendlyException(string.Format(
                "Payroll Status not found for Employee :{0}", employee.Code));
        }

        if (payrollStatus.NextTimeEvaluationFrom < input.OffCyclePaymentDate
            && input.OffCycleReason != PayrollOffCycleReasons.EndOfService)
        {
            throw new UserFriendlyException(string.Format(
                "Time Engine not run for Employee : {0} from Date : {1}",
                employee.Code,
                payrollStatus.NextTimeEvaluationFrom.ToString("dd/MM/yyyy")));
        }

        #endregion

        var onHoldRecord = _employeeOnHoldRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == employee.Id && r.StartDate <= payInDate && r.EndDate >= payInDate);

        var onHold = BuildOnHoldFlags(onHoldRecord);

        var notReturnAbsence = ResolveNotReturnFromLeaveAbsence(employee, payInDate, input.PayrollRunType, employeePayrollArea, settings);

        // Legacy line 940.
        var isNotReturnFromLeave = settings.IsNotReturnFromLeaveLogicEnabled
                                   && notReturnAbsence != null
                                   && notReturnAbsence.EndDate < payInDate;

        var registers = _salaryRegisterRepository.GetAll().Where(r => r.EmployeeId == employee.Id);

        if (input.PayrollRunType == PayrollRunType.Regular)
        {
            await ProcessRegularEmployeeAsync(
                input, employee, payInDate, settings, onHold, onHoldRecord, isNotReturnFromLeave,
                notReturnAbsence, payrollStatus, employeePayrollArea, orgAssignments, employeeResult);
        }
        else
        {
            await ProcessOffCycleEmployeeAsync(
                input, employee, payInDate, settings, onHold, isNotReturnFromLeave,
                notReturnAbsence, payrollStatus, employeePayrollArea, orgAssignments, employeeResult);
        }

        #region << Reset AccountedTo & EarliestMasterDataChange (legacy lines 1386-1415) >>

        if (!onHold.AnyHold)
        {
            // Legacy line 1392: skipped for an off-cycle run that is NOT end of service, so a later regular
            // run can still retro over the advance amount.
            var isNonEosOffCycle = input.PayrollRunType == PayrollRunType.OffCycle
                                   && input.OffCycleReason != PayrollOffCycleReasons.EndOfService;

            if (!isNonEosOffCycle
                && (payrollStatus.AccountedTo != payInDate || payrollStatus.EarliestMasterDataChange != DateTime.MinValue))
            {
                payrollStatus.AccountedTo = payInDate;
                payrollStatus.EarliestMasterDataChange = DateTime.MinValue;
                await _employeePayrollStatusRepository.UpdateAsync(payrollStatus);

                // NOT PORTED: SysService.AddRetroReasonLog (legacy lines 1408-1411) -- no RetroReasonLog
                // entity exists in this solution (same decision as TimeEngineService).
            }
        }

        #endregion

        return true;
    }

    /// <summary>
    /// Set by <see cref="ProcessEmployeeAsync"/> when the skip is legacy's bare <c>continue</c> (contract
    /// checks, lines 810/822) rather than the count-decrementing one (JoiningDate, line 794). Read and reset
    /// by the caller.
    /// </summary>
    private bool SkipWithoutDecrement { get; set; }

    #region << OnHold (legacy lines 860-896) >>

    /// <summary>
    /// Legacy's seven <c>onHold_*</c> locals, including the <c>onHold_All == true</c> fan-out at lines
    /// 885-894 ("all seven become true"), applied here so consumers only ever see resolved flags.
    /// </summary>
    private static PayrollOnHoldFlags BuildOnHoldFlags(EmployeeOnHold record)
    {
        var flags = new PayrollOnHoldFlags();

        if (record == null)
        {
            return flags;
        }

        flags.All = record.OnHold;

        if (flags.All)
        {
            flags.BasicPay = true;
            flags.LeavePay = true;
            flags.IndemnityPay = true;
            flags.AirTicketPay = true;
            flags.LoanDeduction = true;
            flags.AllEarnings = true;
            flags.AllDeductions = true;
            return flags;
        }

        flags.BasicPay = record.OnHoldBasicPay;
        flags.LeavePay = record.OnHoldLeavePay;
        flags.IndemnityPay = record.OnHoldIndemnityPay;
        flags.AirTicketPay = record.OnHoldAirTicketPay;
        flags.LoanDeduction = record.OnHoldLoanDeduction;
        flags.AllEarnings = record.OnHoldAllEarnings;
        flags.AllDeductions = record.OnHoldAllDeductions;

        return flags;
    }

    #endregion

    #region << Not-return-from-leave (legacy lines 898-941) >>

    /// <summary>
    /// Legacy's "Check if Any Not Return From Leave Exists or Not" region. The earliest unreturned leave whose
    /// <c>AbsenceType.IsReturnDateRequired</c> is set, that ended before <paramref name="payInDate"/> and is
    /// not an hourly leave (legacy line 906 excludes <c>AbsenceHours != 0</c>), then narrowed:
    /// <list type="bullet">
    /// <item><description><b>Regular</b>, <c>PayLeaveAsPerCutoff</c> ON: ignored when it starts after the
    /// payroll area's cut-off day (legacy 914-922).</description></item>
    /// <item><description><b>Regular</b>, <c>PayLeaveAsPerCutoff</c> OFF: always ignored (legacy 926-930).</description></item>
    /// <item><description><b>OffCycle</b>: ignored when it starts on or after the month start (legacy 933-938).</description></item>
    /// </list>
    /// </summary>
    private EmployeeAbsence ResolveNotReturnFromLeaveAbsence(
        Employee employee,
        DateTime payInDate,
        PayrollRunType payrollRunType,
        PayrollArea payrollArea,
        PayrollEngineSettings settings)
    {
        var absence = _employeeAbsenceRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id
                        && r.AbsenceType != null
                        && r.AbsenceType.IsReturnDateRequired
                        && r.ReturnDate == default(DateTime)
                        && !r.IsReturned
                        && r.EndDate < payInDate
                        && r.AbsenceHours == 0)
            .OrderBy(r => r.StartDate)
            .FirstOrDefault();

        if (absence == null)
        {
            return null;
        }

        // The "ReturnDate not found for LeaveType : '...'" message names the type, as legacy's did. Filtering on
        // the navigation does not load it (and Core has no EF Include), so it is fetched explicitly.
        absence.AbsenceType ??= _employeeAbsenceRepository.GetAll()
            .Where(r => r.Id == absence.Id)
            .Select(r => r.AbsenceType)
            .FirstOrDefault();

        if (payrollRunType == PayrollRunType.Regular)
        {
            if (!settings.PayLeaveAsPerCutoff)
            {
                return null;
            }

            var cutOffDay = payInDate;
            if (payrollArea != null && payrollArea.CutOffDay != 0)
            {
                cutOffDay = GetCutoffEndDate(payInDate, payrollArea.CutOffDay);
            }

            return absence.StartDate > cutOffDay ? null : absence;
        }

        var monthStart = new DateTime(payInDate.Year, payInDate.Month, 1, 0, 0, 0);
        return absence.StartDate >= monthStart ? null : absence;
    }

    /// <summary>
    /// Legacy <c>DateTimeExtension.GetCutoffEndDate</c>, identical to the private copy in
    /// <see cref="PayrollDayCountService"/> (which is not visible from here).
    /// </summary>
    private static DateTime GetCutoffEndDate(DateTime payDate, int cutoffDay)
    {
        var daysInMonth = DateTime.DaysInMonth(payDate.Year, payDate.Month);
        var day = daysInMonth < cutoffDay ? daysInMonth : cutoffDay;
        return new DateTime(payDate.Year, payDate.Month, day, 0, 0, 0);
    }

    #endregion

    #region << Retro period collection (legacy lines 967-1040 / 1204-1291) >>

    /// <summary>
    /// Legacy's "Collect Period To Calculate Payroll" region: every month end from the retro trigger date up
    /// to and including <paramref name="payInDate"/>.
    ///
    /// <para>Legacy runs the SAME while-loop twice -- once seeded from <c>EarliestMasterDataChange</c> and
    /// once from <c>AccountedTo + 1 month</c> -- and appends both into one list, so a period reachable from
    /// both seeds appears TWICE (legacy never de-duplicates the intermediate periods, only
    /// <c>payInDate</c> itself via its <c>Contains</c> guards). That would calculate the same register twice.
    /// This port de-duplicates. <b>Documented deviation</b>; the second calculation would be idempotent (the
    /// register is cleared and recalculated) but doubles the work and the audit rows.</para>
    /// </summary>
    private List<DateTime> CollectPayrollPeriods(
        Employee employee,
        EmployeePayrollStatus payrollStatus,
        DateTime payInDate,
        PayrollEngineSettings settings,
        bool includeMasterDataChangeSeed,
        bool throwWhenBeforeEarliestRetroAccountingDate)
    {
        var periods = new List<DateTime>();

        if (includeMasterDataChangeSeed)
        {
            var mdChangeDate = payrollStatus.EarliestMasterDataChange;
            var minRetroAccountingDate = payrollStatus.EarliestRetroAccountingDate;

            if (mdChangeDate != default(DateTime) && minRetroAccountingDate != default(DateTime) && mdChangeDate < minRetroAccountingDate)
            {
                if (throwWhenBeforeEarliestRetroAccountingDate)
                {
                    // Legacy line 974 (Regular arm): THROWS.
                    throw new UserFriendlyException(string.Format(
                        "EarliestRetroAccountingDate set to '{0}' for employee : {1}",
                        minRetroAccountingDate.ToString("dd/MM/yyyy"),
                        employee.Code));
                }

                // Legacy line 1223 (End-of-service off-cycle arm): CLAMPS instead of throwing.
                mdChangeDate = minRetroAccountingDate;
            }

            if (mdChangeDate != default(DateTime) && settings.EarliestEvaluationDate != default(DateTime) && mdChangeDate < settings.EarliestEvaluationDate)
            {
                mdChangeDate = settings.EarliestEvaluationDate;
            }

            if (mdChangeDate != default(DateTime) && employee.JoiningDate != default(DateTime) && mdChangeDate < employee.JoiningDate)
            {
                mdChangeDate = employee.JoiningDate;
            }

            AddMonthEndsUpTo(periods, mdChangeDate, payInDate, addOneMonth: false);
        }

        var accountedTo = payrollStatus.AccountedTo;

        if (accountedTo != default(DateTime) && settings.EarliestEvaluationDate != default(DateTime) && accountedTo < settings.EarliestEvaluationDate)
        {
            accountedTo = settings.EarliestEvaluationDate;
        }

        if (accountedTo != default(DateTime) && employee.JoiningDate != default(DateTime) && accountedTo < employee.JoiningDate)
        {
            accountedTo = employee.JoiningDate;
        }

        AddMonthEndsUpTo(periods, accountedTo, payInDate, addOneMonth: true);

        return periods.Distinct().OrderBy(r => r.Date).ToList();
    }

    /// <summary>
    /// Legacy's inner while-loop (lines 988-997 / 1018-1033). <paramref name="addOneMonth"/> reproduces the
    /// AccountedTo seed's "+1 month" step (legacy lines 1020-1024) -- payroll restarts from the month AFTER
    /// the last accounted one. <paramref name="payInDate"/> is always included, even when the seed is unset.
    /// </summary>
    private static void AddMonthEndsUpTo(List<DateTime> periods, DateTime seed, DateTime payInDate, bool addOneMonth)
    {
        if (seed != default(DateTime) && seed != new DateTime(1, 1, 1))
        {
            var cursor = PayrollControlRecordGuard.MonthEnd(seed);

            if (addOneMonth)
            {
                cursor = PayrollControlRecordGuard.MonthEnd(cursor.AddDays(1));
            }

            while (cursor < payInDate)
            {
                periods.Add(cursor);
                cursor = PayrollControlRecordGuard.MonthEnd(cursor.AddMonths(1));
            }
        }

        if (!periods.Contains(payInDate))
        {
            periods.Add(payInDate);
        }
    }

    #endregion
}
