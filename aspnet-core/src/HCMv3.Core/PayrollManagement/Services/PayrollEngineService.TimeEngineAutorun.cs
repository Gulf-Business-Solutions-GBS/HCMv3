using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The TIME ENGINE AUTORUN half of <see cref="PayrollEngineService"/> -- the port of legacy
/// <c>HCM10.PayrollEngine/Program.cs</c> lines 554-753, which evaluates time for an employee INSIDE the payroll
/// run, immediately before that employee's registers are built, so the operator never has to run the Time
/// Engine by hand first.
///
/// <para><b>Why it is here and not in the Application layer.</b> An earlier wave deferred this as "an
/// application/background-job orchestration concern", which would have forced it to become a PRE-PASS over all
/// selected employees. Legacy interleaves -- employee 1's time evaluation, employee 1's payroll, employee 2's
/// time evaluation, ... -- and <see cref="ITimeEngineService"/> already lives in <c>HCMv3.Core</c>, so the call
/// sits exactly where legacy has it with no layer violation.</para>
///
/// <para><b>ONE DELIBERATE DEVIATION, in mechanism not behaviour.</b> Legacy runs
/// <c>TRUNCATE TABLE [dbo].[TimeLog]</c> before each employee (legacy line 700) purely so that its "did this
/// employee fail?" check sees only the current run. That is not reproducible here and must not be: <c>TimeLog</c>
/// is multi-tenant, audited and soft-deletable, and concurrent runs for other tenants write to it. The same
/// scoping is achieved by filtering on <see cref="TimeLog.RunTimeStamp"/> -- the check looks only at rows this
/// call produced. Identical outcome, nothing destroyed. Everything else below is legacy's, including its
/// quirks.</para>
///
/// <para><b>Failure handling is legacy's:</b> a time-engine failure for one employee writes a failed
/// <c>PayrollLog</c> row and skips that employee's payroll, leaving the rest of the run alone. That falls out of
/// the caller's existing per-employee try/catch (legacy's own <c>catch { log; continue; }</c> at lines
/// 725-751), so nothing extra is needed here -- this method simply throws.</para>
/// </summary>
public partial class PayrollEngineService
{
    /// <summary>
    /// Legacy lines 554-753. Evaluates time for <paramref name="employee"/> and returns the off-cycle payment
    /// date the rest of that employee's run must use.
    ///
    /// <para><b>Why it returns a date.</b> Legacy REASSIGNS its own <c>offCyclePaymentDate</c> local inside this
    /// block (lines 612, 648, 684) from whichever leave / full-and-final / encashment record it found, and every
    /// later rule reads that reassigned value. Returning it -- and cloning the input per employee at the call
    /// site -- reproduces that without letting one employee's date leak into the next, which legacy's shared
    /// local technically allows. Every arm either assigns it or throws, so in legacy no stale value survives
    /// either; this is the same behaviour, made explicit.</para>
    /// </summary>
    private async Task<DateTime> RunTimeEngineAutorunAsync(
        PayrollEngineRunInput input,
        Employee employee,
        DateTime payInDate,
        PayrollEngineSettings settings)
    {
        var offCyclePaymentDate = input.OffCyclePaymentDate;

        // Legacy lines 566-567: the window starts at the run month's first day and ends at its last.
        var teStartDate = new DateTime(payInDate.Year, payInDate.Month, 1, 0, 0, 0);
        var teEndDate = new DateTime(payInDate.Year, payInDate.Month, DateTime.DaysInMonth(payInDate.Year, payInDate.Month), 0, 0, 0);

        var payrollStatus = _employeePayrollStatusRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == employee.Id);

        // Legacy lines 570-572: resume from where time evaluation last reached, so a re-run does not redo work
        // already done. BOTH legacy sentinels are treated as unset -- default(DateTime) and its DBDtMinDate.
        if (payrollStatus != null && IsTimeEvaluationDateSet(payrollStatus.NextTimeEvaluationFrom))
        {
            teStartDate = payrollStatus.NextTimeEvaluationFrom;
        }

        if (input.PayrollRunType == PayrollRunType.Regular)
        {
            // Legacy lines 575-585: a leave paid in full WITH this payroll pushes the window out to the leave's
            // end, so the spilled months are evaluated before payroll reads them.
            var advanceLeave = EmployeeAbsences(employee.Id)
                .Where(r => r.AbsenceType.IsPaid
                            && r.LeavePaymentType == LeavePaymentType.PayFullWithPayroll
                            && r.PaymentDate != default(DateTime)
                            && r.PaymentDate.Month == payInDate.Month
                            && r.PaymentDate.Year == payInDate.Year)
                .OrderByDescending(r => r.EndDate)
                .FirstOrDefault();

            if (advanceLeave != null)
            {
                teEndDate = advanceLeave.EndDate.Date;
            }
        }
        else if (input.PayrollRunType == PayrollRunType.OffCycle)
        {
            var resolved = ResolveAutorunOffCycleWindow(input, employee, payInDate, settings, teStartDate, teEndDate);
            offCyclePaymentDate = resolved.OffCyclePaymentDate;
            teEndDate = resolved.TimeEngineEndDate;
        }

        // Legacy line 693: nothing to evaluate when the window is empty (already up to date).
        if (teStartDate > teEndDate)
        {
            return offCyclePaymentDate;
        }

        // NOT PORTED, deliberately: legacy's TRUNCATE TABLE [dbo].[TimeLog] (line 700). See class remarks --
        // the run-stamp filter below scopes the error check instead.
        var runStartedAt = Clock.Now;

        // Legacy lines 706-713: the six console arguments, now a typed input.
        await _timeEngineService.CreateTimeRegisterAsync(new TimeEngineRunInput
        {
            EmployeeCodes = employee.Code,
            StartDate = teStartDate,
            EndDate = teEndDate,
            PayrollAreaName = input.PayrollAreaName,
            CurrentUserName = PayrollEngineOriginatorUserName,
            IsRanFromPayrollEngine = true
        });

        // Legacy lines 717-720: the FIRST failed day for this employee fails the employee. Legacy ordered by
        // DayDate and relied on the truncation for scoping; the run stamp does the scoping here.
        var failure = _timeLogRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id
                        && r.Status == TimeLogFailedStatus
                        && r.RunTimeStamp >= runStartedAt)
            .OrderBy(r => r.DayDate)
            .FirstOrDefault();

        if (failure != null)
        {
            throw new UserFriendlyException(string.Format("TimeEngine : {0}", failure.Message));
        }

        return offCyclePaymentDate;
    }

    /// <summary>
    /// Legacy lines 588-691 -- the off-cycle arm. Each reason finds its own driving record, pushes
    /// <paramref name="teEndDate"/> out to that record's end, and REPLACES the off-cycle payment date with the
    /// record's own. Every arm throws when its record is missing, with legacy's exact message.
    /// </summary>
    private (DateTime OffCyclePaymentDate, DateTime TimeEngineEndDate) ResolveAutorunOffCycleWindow(
        PayrollEngineRunInput input,
        Employee employee,
        DateTime payInDate,
        PayrollEngineSettings settings,
        DateTime teStartDate,
        DateTime teEndDate)
    {
        var offCyclePaymentDate = input.OffCyclePaymentDate;
        var offCycleEndDate = default(DateTime);

        // Legacy lines 593-595. THE ONLY CONSUMER OF PickOffcyclePayment in the whole engine: "CurrentDate"
        // limits the search to records dated up to TODAY (when today falls inside the window), "CurrentMonth"
        // lets it run to the pay-in date. Until this block was ported the setting did nothing at all.
        var pickOffCyclePaymentDate =
            settings.PickOffcyclePayment == PickOffcyclePayment.CurrentDate
            && teStartDate <= Clock.Now.Date
            && teEndDate >= Clock.Now.Date
                ? Clock.Now.Date
                : payInDate;

        if (input.OffCycleReason == PayrollOffCycleReasons.AdvanceLeave)
        {
            // Legacy lines 600-613: the advance-paid leave comes first.
            var advanceLeave = EmployeeAbsences(employee.Id)
                .Where(r => r.AbsenceType.IsPaid
                            && r.LeavePaymentType == LeavePaymentType.PayInAdvance
                            && r.PaymentDate != default(DateTime)
                            && r.PaymentDate.Month == teEndDate.Month
                            && r.PaymentDate.Year == teEndDate.Year
                            && r.PaymentDate <= pickOffCyclePaymentDate)
                .OrderByDescending(r => r.EndDate)
                .FirstOrDefault();

            if (advanceLeave != null)
            {
                offCycleEndDate = advanceLeave.EndDate.Date;
                offCyclePaymentDate = advanceLeave.PaymentDate.Date;
            }
            else
            {
                // Legacy lines 616-632: failing that, an advance-paid encashment/fractional adjustment.
                var adjustment = AutorunEncashmentAdjustment(employee.Id, teEndDate, pickOffCyclePaymentDate);
                if (adjustment == null)
                {
                    throw new UserFriendlyException(string.Format(
                        "Leaves not found for offcycle payment for Employee : {0}", CodeWithFullNameEn(employee)));
                }

                offCycleEndDate = adjustment.EffectiveDate.Date;
                offCyclePaymentDate = adjustment.EffectiveDate.Date;
            }
        }
        else if (input.OffCycleReason == PayrollOffCycleReasons.EndOfService)
        {
            // Legacy lines 637-663.
            var fnf = _employeeFnFPaymentRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id
                            && r.CompensationDate != default(DateTime)
                            && r.CompensationDate.Month == teEndDate.Month
                            && r.CompensationDate.Year == teEndDate.Year
                            && r.CompensationDate <= pickOffCyclePaymentDate)
                .OrderByDescending(r => r.CompensationDate)
                .FirstOrDefault();

            if (fnf == null)
            {
                throw new UserFriendlyException(string.Format(
                    "Full and Final Payment Compensation Date entry not found for Employee : {0}.", employee.Code));
            }

            offCyclePaymentDate = fnf.CompensationDate.Date;

            var separation = _employeeSeparationRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id && r.RevokeDate == null)
                .OrderByDescending(r => r.SubmissionDate)
                .FirstOrDefault();

            if (separation == null)
            {
                throw new UserFriendlyException(string.Format(
                    "Separation (Exit) entry not found for Employee : {0}.", employee.Code));
            }

            // Legacy lines 656-659: whichever is later.
            offCycleEndDate = separation.LastDayInService < fnf.CompensationDate
                ? fnf.CompensationDate.Date
                : separation.LastDayInService.Date;
        }
        else if (input.OffCycleReason == PayrollOffCycleReasons.LeaveEncashment)
        {
            // Legacy lines 667-686.
            var adjustment = AutorunEncashmentAdjustment(employee.Id, teEndDate, pickOffCyclePaymentDate);
            if (adjustment == null)
            {
                throw new UserFriendlyException(string.Format(
                    "Leaves not found for offcycle payment for Employee : {0}", CodeWithFullNameEn(employee)));
            }

            offCycleEndDate = adjustment.EffectiveDate.Date;
            offCyclePaymentDate = adjustment.EffectiveDate.Date;
        }

        // Legacy lines 689-690: the window only ever EXTENDS, never shrinks.
        if (offCycleEndDate != default(DateTime) && teEndDate < offCycleEndDate)
        {
            teEndDate = offCycleEndDate;
        }

        return (offCyclePaymentDate, teEndDate);
    }

    /// <summary>
    /// Legacy's advance-paid annual-leave encashment/fractional adjustment lookup, written out twice in legacy
    /// (lines 618-626 and 671-679) with identical predicates. Note the OR on the absence type -- code "1" or
    /// <see cref="QuotaType.AnnualLeave"/> -- which legacy does NOT assume coincide.
    /// </summary>
    private EmployeeAbsAdjustment AutorunEncashmentAdjustment(
        int employeeId,
        DateTime teEndDate,
        DateTime pickOffCyclePaymentDate)
    {
        return _employeeAbsAdjustmentRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && (r.AbsenceType.Code == AnnualLeaveAbsenceTypeCode
                            || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                        && r.EffectiveDate != default(DateTime)
                        && r.EffectiveDate.Month == teEndDate.Month
                        && r.EffectiveDate.Year == teEndDate.Year
                        && r.EffectiveDate <= pickOffCyclePaymentDate
                        && (r.AdjustmentType == AdjustmentType.Encashment
                            || r.AdjustmentType == AdjustmentType.Fractional)
                        && r.AdjustmentPaymentType == AdjustmentPaymentType.PayInAdvance)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefault();
    }

    private IQueryable<EmployeeAbsence> EmployeeAbsences(int employeeId)
    {
        return _employeeAbsenceRepository.GetAllIncluding(r => r.AbsenceType)
            .Where(r => r.EmployeeId == employeeId);
    }

    /// <summary>
    /// Legacy line 571 tests <c>NextTimeEvaluationFrom</c> against BOTH of its unset sentinels:
    /// <c>default(DateTime)</c> and <c>DateTimeExtension.DBDtMinDate</c>. Kept as one predicate so the two
    /// cannot drift apart.
    /// </summary>
    private static bool IsTimeEvaluationDateSet(DateTime value)
    {
        return value != default(DateTime) && value.Date != LegacyDbMinDate;
    }

    /// <summary>Legacy <c>DateTimeExtension.DBDtMinDate</c>.</summary>
    private static readonly DateTime LegacyDbMinDate = new DateTime(1900, 1, 1);

    /// <summary>Legacy <c>TimeLog.Status == 1</c> means failed.</summary>
    private const int TimeLogFailedStatus = 1;

    /// <summary>
    /// Legacy passed its <c>SysService.userSysPayrollAdmin</c> service account as the time engine's user name
    /// (legacy line 710). That account is an XAF artefact with no ABP equivalent, and the value is only ever
    /// recorded for display (<c>ExecutionLog.StartedByUserName</c>, <c>TimeLog.UserName</c>), so a constant
    /// naming the originator carries the same meaning: this time-engine run came from a payroll run, not from
    /// someone pressing the Time Engine button.
    /// </summary>
    private const string PayrollEngineOriginatorUserName = "PayrollEngine";

    /// <summary>
    /// The concurrency lock key: one payroll run per PAYROLL AREA per tenant. Normalised so that trailing
    /// spaces or a different casing cannot smuggle a second run past the guard for the same area.
    /// </summary>
    public static string BuildRunKey(string payrollAreaName)
    {
        return string.IsNullOrWhiteSpace(payrollAreaName)
            ? null
            : "PayrollArea:" + payrollAreaName.Trim().ToUpperInvariant();
    }

    /// <summary>Legacy's <c>AbsenceType.Code == "1"</c> for annual leave.</summary>
    private const string AnnualLeaveAbsenceTypeCode = "1";

    /// <summary>
    /// Legacy's XPO computed <c>Employee.CodeWithFullNameEn</c>, which this port has no equivalent of -- a
    /// documented solution-wide gap. It is <c>Concat(Code, ' ', FullNameEn)</c>, so it is reproduced literally
    /// here rather than substituting <c>Code</c>: the two autorun "Leaves not found" messages are the operator's
    /// only clue as to WHICH employee is missing a leave, and a bare code sends them to look it up.
    ///
    /// <para>Note legacy is inconsistent about this. The autorun block uses <c>CodeWithFullNameEn</c> (lines 632
    /// and 683) while its End-Of-Service messages in the same block use plain <c>Code</c> (lines 652, 661).
    /// Both are reproduced as they are.</para>
    /// </summary>
    private static string CodeWithFullNameEn(Employee employee)
    {
        return string.IsNullOrWhiteSpace(employee?.FullNameEn)
            ? employee?.Code
            : employee.Code + " " + employee.FullNameEn;
    }

    /// <summary>
    /// A copy of the run input carrying the off-cycle payment date the autorun resolved for ONE employee.
    /// Legacy reassigns a shared local instead; see <see cref="RunTimeEngineAutorunAsync"/> for why a copy is
    /// the faithful choice.
    /// </summary>
    private static PayrollEngineRunInput CloneForEmployee(PayrollEngineRunInput input, DateTime offCyclePaymentDate)
    {
        return new PayrollEngineRunInput
        {
            PayrollRunType = input.PayrollRunType,
            EmployeeCodes = input.EmployeeCodes,
            PayInDate = input.PayInDate,
            PayrollAreaName = input.PayrollAreaName,
            OffCyclePaymentDate = offCyclePaymentDate,
            OffCycleReason = input.OffCycleReason,
            RegisterTarget = input.RegisterTarget
        };
    }
}
