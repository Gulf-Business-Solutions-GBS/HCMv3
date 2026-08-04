using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using Abp.UI;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;

namespace HCMv3.PayrollManagement.Services;

/// <inheritdoc cref="IPayrollEngineService"/>
/// <remarks>
/// <para><b>Ported from</b> legacy <c>HCM10.PayrollEngine/Program.cs</c>: <c>Main</c> (lines 154-379),
/// <c>CreateSalaryRegister</c> (380-1596), <c>RunPayrollCalculation</c> (1597-2601),
/// <c>GetInitialVariables</c> (2622-2712), <c>Allow_CalcWageType</c> (2752-2770),
/// <c>GetEmployeeList</c> (5346-5373).</para>
///
/// <para><b>DELIBERATE DEVIATIONS -- read before extending.</b></para>
/// <list type="number">
/// <item><description><b>XAF <c>IObjectSpace</c> lifecycle NOT ported.</b> Legacy created, committed and
/// disposed an object space per employee. ABP's unit of work owns this; <c>[UnitOfWork]</c> matches
/// <c>TimeEngineService</c>/<c>AccrualService</c>.</description></item>
/// <item><description><b>XAF <c>AuditTrailService</c> plumbing NOT ported</b> (legacy 758-769, 2604-2618).
/// ABP auditing via <c>FullAuditedEntity</c> is always on.
/// <see cref="PayrollEngineSettings.EnableAuditLogInPayrollEngine"/> is carried but unused -- same decision as
/// <c>TimeEngineService</c>.</description></item>
/// <item><description><b><c>SysCreator</c>/<c>SysModifier</c> NOT ported</b> -- superseded by ABP's audit
/// columns. <c>IsSystemGenerated</c> IS ported: the column was added to <c>SalaryRegisterDetail</c> by
/// migration <c>20260729111551</c> and <see cref="IPayrollRegisterStore.SaveLineAsync"/> stamps it on every
/// engine write.</description></item>
/// <item><description><b>The nested TIME ENGINE AUTORUN call is NOT made</b> (legacy 555-753 calls
/// <c>HCM10.TimeEngine.Program.Main</c> per employee, truncating <c>TimeLog</c> first). Chaining one engine
/// into another is an application/background-job orchestration concern in this port, not a domain-service
/// one, and <c>TRUNCATE TABLE</c> has no place in a domain service.
/// <see cref="PayrollEngineSettings.EnableTimeEngineAutorun"/> IS honoured everywhere it selects a
/// GATE or a QUERY (control-record arm, employee selection). <b>Reported gap.</b></description></item>
/// <item><description><b>The TIME-TYPE cumulation pre-pass IS ported</b> (legacy 1956-2518: 63
/// <c>CalculateTimeType</c> calls for wage codes 4000-4031/5000-5031) -- see
/// <see cref="ITimeTypeCumulationService"/> for the per-code reconciliation table and the eight
/// commented-out-in-legacy blocks that are deliberately absent.</description></item>
/// <item><description><b>The 2005/2011 advance-recovery CLEARING blocks are NOT ported</b> (legacy
/// 1619-1653, 1787-1851). Their predicate keys off <c>EmployeePayment.OffCyclePaymentDate</c>, a field this
/// port's <see cref="EmployeePayment"/> deliberately dropped (already documented at
/// <c>EmployeeBasicPayCalculationService.cs</c> ~line 1195). Running the predicate without it would zero
/// MORE payment rows than legacy. <b>Skipped rather than approximated</b> -- skipping only leaves a recovery
/// in place, whereas guessing destroys money. <b>Reported gap.</b></description></item>
/// <item><description><b>RetroReasonLog NOT ported</b> (legacy 1408-1411) -- no such entity exists in this
/// solution; same decision as <c>TimeEngineService</c>. The <c>EarliestMasterDataChange</c> reset it
/// accompanied IS ported.</description></item>
/// <item><description><b>No EF Core in <c>HCMv3.Core</c></b> -- every query is synchronous LINQ over
/// <c>IRepository&lt;T&gt;.GetAll()</c>; async signatures are kept for interface consistency. Same constraint
/// as <c>AccrualService.cs</c> line ~158.</description></item>
/// </list>
/// </remarks>
[UnitOfWork]
public partial class PayrollEngineService : HCMv3DomainServiceBase, IPayrollEngineService
{
    private const string PayrollEngineScreenName = "HCM10.Web.UserControls.PayrollManagement.uscPayrollEngine";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<EmployeePayrollStatus> _employeePayrollStatusRepository;
    private readonly IRepository<EmployeeOnHold> _employeeOnHoldRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<EmployeeFnFPayment> _employeeFnFPaymentRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IRepository<EmployeePayment> _employeePaymentRepository;
    private readonly IRepository<EmployeeRecurringPayment> _employeeRecurringPaymentRepository;
    private readonly IRepository<EmployeeOvertime> _employeeOvertimeRepository;
    private readonly IRepository<EmployeeAttendance> _employeeAttendanceRepository;
    private readonly IRepository<EmployeeSubGroupPayment> _employeeSubGroupPaymentRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<PayrollCalendar> _payrollCalendarRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<PayrollReasonType> _payrollReasonTypeRepository;
    private readonly IRepository<PayrollLog> _payrollLogRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<WageTypeException> _wageTypeExceptionRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<ExecutionLog> _executionLogRepository;

    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly IWageTypeCalculatorResolver _wageTypeCalculatorResolver;
    private readonly IEmployeeBasicPayCalculationService _basicPayService;
    private readonly IEmployeeLeavePayCalculationService _leavePayService;
    private readonly IPayrollRegisterStore _registerStore;
    private readonly ITimeTypeCumulationService _timeTypeCumulationService;

    // ---------------------------------------------------------------------------------------------------
    // The four category services. These were OPTIONAL property injections defaulting to a throwing Null*
    // stub while they were unported; as of wave P9 all four exist, so they are REQUIRED constructor
    // dependencies instead.
    //
    // The distinction is not cosmetic. Property injection is silent when it does not happen: the container
    // resolves PayrollEngineService perfectly happily, the constructor's Null* default survives, and the
    // engine then aborts mid-run at the first arrear wage type -- which is exactly how a validation run
    // against production data died at wage code 7000 for all five employees, with everything downstream of
    // PayrollOrder 20020 (allowances, deductions, provisions, Net Pay) never evaluated. As a constructor
    // dependency an unregistered implementation fails loudly at resolution, before any money is computed.
    //
    // Setters are retained so tests can substitute a fake, but nothing defaults to a stub any more.
    // ---------------------------------------------------------------------------------------------------

    public IPayrollArrearCalculationService ArrearService { get; set; }

    public IEmployeePaymentCalculationService PaymentService { get; set; }

    public IEmployeeOvertimePayCalculationService OvertimePayService { get; set; }

    public IEmployeeAttendancePayCalculationService AttendancePayService { get; set; }

    /// <summary>
    /// Live progress publisher. OPTIONAL property injection defaulting to a no-op -- see
    /// <see cref="NullEngineRunProgressWriter"/> for why this is not a constructor dependency.
    ///
    /// <para>Every call on it commits in its OWN unit of work, which is the only reason progress is visible
    /// while this service's single <c>[UnitOfWork]</c> is still open.</para>
    /// </summary>
    public IEngineRunProgressWriter ProgressWriter { get; set; } = NullEngineRunProgressWriter.Instance;

    public PayrollEngineService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<EmployeePayrollStatus> employeePayrollStatusRepository,
        IRepository<EmployeeOnHold> employeeOnHoldRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
        IRepository<EmployeeFnFPayment> employeeFnFPaymentRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IRepository<EmployeePayment> employeePaymentRepository,
        IRepository<EmployeeRecurringPayment> employeeRecurringPaymentRepository,
        IRepository<EmployeeOvertime> employeeOvertimeRepository,
        IRepository<EmployeeAttendance> employeeAttendanceRepository,
        IRepository<EmployeeSubGroupPayment> employeeSubGroupPaymentRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<PayrollCalendar> payrollCalendarRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<PayrollReasonType> payrollReasonTypeRepository,
        IRepository<PayrollLog> payrollLogRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<WageTypeException> wageTypeExceptionRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<ExecutionLog> executionLogRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        IWageTypeCalculatorResolver wageTypeCalculatorResolver,
        IEmployeeBasicPayCalculationService basicPayService,
        IEmployeeLeavePayCalculationService leavePayService,
        IPayrollRegisterStore registerStore,
        ITimeTypeCumulationService timeTypeCumulationService,
        IPayrollArrearCalculationService arrearService,
        IEmployeePaymentCalculationService paymentService,
        IEmployeeOvertimePayCalculationService overtimePayService,
        IEmployeeAttendancePayCalculationService attendancePayService)
    {
        _employeeRepository = employeeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeContractRepository = employeeContractRepository;
        _employeePayrollStatusRepository = employeePayrollStatusRepository;
        _employeeOnHoldRepository = employeeOnHoldRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _employeeFnFPaymentRepository = employeeFnFPaymentRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _employeePaymentRepository = employeePaymentRepository;
        _employeeRecurringPaymentRepository = employeeRecurringPaymentRepository;
        _employeeOvertimeRepository = employeeOvertimeRepository;
        _employeeAttendanceRepository = employeeAttendanceRepository;
        _employeeSubGroupPaymentRepository = employeeSubGroupPaymentRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _payrollCalendarRepository = payrollCalendarRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _payrollReasonTypeRepository = payrollReasonTypeRepository;
        _payrollLogRepository = payrollLogRepository;
        _wageTypeRepository = wageTypeRepository;
        _wageTypeExceptionRepository = wageTypeExceptionRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _executionLogRepository = executionLogRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _payrollAreaResolver = payrollAreaResolver;
        _wageTypeCalculatorResolver = wageTypeCalculatorResolver;
        _basicPayService = basicPayService;
        _leavePayService = leavePayService;
        _registerStore = registerStore;
        _timeTypeCumulationService = timeTypeCumulationService;

        ArrearService = arrearService ?? throw new ArgumentNullException(nameof(arrearService));
        PaymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        OvertimePayService = overtimePayService ?? throw new ArgumentNullException(nameof(overtimePayService));
        AttendancePayService = attendancePayService ?? throw new ArgumentNullException(nameof(attendancePayService));
    }

    #region << Run (legacy Program.Main, lines 154-379) >>

    /// <inheritdoc/>
    public async Task<PayrollEngineRunResult> RunAsync(PayrollEngineRunInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var payInDate = input.PayInDate;

        // Legacy lines 218-224.
        if (payInDate == default(DateTime))
        {
            throw new UserFriendlyException("No Pay Date defined for Payroll Calculation");
        }

        // Legacy lines 234-237. (Legacy's offCyclePaymentDate check at 227-233 is entirely commented out.)
        if (input.PayrollRunType == PayrollRunType.OffCycle && string.IsNullOrWhiteSpace(input.OffCycleReason))
        {
            throw new UserFriendlyException("Offcycle reason can not be blank.");
        }

        // Legacy lines 239-246.
        var controlRecord = _payrollControlRecordRepository.GetAll()
            .FirstOrDefault(r => r.PayrollArea != null && r.PayrollArea.Name == input.PayrollAreaName);
        if (controlRecord == null)
        {
            throw new UserFriendlyException("Payroll Control Record not found, Cannot Run Payroll Engine.");
        }

        // Legacy lines 248-255.
        var payrollCalendar = _payrollCalendarRepository.GetAll()
            .FirstOrDefault(a => a.PayrollMonth == payInDate.Month && a.PayrollYear == payInDate.Year);
        if (payrollCalendar == null)
        {
            throw new UserFriendlyException("Payroll Calender does not have record for Payroll month you are running.");
        }

        // Legacy line 259: GetInitialVariables.
        var settings = await LoadSettingsAsync();

        // Legacy lines 262-350, extracted verbatim into PayrollControlRecordGuard (see that class).
        if (input.PayrollRunType == PayrollRunType.Regular)
        {
            PayrollControlRecordGuard.EnsureRegularRunAllowed(controlRecord, payInDate);
        }
        else
        {
            payInDate = PayrollControlRecordGuard.ResolveOffCyclePayInDate(
                controlRecord,
                payInDate,
                input.OffCyclePaymentDate,
                input.EmployeeCodes,
                settings.EnableTimeEngineAutorun);
        }

        return await CreateSalaryRegisterAsync(input, payInDate, settings);
    }

    #endregion

    #region << CreateSalaryRegister (legacy lines 380-1596) >>

    /// <summary>
    /// Legacy <c>Program.CreateSalaryRegister</c>: execution log, employee selection, the per-employee loop
    /// with its isolation <c>catch</c>, and the <c>finally</c> that finalises the execution log.
    /// </summary>
    private async Task<PayrollEngineRunResult> CreateSalaryRegisterAsync(
        PayrollEngineRunInput input,
        DateTime payInDate,
        PayrollEngineSettings settings)
    {
        // Opened on its own committed transaction so the operator screen sees "Running" immediately. The
        // ExecutionLog row created on the next line cannot serve this purpose: it is written inside this
        // run's outer unit of work and stays invisible to every other connection until the run ends.
        var engineRunId = await ProgressWriter.BeginRunAsync(new EngineRunStartInfo
        {
            EngineType = EngineType.PayrollEngine,
            Parameters = BuildExecutionLogParameters(input, payInDate)
        });

        var executionLogId = await AddExecutionLogAsync(input, payInDate);

        var result = new PayrollEngineRunResult { PayInDate = payInDate };
        string runFailureMessage = null;

        try
        {
            var employees = await GetEmployeeListAsync(input, payInDate, settings);

            // Legacy line 547: payInDate is normalised to its month end before the loop.
            payInDate = PayrollControlRecordGuard.MonthEnd(payInDate);
            result.PayInDate = payInDate;

            var employeeCount = employees.Count;

            await ProgressWriter.SetTotalEmployeesAsync(engineRunId, employeeCount);

            var sequenceNo = 0;

            foreach (var employee in employees)
            {
                // Legacy lines 776-777: a PayrollLog row with StopFlag aborts the whole run. Preserved.
                if (_payrollLogRepository.GetAll().Any(x => x.StopFlag))
                {
                    result.Aborted = true;
                    break;
                }

                // The API-initiated Stop, read on the progress writer's own connection. The legacy check
                // above cannot see a flag set after this run's transaction opened, and setting that flag
                // from outside would contend with the locks this transaction holds on PayrollLog.
                if (await ProgressWriter.IsStopRequestedAsync(engineRunId))
                {
                    result.Aborted = true;
                    break;
                }

                var employeeResult = new PayrollEmployeeRunResult
                {
                    EmployeeId = employee.Id,
                    EmployeeCode = employee.Code
                };

                sequenceNo = sequenceNo + 1;

                // Measured, not estimated: the ETA is only as honest as this stopwatch.
                var employeeStartedAt = Clock.Now;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    SkipWithoutDecrement = false;
                    var processed = await ProcessEmployeeAsync(input, employee, payInDate, settings, employeeResult);

                    stopwatch.Stop();

                    if (!processed)
                    {
                        // Legacy "noofEmp = noofEmp - 1; continue;" (lines 794-795): the employee is skipped
                        // entirely and gets NO PayrollLog row at all. The two contract skips (lines 810, 822)
                        // are a bare "continue" and do NOT decrement -- see ProcessEmployeeAsync's remarks.
                        if (!SkipWithoutDecrement)
                        {
                            employeeCount = employeeCount - 1;
                        }

                        // No grid row -- the employee was never processed -- but the remaining count must
                        // shrink or the ETA would never reach zero.
                        await ProgressWriter.RecordSkippedEmployeeAsync(engineRunId);
                        continue;
                    }

                    // Legacy lines 1419-1428.
                    employeeResult.Succeeded = true;
                    employeeResult.Message = "Successfull";
                    await AddPayrollLogAsync(employee.Id, payInDate, "Successfull", 0, employeeCount);

                    await RecordEmployeeProgressAsync(
                        engineRunId, sequenceNo, employee, true, "Successfull", employeeStartedAt, stopwatch);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    // ==============================================================================
                    // PER-EMPLOYEE ERROR ISOLATION -- legacy lines 1438-1460.
                    // One bad employee (including one bad wage type inside one bad employee, wrapped by
                    // RunPayrollCalculationAsync as "Error in calculating WageType '{code} : {name}'")
                    // must NEVER abort the run. Log it against the employee and carry on.
                    // ==============================================================================
                    var message = TruncateMessage(ex.Message);

                    employeeResult.Succeeded = false;
                    employeeResult.Message = message;
                    await AddPayrollLogAsync(employee.Id, payInDate, message, 1, employeeCount);

                    await RecordEmployeeProgressAsync(
                        engineRunId, sequenceNo, employee, false, message, employeeStartedAt, stopwatch);
                }

                result.EmployeeResults.Add(employeeResult);
            }
        }
        catch (Exception ex)
        {
            // Run-level failure (employee selection, control-record resolution) as opposed to the
            // per-employee isolation above. Recorded so the screen shows WHY the run stopped instead of a
            // run that silently freezes at 40%, then rethrown -- swallowing it here would change behaviour.
            runFailureMessage = TruncateMessage(ex.Message);
            throw;
        }
        finally
        {
            // Legacy lines 1563-1595: the execution log is finalised even when the run blew up.
            await FinalizeExecutionLogAsync(executionLogId, result.Aborted);

            // In the finally too, and for the same reason: a run that dies without closing its progress row
            // would sit on the screen as "Running" until the heartbeat ages it out.
            await ProgressWriter.CompleteRunAsync(
                engineRunId,
                ResolveEngineRunStatus(runFailureMessage, result),
                executionLogId,
                runFailureMessage);
        }

        return result;
    }

    /// <summary>Maps the run's outcome onto the operator-facing <see cref="EngineRunStatus"/>.</summary>
    private static EngineRunStatus ResolveEngineRunStatus(string runFailureMessage, PayrollEngineRunResult result)
    {
        if (runFailureMessage != null)
        {
            return EngineRunStatus.Failed;
        }

        if (result.Aborted)
        {
            return EngineRunStatus.Aborted;
        }

        return result.EmployeeResults.Any(r => !r.Succeeded)
            ? EngineRunStatus.CompletedWithErrors
            : EngineRunStatus.Completed;
    }

    private async Task RecordEmployeeProgressAsync(
        long engineRunId,
        int sequenceNo,
        Employee employee,
        bool succeeded,
        string message,
        DateTime startedAt,
        System.Diagnostics.Stopwatch stopwatch)
    {
        await ProgressWriter.RecordEmployeeAsync(new EngineRunEmployeeProgress
        {
            EngineRunId = engineRunId,
            SequenceNo = sequenceNo,
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullNameEn,
            Succeeded = succeeded,
            Message = message,
            StartedAt = startedAt,
            CompletedAt = Clock.Now,
            ElapsedMilliseconds = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue)
        });
    }

    #endregion

    #region << Settings snapshot (legacy GetInitialVariables, lines 2622-2712) >>

    /// <summary>
    /// Legacy <c>Program.GetInitialVariables</c>'s settings half (lines 2624-2661), plus
    /// <c>earliestEvaluationDate</c> (line 539) and <c>AnnualLeaveOid</c> (line 2710), folded into the single
    /// <see cref="PayrollEngineSettings"/> snapshot the whole run shares. Read ONCE per run -- see that
    /// class's remarks for why re-reading mid-run is forbidden.
    ///
    /// <para>Legacy's master-data half (<c>WageTypeList</c>, <c>AbsenceTypeList</c>, the six
    /// <c>wt*WageList</c> id sets...) is NOT snapshotted here: those lists are rebuilt per employee inside
    /// <see cref="RunPayrollCalculationAsync"/>, which is where legacy rebuilds them too (lines 1918-1942).</para>
    /// </summary>
    private async Task<PayrollEngineSettings> LoadSettingsAsync()
    {
        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var securitySettings = await _sysSettingsAccessor.GetSecuritySettingsAsync();

        var settings = new PayrollEngineSettings();

        if (timeSettings != null)
        {
            settings.IsRosterActive = timeSettings.EnableRosterDuty;
            settings.ForfeitRule = timeSettings.ForfeitRule;
            settings.EnableTimeRegisterApproval = timeSettings.EnableTimeRegisterApproval;
            settings.EnableTimeEngineAutorun = timeSettings.EnableTimeEngineAutorun;
            settings.EarliestEvaluationDate = timeSettings.EarliestEvaluationDate;
        }

        if (securitySettings != null)
        {
            settings.EnableAuditLogInPayrollEngine = securitySettings.EnableAuditLogInPayrollEngine;
        }

        if (payrollSettings != null)
        {
            settings.IsPeriodwiseProvision = payrollSettings.LeaveProvisionAsPerCurrentSalary;
            settings.IsNetPayRound = payrollSettings.RoundNetPay;
            settings.ApplyAnnualLeaveNoImpactLogic = payrollSettings.ApplyAnnualleaveNoImpactLogic;
            settings.IsNotReturnFromLeaveLogicEnabled = payrollSettings.EnableNotReturnFromLeaveLogic;
            settings.GenerateRegisterOnNotReturnFromLeave = payrollSettings.GenerateRegisterOnNotReturnFromLeave;
            settings.EnableBasicPayMoreThanFixedDays = payrollSettings.EnableBasicpayMoreThenFixedDays;
            settings.EnableUnpaidDedSlabWise = payrollSettings.EnableUnpaidDedSlabWise;
            settings.DefaultWorkingHours = payrollSettings.DefaultWorkingHours;
            settings.Decimal6RoundingFrom = payrollSettings.Decimal6RoundingFrom;

            // Legacy line 2640's zero guard, applied at load time exactly as legacy does.
            settings.DecimalInSalaryRegister = payrollSettings.DecimalInSalaryRegister == 0
                ? 6
                : payrollSettings.DecimalInSalaryRegister;

            settings.FullMonthLeaveAdjForFixedDays = payrollSettings.FullMonthLeaveAdjForFixedDays;
            settings.PayLeaveAsPerCutoff = payrollSettings.PayLeaveAsPerCutoff;
            settings.FixedToActualWhenLeaveOverlapOnMonthStart = payrollSettings.FixedToActualWhenLeaveOverlapOnMonthStart;
            settings.FixedToActualWhenLeaveOverlapOnMonthEnd = payrollSettings.FixedToActualWhenLeaveOverlapOnMonthEnd;
            settings.FixedToActualWhenUnpaidLeaveFound = payrollSettings.FixedToActualWhenUpaidLeaveFound;
            settings.FixedToActualWhenAbsentDayFound = payrollSettings.FixedToActualWhenAbsentDayFound;
            settings.OvertimeHoursBasis = payrollSettings.OvertimeHoursBasis;
            settings.SocialSecurityRateRule = payrollSettings.SocialSecurityRateRule;
            settings.RunOffCycleForFutureDate = payrollSettings.RunOffCycleForFutureDate;
            settings.RegularLeaveEncashOnBasicPayOnly = payrollSettings.RegularLeaveEncashOnBasicPayOnly;
            settings.FnFLeaveEncashOnBasicPayOnly = payrollSettings.FnFLeaveEncashOnBasicPayOnly;
            settings.ChangePartialPeriodFactoringInFixedDaysBilling = payrollSettings.ChangePartialPeriodFactoringInFixedDaysBilling;
            settings.PartialPeriodCutoffDayInFixedDaysBilling = payrollSettings.PartialPeriodCutoffDayInFixedDaysBilling;
            settings.IncludeSalaryIncProvDiffInSalReg = payrollSettings.IncludeSalaryIncProvDiffInSalReg;
            settings.IncludeSalaryIncProvDiffInSalRegEffDt = payrollSettings.IncludeSalaryIncProvDiffInSalRegEffDt;
            settings.LockSalaryRegisterOnPayrollExit = payrollSettings.LockSalaryRegisterOnPayrollExit;
            settings.PickOffcyclePayment = payrollSettings.PickOffcyclePayment;
        }

        return settings;
    }

    #endregion

    #region << Execution log / payroll log (mirrors TimeEngineService) >>

    /// <summary>
    /// Legacy <c>CreateSalaryRegister</c>'s "Execution Log" region (lines 385-416), including the exact
    /// pipe-separated parameter string legacy builds.
    /// </summary>
    private async Task<int> AddExecutionLogAsync(PayrollEngineRunInput input, DateTime payInDate)
    {
        var log = new ExecutionLog
        {
            ActionDateTime = Clock.Now,
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.PayrollEngine,
            ScreenName = PayrollEngineScreenName,
            Parameters = BuildExecutionLogParameters(input, payInDate),
            ExecutionStatus = ExecutionLogStatus.INPRECESS,
            TotalRecords = 0,
            Successfull = 0,
            Failed = 0
        };

        // InsertAndGetIdAsync, never InsertAsync: the id is read back immediately by
        // FinalizeExecutionLogAsync and ABP only assigns it on flush.
        return await _executionLogRepository.InsertAndGetIdAsync(log);
    }

    /// <summary>
    /// The exact pipe-separated parameter string legacy builds (lines 385-416). Extracted so the live
    /// progress row can carry text identical to the <c>ExecutionLog</c> row.
    /// </summary>
    private static string BuildExecutionLogParameters(PayrollEngineRunInput input, DateTime payInDate)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(input.PayrollAreaName))
        {
            parts.Add("Payroll Area : " + input.PayrollAreaName);
        }

        parts.Add("Period : " + payInDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        parts.Add("Payroll RunType : " + input.PayrollRunType);

        if (!string.IsNullOrEmpty(input.OffCycleReason))
        {
            parts.Add("OffCycle Reasons : " + input.OffCycleReason);
        }

        if (input.OffCyclePaymentDate != default(DateTime))
        {
            parts.Add("OffCycle Payment Date : " + input.OffCyclePaymentDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(input.EmployeeCodes))
        {
            parts.Add("Employee Code : " + input.EmployeeCodes);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Legacy <c>CreateSalaryRegister</c>'s <c>finally</c> block (lines 1566-1593). Legacy counts ALL
    /// <see cref="PayrollLog"/> rows in the database rather than only this run's -- the same known quirk
    /// <c>TimeEngineService.FinalizeExecutionLogAsync</c> documents for <c>TimeLog</c>. Preserved as-is;
    /// "improving" it would change the reported <c>ExecutionStatus</c> of every historical run.
    ///
    /// <para>Legacy swallows any exception from this block (lines 1591-1593) so that a logging failure never
    /// fails a payroll run. Preserved.</para>
    /// </summary>
    private async Task FinalizeExecutionLogAsync(int executionLogId, bool aborted)
    {
        try
        {
            var allLogs = _payrollLogRepository.GetAll();
            var summaryAll = allLogs.Count();
            var summarySuccess = allLogs.Count(r => r.Status == 0);
            var summaryFailed = (summaryAll - summarySuccess) > 0 ? (summaryAll - summarySuccess) : 0;

            var isAborted = aborted || _payrollLogRepository.GetAll().Any(x => x.StopFlag);

            var status = isAborted ? ExecutionLogStatus.ABORTED
                : summaryAll == summarySuccess ? ExecutionLogStatus.SUCCESS
                : summaryAll == summaryFailed ? ExecutionLogStatus.FAILED
                : ExecutionLogStatus.PARTIAL;

            var log = await _executionLogRepository.FirstOrDefaultAsync(executionLogId);
            if (log != null)
            {
                log.ExecutionStatus = status;
                log.TotalRecords = summaryAll;
                log.Successfull = summarySuccess;
                log.Failed = summaryFailed;
                await _executionLogRepository.UpdateAsync(log);
            }
        }
        catch (Exception)
        {
            // Legacy lines 1591-1593: swallowed.
        }
    }

    private async Task AddPayrollLogAsync(int? employeeId, DateTime inPeriod, string message, int status, int employeeCount)
    {
        await _payrollLogRepository.InsertAsync(new PayrollLog
        {
            EmployeeId = employeeId,
            InPeriod = inPeriod,
            Message = message,
            Status = status,
            RunTimeStamp = Clock.Now,
            NoOfEmployee = employeeCount
        });
    }

    /// <summary>Legacy truncates the logged message to 1000 characters (lines 735, 1447). Preserved.</summary>
    private static string TruncateMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        return message.Length > 1000 ? message.Substring(0, 1000) : message;
    }

    #endregion

    // NullPayrollCategoryService (the throwing constructor-time fallback for the four category services) was
    // removed in wave P9 when all four were ported and promoted to REQUIRED constructor dependencies.
    //
    // It had been retained as a "detector" for conventional-registration drift, on the reasoning that a
    // missing registration would leave the stub in place and fail loudly per employee. Constructor injection
    // supersedes that role and is strictly stronger: an unregistered implementation now fails at container
    // RESOLUTION, before a single employee is processed, rather than part-way through a run after some
    // registers have already been written. PayrollCategoryServiceRegistration_Tests still guards the
    // registration itself.
}
