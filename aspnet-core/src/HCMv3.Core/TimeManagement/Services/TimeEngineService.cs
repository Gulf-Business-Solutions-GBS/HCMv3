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
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;

using HCMv3.PayrollManagement.PayrollLock;

namespace HCMv3.TimeManagement.Services;

/// <inheritdoc cref="ITimeEngineService"/>
/// <remarks>
/// Ported from legacy <c>HCM10.TimeEngine.Program.CreateTimeRegister</c> (legacy <c>Program.cs</c> lines
/// ~210-1764) plus its private helpers (<c>GetEmployeeList</c>, <c>GetEmployeeDutyPeriod</c>,
/// <c>GetEmployeeDuty</c>, <c>GetEmployeeTimePolicy</c>, <c>GetEmployeeSubGroupLeave</c>,
/// <c>GetEmployeeSubGroupGratuity</c>, <c>CalcMonthlyToleranceDeduction</c>,
/// <c>CalculateAnnualLeaveAdjust</c>, <c>AutoAirTicketPay</c>, <c>IsDependantValid</c>).
///
/// <para><b>DELIBERATE DEVIATIONS FROM LEGACY -- read before extending.</b></para>
/// <list type="number">
/// <item><description><b>XAF AuditTrailService plumbing NOT ported</b> (legacy lines ~269-289, 2272-2282,
/// 1767-1787, 2436-2451). ABP auditing via <c>FullAuditedEntity</c> is always on and needs no session
/// bracketing. <see cref="TimeEngineSettings.EnableAuditLogInTimeEngine"/> is carried but unused.</description></item>
/// <item><description><b>SysCreator/SysCreatedOn/SysModifier/SysModifiedOn NOT ported</b> -- superseded by ABP's
/// CreatorUserId/CreationTime/LastModifierUserId/LastModificationTime, applied automatically. Same decision
/// already documented on <c>AccrualService</c>.</description></item>
/// <item><description><b><c>IsSystemGenerated</c> NOT ported</b> -- no ported entity in this solution carries
/// that legacy flag.</description></item>
/// <item><description><b>ObjectSpace lifecycle replaced by batches</b> -- legacy created/committed/disposed an
/// <c>IObjectSpace</c> per employee and force-committed every 500 modified objects. Here the run is saved
/// <see cref="TimeEngineRunInput.CommitBatchSize"/> employees at a time, each batch in its own <c>RequiresNew</c>
/// unit of work (the Time Engine job uses <see cref="TimeEngineBatches.Size"/>); with 0 the whole run stays in the
/// caller's unit of work, as Go-Live Setup and the payroll engine's autorun expect. The <c>[UnitOfWork]</c>
/// attribute below matches <c>AccrualService</c>/<c>SysSettingsAccessor</c>.</description></item>
/// <item><description><b>RetroReasonLog NOT ported</b> (legacy lines ~1549-1577, <c>SysService.AddRetroReasonLog</c>).
/// No <c>RetroReasonLog</c> entity exists in this solution. The <c>EarliestMasterDataChange</c> update it
/// accompanied IS ported.</description></item>
/// <item><description><b>TimeRegister approval "Reopened vs Opened" branch -- NOW FULLY PORTED</b> (legacy
/// lines ~1473-1511). This entry previously recorded a deviation: with no <c>WfRequest</c> FK and no
/// <c>TimeLogHistory</c> entity, neither the discriminator nor the history row could be reproduced, so the
/// branch was collapsed to an unconditional <see cref="TimeRegisterStatus.Reopened"/>. Both now exist
/// (<see cref="TimeRegister.TimeLogApprovalId"/> and <see cref="TimeLogHistory"/>), and
/// <c>ApplyApprovalFieldsAsync</c> reproduces legacy exactly: history row + Reopened + FK cleared when an
/// approval batch is behind the day, plain <see cref="TimeRegisterStatus.Opened"/> when it was
/// auto-approved.</description></item>
/// <item><description><b>"ProbationDate Update" block NOT ported</b> (legacy lines ~1655-1672). It runs only
/// when <c>ProbationDaysCalculation == WorkingDays</c> and calls <c>emp.GetProbationEndDate(null, null)</c>.
/// This port's <see cref="Employee.ProbationEndDate"/> is documented as carrying the legacy CalendarDays branch
/// ONLY; the WorkingDays algorithm was never ported, so there is nothing to call. <b>Blocked, not
/// approximated.</b></description></item>
/// <item><description><b>"Employee BasicPay Record" auto-creation block NOT ported</b> (legacy lines
/// ~1140-1178). Legacy built an <c>EmployeeBasicPay</c> with child <c>EmployeeBasicPayDetail</c> rows carrying
/// only WageType + Amount. This port's <see cref="EmployeeBasicPayDetail"/> additionally requires a
/// non-nullable <c>CurrencyId</c> FK for which the legacy creation path supplies no value, and
/// <see cref="EmployeeBasicPay"/> here carries <c>Amount</c>/<c>PayType</c>/<c>PayInterval</c> directly rather
/// than legacy's detail-only shape. Inventing a currency would be a silent business-rule fabrication.
/// <b>Blocked, not approximated.</b></description></item>
/// <item><description><b>Ticket-quota and dependant-ticket-quota creation blocks NOT ported</b> (legacy lines
/// ~773-1112) -- they are entirely commented out in the legacy source. <c>AutoAirTicketPay</c>, which IS live,
/// IS ported.</description></item>
/// <item><description><b>EmployeeSubGroup resolution</b> -- see
/// <see cref="TimeEngineRunInput.EmployeeSubGroupIds"/>. <see cref="EmployeeOrgAssignment"/> in this port has
/// no <c>EmployeeSubGroupId</c> FK, so the mapping is caller-supplied (same adaptation
/// <c>IAccrualService</c> already uses). Legacy's separate "Org Assignment not found on {date}" and "Employee
/// Sub Group not found for Employee." errors are both preserved.</description></item>
/// <item><description><b>TimeSheet punch dispatch copy/paste bugs NOT reproduced.</b> Legacy's ternary chains
/// call <c>TimesheetService.GetActualIn</c> in the fall-through arm of BOTH <c>BreakIn</c> (line ~1285) and
/// <c>ActualOut</c> (line ~1293), instead of <c>GetBreakIn</c>/<c>GetActualOut</c>. That is plainly a
/// copy/paste defect, and the strategy-dispatch design cannot express it without deliberately mis-wiring the
/// interface. This port calls the correct method for each field. <b>Behavioral divergence for TimeSheet/NA
/// policies only.</b></description></item>
/// <item><description><b>Monthly forfeit is delegated</b> to
/// <see cref="IAccrualService.CalculateMonthlyForfeitAsync"/> rather than re-derived, per this port's
/// no-duplicate-business-rules rule.</description></item>
/// <item><description><b>No EF Core in HCMv3.Core</b> -- all queries use the synchronous <c>IQueryable</c> from
/// <c>IRepository&lt;T&gt;.GetAll()</c>; async signatures are kept for interface consistency. Same constraint
/// documented at <c>AccrualService.cs</c> line ~164.</description></item>
/// </list>
/// </remarks>
[UnitOfWork]
public partial class TimeEngineService : HCMv3DomainServiceBase, ITimeEngineService
{
    /// <summary>
    /// Mutable run-scoped cursor. Legacy kept <c>cutoffDate</c>/<c>OldPayrollArea</c> as locals declared
    /// OUTSIDE the employee loop and mutated inside it; C# forbids <c>ref</c> parameters on async methods, so
    /// the same "carried across iterations" semantics are expressed with this holder.
    /// </summary>
    private sealed class RunCursor
    {
        public DateTime CutoffDate;
        public int OldPayrollAreaId;
    }

    private const string TimeEngineScreenName = "HCM10.Web.UserControls.PayrollManagement.uscTimeEngine";
    private const string AnnualLeaveMaxLimitNotificationName = "ANNUALLEAVEMAXLIMITREACHED_NOTIFICATION";

    /// <summary>Legacy <c>new DateTime(9999, 12, 31)</c> open-ended quota end.</summary>
    private static readonly DateTime OpenEndedDate = new DateTime(9999, 12, 31);

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<EmployeePayrollStatus> _employeePayrollStatusRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<TimeLog> _timeLogRepository;
    private readonly IRepository<TimeLogHistory> _timeLogHistoryRepository;
    private readonly IRepository<ExecutionLog> _executionLogRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeTimePolicy> _employeeTimePolicyRepository;
    private readonly IRepository<EmployeeSubstituteDuty> _employeeSubstituteDutyRepository;
    private readonly IRepository<TimeEvent> _timeEventRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IRepository<TimeTransferType> _timeTransferTypeRepository;
    private readonly IRepository<TransType> _transTypeRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeAttendance> _employeeAttendanceRepository;
    private readonly IRepository<EmployeeOvertime> _employeeOvertimeRepository;
    private readonly IRepository<EmployeeTicket> _employeeTicketRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _employeeAbsenceQuotaRepository;
    private readonly IRepository<EmployeeTicketQuota> _employeeTicketQuotaRepository;
    private readonly IRepository<EmployeeGratuityQuota> _employeeGratuityQuotaRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeDependant> _employeeDependantRepository;
    private readonly IRepository<EmployeeDependantException> _employeeDependantExceptionRepository;
    private readonly IRepository<EmployeeBenefitException> _employeeBenefitExceptionRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<AttendanceType> _attendanceTypeRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<TicketType> _ticketTypeRepository;
    private readonly IRepository<EmployeeSubGroup> _employeeSubGroupRepository;
    private readonly IRepository<EmployeeSubGroupAbsence> _employeeSubGroupAbsenceRepository;
    private readonly IRepository<EmployeeSubGroupGratuity> _employeeSubGroupGratuityRepository;
    private readonly IRepository<EmployeeSubGroupTicket> _employeeSubGroupTicketRepository;
    private readonly IRepository<EmployeeSubGroupPayment> _employeeSubGroupPaymentRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<SystemNotification> _systemNotificationRepository;
    private readonly IRepository<DutyPeriod> _dutyPeriodRepository;
    private readonly IRepository<Duty> _dutyRepository;
    private readonly IRepository<TimePolicy> _timePolicyRepository;

    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRosterService _rosterService;
    private readonly IAccrualService _accrualService;
    private readonly ITimeRegisterFieldCalculator _fieldCalculator;
    private readonly IPunchResolutionStrategyResolver _punchStrategyResolver;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;

    /// <summary>
    /// Live progress publisher. OPTIONAL property injection (Windsor fills it when the implementation is
    /// registered), defaulting to a no-op -- see <see cref="NullEngineRunProgressWriter"/> for why this is
    /// not a constructor dependency.
    ///
    /// <para>Every call on it commits in its OWN unit of work. That is essential and not incidental: unless the
    /// run is saved in batches, this whole service runs inside one <c>[UnitOfWork]</c> that completes only when
    /// the last employee is done, so anything written through the engine's own repositories -- <c>TimeLog</c>
    /// rows included -- stays invisible to other connections until the run ends.</para>
    /// </summary>
    public IEngineRunProgressWriter ProgressWriter { get; set; } = NullEngineRunProgressWriter.Instance;

    /// <summary>
    /// Run id of the run currently executing on this instance, so helpers deep in the day loop can
    /// heartbeat without threading it through every signature. Instance state is safe -- ABP domain services
    /// are transient, so one instance serves one run.
    /// </summary>
    private long _currentEngineRunId;

    public TimeEngineService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<EmployeePayrollStatus> employeePayrollStatusRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<TimeLog> timeLogRepository,
        IRepository<TimeLogHistory> timeLogHistoryRepository,
        IRepository<ExecutionLog> executionLogRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeTimePolicy> employeeTimePolicyRepository,
        IRepository<EmployeeSubstituteDuty> employeeSubstituteDutyRepository,
        IRepository<TimeEvent> timeEventRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        IRepository<TimeTransferType> timeTransferTypeRepository,
        IRepository<TransType> transTypeRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeAttendance> employeeAttendanceRepository,
        IRepository<EmployeeOvertime> employeeOvertimeRepository,
        IRepository<EmployeeTicket> employeeTicketRepository,
        IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
        IRepository<EmployeeAbsenceQuota> employeeAbsenceQuotaRepository,
        IRepository<EmployeeTicketQuota> employeeTicketQuotaRepository,
        IRepository<EmployeeGratuityQuota> employeeGratuityQuotaRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeDependant> employeeDependantRepository,
        IRepository<EmployeeDependantException> employeeDependantExceptionRepository,
        IRepository<EmployeeBenefitException> employeeBenefitExceptionRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<AttendanceType> attendanceTypeRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<TicketType> ticketTypeRepository,
        IRepository<EmployeeSubGroup> employeeSubGroupRepository,
        IRepository<EmployeeSubGroupAbsence> employeeSubGroupAbsenceRepository,
        IRepository<EmployeeSubGroupGratuity> employeeSubGroupGratuityRepository,
        IRepository<EmployeeSubGroupTicket> employeeSubGroupTicketRepository,
        IRepository<EmployeeSubGroupPayment> employeeSubGroupPaymentRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<SystemNotification> systemNotificationRepository,
        IRepository<DutyPeriod> dutyPeriodRepository,
        IRepository<Duty> dutyRepository,
        IRepository<TimePolicy> timePolicyRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IRosterService rosterService,
        IAccrualService accrualService,
        ITimeRegisterFieldCalculator fieldCalculator,
        IPunchResolutionStrategyResolver punchStrategyResolver,
        IEmployeePayrollAreaResolver payrollAreaResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeeContractRepository = employeeContractRepository;
        _employeePayrollStatusRepository = employeePayrollStatusRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _timeLogRepository = timeLogRepository;
        _timeLogHistoryRepository = timeLogHistoryRepository;
        _executionLogRepository = executionLogRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeTimePolicyRepository = employeeTimePolicyRepository;
        _employeeSubstituteDutyRepository = employeeSubstituteDutyRepository;
        _timeEventRepository = timeEventRepository;
        _timeTransferRepository = timeTransferRepository;
        _timeTransferTypeRepository = timeTransferTypeRepository;
        _transTypeRepository = transTypeRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeAttendanceRepository = employeeAttendanceRepository;
        _employeeOvertimeRepository = employeeOvertimeRepository;
        _employeeTicketRepository = employeeTicketRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _employeeAbsenceQuotaRepository = employeeAbsenceQuotaRepository;
        _employeeTicketQuotaRepository = employeeTicketQuotaRepository;
        _employeeGratuityQuotaRepository = employeeGratuityQuotaRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeDependantRepository = employeeDependantRepository;
        _employeeDependantExceptionRepository = employeeDependantExceptionRepository;
        _employeeBenefitExceptionRepository = employeeBenefitExceptionRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _attendanceTypeRepository = attendanceTypeRepository;
        _wageTypeRepository = wageTypeRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _employeeSubGroupRepository = employeeSubGroupRepository;
        _employeeSubGroupAbsenceRepository = employeeSubGroupAbsenceRepository;
        _employeeSubGroupGratuityRepository = employeeSubGroupGratuityRepository;
        _employeeSubGroupTicketRepository = employeeSubGroupTicketRepository;
        _employeeSubGroupPaymentRepository = employeeSubGroupPaymentRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _systemNotificationRepository = systemNotificationRepository;
        _dutyPeriodRepository = dutyPeriodRepository;
        _dutyRepository = dutyRepository;
        _timePolicyRepository = timePolicyRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _rosterService = rosterService;
        _accrualService = accrualService;
        _fieldCalculator = fieldCalculator;
        _punchStrategyResolver = punchStrategyResolver;
        _payrollAreaResolver = payrollAreaResolver;
    }

    // NOTE: HCMv3.Core has no EF Core reference, so ToListAsync()/FirstOrDefaultAsync() are unavailable.
    // All queries use the synchronous IQueryable from IRepository<T>.GetAll(). See class remarks.

    /// <summary>
    /// Exempts this run's unit of work from the payroll lock, as legacy's engines mark what they write
    /// <c>IsSystemGenerated</c>. Property-injected so the tests that build this service by hand still compile;
    /// if it were ever missing, a locked area would stop the run with the lock message -- loud, not silent.
    /// </summary>
    public IPayrollLockGuard PayrollLockGuard { get; set; }

    #region << Entry point >>

    /// <summary>
    /// Set in <see cref="Exception.Data"/> on a run-level failure the engine has ALREADY written onto its
    /// <see cref="EngineRun"/> as Failed. A failure without it happened before the run began (a missing end date,
    /// another run of the same area going) and has no progress row yet -- see <c>TimeEngineBackgroundJob</c>.
    /// </summary>
    public const string RunFailureRecordedKey = "HCMv3.TimeEngine.RunFailureRecorded";

    public async Task<TimeEngineRunResult> CreateTimeRegisterAsync(TimeEngineRunInput input)
    {
        // It creates tickets for employees whose payroll area may be locked. See PayrollLockGuard.
        PayrollLockGuard?.ExemptCurrentUnitOfWork();

        // Legacy Main() validation: "No End Date defined for Time Calculation or Check Date format."
        if (input?.EndDate == null || input.EndDate.Value == default(DateTime))
        {
            throw new ArgumentException("No End Date defined for Time Calculation or Check Date format.");
        }

        // ==================================================================================================
        // ONE TIME ENGINE RUN PER PAYROLL AREA. Two runs of the same area rewrite the same employees' time
        // registers, quotas and payroll status at the same time. NOT a port -- legacy had no guard at all (see
        // TimeEngineRunKeys). The app service checks this too, so the operator is told at once; this catches the
        // race between two queued jobs. Runs made by a payroll run (or Go-Live Setup) are never refused: they run
        // inside that caller's own work, which must not fail because someone pressed Run on this screen.
        // ==================================================================================================
        var runPayrollAreaName = await ResolveRunPayrollAreaNameAsync(input);
        var runKey = TimeEngineRunKeys.ForRun(input, runPayrollAreaName);

        if (!input.IsRanFromPayrollEngine)
        {
            await EnsureNoRunInProgressAsync(runKey, runPayrollAreaName);
        }

        // Opened FIRST, before any of the (slow) settings/master-data loading, so the screen shows "Running"
        // the moment the job starts rather than minutes later. It is written on its own connection, so it is
        // visible immediately -- unlike the ExecutionLog row created a few lines below.
        var engineRunId = await ProgressWriter.BeginRunAsync(new EngineRunStartInfo
        {
            EngineType = EngineType.TimeEngine,
            RunKey = runKey,
            StartedByUserName = input.CurrentUserName,
            Parameters = BuildExecutionLogParameters(input)
        });

        _currentEngineRunId = engineRunId;

        int? executionLogId = null;
        var loop = new EmployeeLoopState { EngineRunId = engineRunId };
        TimeEngineRunResult result;

        try
        {
            var settings = await LoadSettingsAsync();
            var masterData = LoadMasterData(settings);

            executionLogId = await AddExecutionLogAsync(input);

            // Stamped BEFORE the first day is evaluated, so the run summary can be scoped to the rows this run
            // wrote. See FinalizeExecutionLogAsync for why that matters.
            loop.RunStartedAt = Clock.Now;

            // Legacy: f_timeStartDate defaults to the 1st of the current month when no start date was passed.
            loop.RunStartDate = input.StartDate == null || input.StartDate.Value == default(DateTime)
                ? new DateTime(Clock.Now.Year, Clock.Now.Month, 1, 0, 0, 0)
                : input.StartDate.Value.Date;
            loop.RunEndDate = input.EndDate.Value.Date;
            loop.Settings = settings;
            loop.MasterData = masterData;

            var employees = await GetEmployeeListAsync(input.PayrollAreaName, input.EmployeeCodes, loop.RunEndDate);

            // Legacy hoists cutoffDate/OldPayrollArea OUTSIDE the employee loop and only recomputes when the
            // payroll area changes, so consecutive employees in the same area reuse the previous value. Preserved
            // -- across batches too, since the cursor lives on the loop state, not in a batch.
            loop.Cursor = new RunCursor { CutoffDate = Clock.Now.AddDays(-1).Date, OldPayrollAreaId = 0 };
            loop.EmployeeCount = employees.Count;

            await ProgressWriter.SetTotalEmployeesAsync(engineRunId, loop.EmployeeCount);

            if (input.CommitBatchSize <= 0)
            {
                // The whole run in the caller's unit of work, exactly as before.
                await ProcessEmployeesAsync(input, employees, loop);
            }
            else
            {
                await ProcessEmployeesInBatchesAsync(input, employees, loop);
            }

            result = await FinalizeExecutionLogAsync(executionLogId.Value, loop.Aborted, loop.RunStartedAt);

            // The run is marked finished only once everything it wrote is saved: the batches already are, and the
            // execution log update above is saved here. (In one unit of work the caller saves it, as before.)
            await SaveOuterUnitOfWorkWhenBatchedAsync(input);
        }
        catch (Exception ex)
        {
            // Run-level failure (settings, master data, employee selection, a batch that would not save) as
            // opposed to the per-employee isolation in the loop. Recorded so the screen shows WHY the run stopped
            // instead of a run left "Running" until its heartbeat ages out, then rethrown -- the caller decides
            // what a failure means (the job records it and never retries; Go-Live reports it; the payroll engine
            // fails that one employee). Legacy's outer catch (Program.cs lines 1716-1729) logged it and its
            // finally still finalised the execution log; the same is done here.
            await FinalizeAfterFailureAsync(input, executionLogId, loop, ex);

            ex.Data[RunFailureRecordedKey] = true;

            await ProgressWriter.CompleteRunAsync(engineRunId, EngineRunStatus.Failed, executionLogId, FailureReason(ex));
            throw;
        }

        await ProgressWriter.CompleteRunAsync(
            engineRunId,
            ResolveEngineRunStatus(result.Aborted, Math.Max(result.Failed, loop.FailedEmployees)),
            executionLogId,
            null);

        return result;
    }

    /// <summary>
    /// The counters and fixed values the per-employee loop shares across employees and batches. Legacy kept them
    /// as locals of <c>CreateTimeRegister</c>; they live here so a batch can pick up where the previous one ended.
    /// </summary>
    private sealed class EmployeeLoopState
    {
        public long EngineRunId { get; set; }

        public DateTime RunStartedAt { get; set; }

        public DateTime RunStartDate { get; set; }

        public DateTime RunEndDate { get; set; }

        public TimeEngineSettings Settings { get; set; }

        public TimeEngineMasterData MasterData { get; set; }

        public RunCursor Cursor { get; set; }

        /// <summary>Legacy <c>NoofEmp</c>: shrinks for the skip and is written on every TimeLog row.</summary>
        public int EmployeeCount { get; set; }

        public int SequenceNo { get; set; }

        public bool Aborted { get; set; }

        /// <summary>Employees counted as failed -- an employee-level error, or any failed day.</summary>
        public int FailedEmployees { get; set; }

        /// <summary>
        /// Progress rows held back until their batch is saved (null = written at once, as in one unit of work). A
        /// batch that fails to save would otherwise leave its employees on the screen as successful when nothing
        /// of theirs was kept.
        /// </summary>
        public List<EngineRunEmployeeProgress> PendingProgress { get; set; }

        public int PendingSkips { get; set; }
    }

    /// <summary>
    /// SAVE IN BATCHES. The employees of the run, <see cref="TimeEngineRunInput.CommitBatchSize"/> at a time, each
    /// batch in its own <c>RequiresNew</c> unit of work that is completed -- saved and committed -- before the next
    /// starts. Same approach as <c>PayrollEngineService.ProcessEmployeesInBatchesAsync</c>: one
    /// <see cref="EngineRun"/> from start to finish, Stop still honoured between employees.
    ///
    /// <para>Each batch re-reads its employees and the run's master data inside its own unit of work, so nothing a
    /// batch tracks outlives it (a master-data row read in another unit of work and attached to a new row -- the
    /// extra-working attendance type is one -- would otherwise be inserted a second time). The figures are the
    /// same as in one unit of work: no employee's calculation reads anything another employee's wrote.</para>
    ///
    /// <para>Legacy committed per employee and every 500 changes (Program.cs lines 1599-1602, 1705-1711). A commit
    /// that failed escaped its employee's <c>finally</c> and ended the run through the outer catch; a batch that
    /// will not save does the same here, its employees reported as not saved.</para>
    /// </summary>
    private async Task ProcessEmployeesInBatchesAsync(
        TimeEngineRunInput input,
        List<Employee> employees,
        EmployeeLoopState loop)
    {
        var tenantId = UnitOfWorkManager.Current?.GetTenantId();

        foreach (var batch in employees.Select(e => e.Id).Chunk(input.CommitBatchSize))
        {
            if (loop.Aborted)
            {
                break;
            }

            loop.PendingProgress = new List<EngineRunEmployeeProgress>();
            loop.PendingSkips = 0;

            try
            {
                using (var uow = UnitOfWorkManager.Begin(System.Transactions.TransactionScopeOption.RequiresNew))
                using (tenantId.HasValue ? UnitOfWorkManager.Current.SetTenantId(tenantId) : null)
                {
                    PayrollLockGuard?.ExemptCurrentUnitOfWork();

                    loop.MasterData = LoadMasterData(loop.Settings);

                    var loaded = _employeeRepository.GetAll().Where(e => batch.Contains(e.Id)).ToList();
                    var batchEmployees = batch
                        .Select(id => loaded.FirstOrDefault(e => e.Id == id))
                        .Where(e => e != null)
                        .ToList();

                    await ProcessEmployeesAsync(input, batchEmployees, loop);

                    // MUST stay inside the SetTenantId scope -- see TimeEngineBackgroundJob for what happens when a
                    // unit of work saves IMustHaveTenant rows after the scope has gone.
                    await uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                var message = "Not saved: " + FailureReason(ex);
                foreach (var pending in loop.PendingProgress)
                {
                    if (pending.Succeeded)
                    {
                        pending.Succeeded = false;
                        loop.FailedEmployees = loop.FailedEmployees + 1;
                    }

                    pending.Message = message;
                }

                await FlushPendingProgressAsync(loop);
                loop.PendingProgress = null;
                throw;
            }

            await FlushPendingProgressAsync(loop);
        }

        loop.PendingProgress = null;
    }

    /// <summary>
    /// The per-employee loop of legacy <c>CreateTimeRegister</c> (Program.cs lines 263-1712), for the employees
    /// given. Sets <see cref="EmployeeLoopState.Aborted"/> and returns when a Stop is found.
    /// </summary>
    private async Task ProcessEmployeesAsync(TimeEngineRunInput input, List<Employee> employees, EmployeeLoopState loop)
    {
        foreach (var employee in employees)
        {
            // Legacy "Check TE Stopped Or Not": a TimeLog row with StopFlag aborts the whole run -- SCOPED to this
            // run's rows, which legacy achieved by truncating the table before every run. Left unscoped, one flag
            // ever set would abort every later run of every area. (Same correction as the payroll engine's.)
            if (_timeLogRepository.GetAll().Any(x => x.StopFlag && x.RunTimeStamp >= loop.RunStartedAt))
            {
                loop.Aborted = true;
                return;
            }

            // The API-initiated Stop. Read through the progress writer's OWN connection: the legacy check
            // above cannot see a StopFlag set after this run's transaction opened, and writing that flag
            // from outside would contend with the locks this transaction holds on TimeLog.
            if (await ProgressWriter.IsStopRequestedAsync(loop.EngineRunId))
            {
                loop.Aborted = true;
                return;
            }

            loop.SequenceNo = loop.SequenceNo + 1;
            var sequenceNo = loop.SequenceNo;

            // Measured, not estimated: the ETA is only as honest as this stopwatch.
            var employeeStartedAt = Clock.Now;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _currentEmployeeDayFailures = new DayFailures();

            try
            {
                var processed = await ProcessEmployeeAsync(
                    input, employee, loop.RunStartDate, loop.RunEndDate, loop.Settings, loop.MasterData, loop.Cursor,
                    loop.EmployeeCount);

                stopwatch.Stop();

                if (!processed)
                {
                    // Legacy "NoofEmp = NoofEmp - 1; continue;" -- employee skipped, not counted. It gets no
                    // grid row (it was never processed) but must shrink the remaining count or the ETA would
                    // never reach zero.
                    loop.EmployeeCount = loop.EmployeeCount - 1;
                    await RecordSkippedEmployeeAsync(loop);
                    continue;
                }

                // Legacy logged per employee PER DAY (TimeLog Status 1 for each failed day, Program.cs lines
                // 1607-1626) and its screen counted those rows. One failed day is a failure the operator must see,
                // so an employee with any failed day is a failed employee here, the first failed day's message
                // leading -- not "Successfull" with the failure buried in a log.
                if (_currentEmployeeDayFailures.Count > 0)
                {
                    loop.FailedEmployees = loop.FailedEmployees + 1;
                    await RecordEmployeeProgressAsync(
                        loop, sequenceNo, employee, false, _currentEmployeeDayFailures.Describe(), employeeStartedAt, stopwatch);
                }
                else
                {
                    await RecordEmployeeProgressAsync(
                        loop, sequenceNo, employee, true, "Successfull", employeeStartedAt, stopwatch);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Legacy per-employee catch: log and carry on, never rethrow.
                await AddTimeLogAsync(employee.Id, loop.RunStartDate, ex.Message, 1, loop.EmployeeCount);

                loop.FailedEmployees = loop.FailedEmployees + 1;
                await RecordEmployeeProgressAsync(
                    loop, sequenceNo, employee, false, ex.Message, employeeStartedAt, stopwatch);
            }
            finally
            {
                _currentEmployeeDayFailures = null;
            }
        }
    }

    /// <summary>
    /// The failed days of the employee in hand, so the employee can be reported failed once its day loop is done.
    /// Null outside an employee.
    /// </summary>
    private DayFailures _currentEmployeeDayFailures;

    /// <summary>Count of an employee's failed days and the first one, for the employee's progress message.</summary>
    private sealed class DayFailures
    {
        public int Count { get; private set; }

        public DateTime FirstDay { get; private set; }

        public string FirstMessage { get; private set; }

        public void Add(DateTime dayDate, string message)
        {
            if (Count == 0)
            {
                FirstDay = dayDate;
                FirstMessage = message;
            }

            Count = Count + 1;
        }

        public string Describe()
        {
            var first = FirstDay.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) + " : " + FirstMessage;
            return Count == 1
                ? first
                : string.Format(CultureInfo.InvariantCulture, "{0} days failed. First {1}", Count, first);
        }
    }

    /// <summary>
    /// The reason to show for a run-level failure. A save failure's own message is EF's "see the inner exception";
    /// the reason is in the innermost one, so that is added when it differs.
    /// </summary>
    private static string FailureReason(Exception ex)
    {
        var innermost = ex.GetBaseException();
        return innermost == null || ReferenceEquals(innermost, ex) || innermost.Message == ex.Message
            ? ex.Message
            : ex.Message + " " + innermost.Message;
    }

    /// <summary>Maps the run's outcome onto the operator-facing <see cref="EngineRunStatus"/>.</summary>
    private static EngineRunStatus ResolveEngineRunStatus(bool aborted, int failedCount)
    {
        if (aborted)
        {
            return EngineRunStatus.Aborted;
        }

        return failedCount > 0 ? EngineRunStatus.CompletedWithErrors : EngineRunStatus.Completed;
    }

    private async Task RecordEmployeeProgressAsync(
        EmployeeLoopState loop,
        int sequenceNo,
        Employee employee,
        bool succeeded,
        string message,
        DateTime startedAt,
        System.Diagnostics.Stopwatch stopwatch)
    {
        var progress = new EngineRunEmployeeProgress
        {
            EngineRunId = loop.EngineRunId,
            SequenceNo = sequenceNo,
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullNameEn,
            Succeeded = succeeded,
            Message = message,
            StartedAt = startedAt,
            CompletedAt = Clock.Now,
            ElapsedMilliseconds = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue)
        };

        if (loop.PendingProgress != null)
        {
            loop.PendingProgress.Add(progress);
            return;
        }

        await ProgressWriter.RecordEmployeeAsync(progress);
    }

    private async Task RecordSkippedEmployeeAsync(EmployeeLoopState loop)
    {
        if (loop.PendingProgress != null)
        {
            loop.PendingSkips = loop.PendingSkips + 1;
            return;
        }

        await ProgressWriter.RecordSkippedEmployeeAsync(loop.EngineRunId);
    }

    private async Task FlushPendingProgressAsync(EmployeeLoopState loop)
    {
        if (loop.PendingProgress == null)
        {
            return;
        }

        foreach (var progress in loop.PendingProgress)
        {
            await ProgressWriter.RecordEmployeeAsync(progress);
        }

        for (var i = 0; i < loop.PendingSkips; i++)
        {
            await ProgressWriter.RecordSkippedEmployeeAsync(loop.EngineRunId);
        }

        loop.PendingProgress.Clear();
        loop.PendingSkips = 0;
    }

    /// <summary>
    /// In batches the caller's unit of work only holds the execution log (the batches saved everything else), so it
    /// is saved before the run is marked finished. In one unit of work it is the caller's to save, as before.
    /// </summary>
    private async Task SaveOuterUnitOfWorkWhenBatchedAsync(TimeEngineRunInput input)
    {
        if (input.CommitBatchSize > 0 && UnitOfWorkManager?.Current != null)
        {
            await UnitOfWorkManager.Current.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Legacy's outer <c>catch</c> and <c>finally</c> (Program.cs lines 1716-1762, and Main's own catch at 185-199):
    /// a failed <c>TimeLog</c> row with no employee carrying the reason, then the execution log finalised -- so it
    /// reads FAILED or PARTIAL rather than staying "in process".
    /// </summary>
    private async Task FinalizeAfterFailureAsync(
        TimeEngineRunInput input, int? executionLogId, EmployeeLoopState loop, Exception failure)
    {
        try
        {
            var dayDate = loop.RunStartDate != default(DateTime) ? loop.RunStartDate : (input.StartDate ?? default(DateTime));
            await AddTimeLogAsync(null, dayDate, failure.Message, 1, loop.EmployeeCount);

            if (executionLogId.HasValue)
            {
                await FinalizeExecutionLogAsync(executionLogId.Value, loop.Aborted, loop.RunStartedAt);
            }

            await SaveOuterUnitOfWorkWhenBatchedAsync(input);
        }
        catch (Exception finalizeFailure)
        {
            // Legacy swallowed this too ("catch (Exception ex) { }"); the run's own failure is what matters.
            Logger.Warn("TimeEngine: could not finalise the execution log of a failed run.", finalizeFailure);
        }
    }

    /// <summary>
    /// The payroll area the run is filed under: the one selected, or -- for a run of named employees only -- the
    /// area of the first code's current org assignment, the same lookup the screen's checks make (legacy
    /// <c>uscTimeEngine.ascx.cs</c> lines 211-216). Null when there is none; such a run gets a key of its own and no
    /// guard. Never throws: it only files the run, and must not be able to stop one.
    /// </summary>
    private async Task<string> ResolveRunPayrollAreaNameAsync(TimeEngineRunInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            return input.PayrollAreaName.Trim();
        }

        // The payroll engine's per-employee calls always carry their area; no lookups added to every payroll employee.
        if (TimeEngineRunKeys.IsPayrollEngineAutorun(input) || string.IsNullOrWhiteSpace(input.EmployeeCodes))
        {
            return null;
        }

        try
        {
            var firstCode = input.EmployeeCodes.Split(',')
                .Select(c => c.Trim())
                .FirstOrDefault(c => c.Length > 0);
            if (firstCode == null)
            {
                return null;
            }

            var employeeId = _employeeRepository.GetAll()
                .Where(e => e.Code == firstCode)
                .Select(e => (int?)e.Id)
                .FirstOrDefault();
            if (employeeId == null)
            {
                return null;
            }

            var area = await _payrollAreaResolver.GetEmployeePayrollAreaAsync(employeeId.Value, Clock.Now.Date);
            return area?.Name;
        }
        catch (Exception ex)
        {
            Logger.Warn("TimeEngine: could not resolve the payroll area of a codes-only run; it runs unguarded.", ex);
            return null;
        }
    }

    /// <summary>ONE RUN PER PAYROLL AREA -- the check. See <see cref="CreateTimeRegisterAsync"/>.</summary>
    private async Task EnsureNoRunInProgressAsync(string runKey, string payrollAreaName)
    {
        if (string.IsNullOrWhiteSpace(runKey))
        {
            return;
        }

        var alreadyRunning = await ProgressWriter.GetActiveRunAsync(EngineType.TimeEngine, runKey);
        if (alreadyRunning != null)
        {
            throw new UserFriendlyException(string.Format(
                CultureInfo.InvariantCulture,
                "The Time Engine is already running for payroll area '{0}' (started by {1} at {2}). Wait for it to finish, or stop it, before running this area again.",
                payrollAreaName,
                string.IsNullOrWhiteSpace(alreadyRunning.StartedByUserName) ? "another user" : alreadyRunning.StartedByUserName,
                alreadyRunning.StartedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)));
        }
    }

    #endregion

    #region << Settings & master data (legacy GetInitialVariables) >>

    private async Task<TimeEngineSettings> LoadSettingsAsync()
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();
        var personnelSettings = await _sysSettingsAccessor.GetPersonnelSettingsAsync();
        var securitySettings = await _sysSettingsAccessor.GetSecuritySettingsAsync();

        return new TimeEngineSettings
        {
            IsAutoAttendanceEnabled = timeSettings.EnableAttendanceOnHoliday,
            IsHourlyLeaveLogicEnabled = timeSettings.HourlyLeavelogicEnable,
            ShouldSignInAfterHourlyLeave = timeSettings.HourlyLeaveShouldSignIn,
            ShouldSignOutBeforeHourlyLeave = timeSettings.HourlyLeaveShouldSignOut,
            IsRosterActive = timeSettings.EnableRosterDuty,
            ExcludePaidLeaveAccrualOnAbsent = timeSettings.ExcludePaidLeaveAccrualOnAbsent,
            LeaveDeductionStartMonth = timeSettings.LeaveDeductionStartMonth,
            EnableOvertimeInTimeEngine = timeSettings.EnableOvertimeInTimeEngine,
            CountWeekoffOnPH = timeSettings.CountWeekoffOnPH,
            CountRestDayOnPH = timeSettings.CountRestDayOnPH,
            ForfeitRule = timeSettings.ForfeitRule,
            EnableTimeRegisterApproval = timeSettings.EnableTimeRegisterApproval,
            AutoApproveTimeLog = timeSettings.AutoApproveTimeLog,
            EnableTimeEngineAutorun = timeSettings.EnableTimeEngineAutorun,
            UseActualTransInPlannedTimePolicy = timeSettings.UseActualTransInPlannedTimePolicy,
            EarliestEvaluationDate = timeSettings.EarliestEvaluationDate,
            IsNotReturnFromLeaveLogicEnabled = payrollSettings.EnableNotReturnFromLeaveLogic,
            FullMonthLeaveAdjForFixedDays = payrollSettings.FullMonthLeaveAdjForFixedDays,
            ProbationDaysCalculation = personnelSettings.ProbationDaysCalculation,
            EnableAuditLogInTimeEngine = securitySettings.EnableAuditLogInTimeEngine,
            AnnualLeaveMaxLimitReachedNotification = _systemNotificationRepository.GetAll()
                .Where(r => r.NotificationName == AnnualLeaveMaxLimitNotificationName)
                .Select(r => r.IsActive)
                .FirstOrDefault()
        };
    }

    private TimeEngineMasterData LoadMasterData(TimeEngineSettings settings)
    {
        var absenceTypes = _absenceTypeRepository.GetAll().ToList();
        var annualLeaveTypeId = absenceTypes.Where(r => r.Code == "1").Select(r => r.Id).FirstOrDefault();

        return new TimeEngineMasterData
        {
            AbsenceTypes = absenceTypes,
            WageTypes = _wageTypeRepository.GetAll().ToList(),
            TicketTypes = _ticketTypeRepository.GetAll().ToList(),
            EmployeeSubGroups = _employeeSubGroupRepository.GetAll().ToList(),
            EmployeeSubGroupAbsences = _employeeSubGroupAbsenceRepository.GetAll().ToList(),
            EmployeeSubGroupGratuities = _employeeSubGroupGratuityRepository.GetAll().ToList(),
            EmployeeSubGroupPayments = _employeeSubGroupPaymentRepository.GetAll().ToList(),
            AnnualLeaveTypeId = annualLeaveTypeId,
            AnnualLeave = absenceTypes.FirstOrDefault(r => r.Code == "1"),
            UnpaidLeave = absenceTypes.FirstOrDefault(r => r.Code == "2"),
            SickLeavePaid = absenceTypes.FirstOrDefault(r => r.Code == "3"),
            SickLeaveUnpaid = absenceTypes.FirstOrDefault(r => r.Code == "5"),
            AnnualLeaveTypes = absenceTypes
                .Where(r => r.QuotaType == QuotaType.AnnualLeave || r.Id == annualLeaveTypeId).ToList(),
            AirTicketTypeId = _ticketTypeRepository.GetAll().Where(r => r.Code == "1").Select(r => r.Id).FirstOrDefault(),
            TimeTransferTypeId = _timeTransferTypeRepository.GetAll().Where(r => r.Code == "1").Select(r => r.Id).FirstOrDefault(),
            ClockInTransTypeId = _transTypeRepository.GetAll().Where(r => r.Code == "1").Select(r => r.Id).FirstOrDefault(),
            ClockOutTransTypeId = _transTypeRepository.GetAll().Where(r => r.Code == "2").Select(r => r.Id).FirstOrDefault(),
            ExtraWorkingAttendanceType = _attendanceTypeRepository.GetAll().FirstOrDefault(r => r.Code == "1"),
            CalendarDaysBasicPayWageType = _wageTypeRepository.GetAll()
                .FirstOrDefault(r => r.Code == "1000" && r.BillingMethod == BillingMethod.CalendarDaysPerMonth)
        };
    }

    #endregion

    #region << Employee list (legacy GetEmployeeList) >>

    /// <summary>
    /// Ported from legacy <c>Program.GetEmployeeList</c>. Employee-code filter wins over payroll-area filter;
    /// with neither, only <see cref="EmployeeStatus.Active"/> employees are returned. Note legacy applies the
    /// Active filter ONLY in the "no filter" branch -- the code-list branch deliberately includes inactive
    /// employees (so a named exited employee can still be processed). Preserved exactly.
    /// </summary>
    private async Task<List<Employee>> GetEmployeeListAsync(string payrollAreaName, string employeeCodes, DateTime dayDate)
    {
        var query = _employeeRepository.GetAll();

        if (!string.IsNullOrEmpty(employeeCodes))
        {
            // Legacy GetEmpCodeList: split on ',' and Trim each entry.
            var codes = employeeCodes.Split(',').Select(s => s.Trim()).ToList();
            return query.Where(r => codes.Contains(r.Code)).OrderBy(r => r.Code).ToList();
        }

        if (!string.IsNullOrEmpty(payrollAreaName))
        {
            // Legacy (Program.cs lines 2548-2553): active employees with an org assignment covering the end date
            // IN THIS PAYROLL AREA (rd.PayrollArea.Name == PayrollArea). The area is matched through
            // IEmployeePayrollAreaResolver -- the assignment's own PayrollAreaId, falling back to its org unit's
            // profile for rows that predate that column -- exactly as the payroll engine selects its employees.
            //
            // BUG FIXED HERE: the area used to be looked up and then never applied -- every active employee with
            // any assignment covering the date was run, whatever payroll area the operator picked.
            var employeeIds = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName, dayDate)
                              ?? new List<int>();

            if (!employeeIds.Any())
            {
                return new List<Employee>();
            }

            return query
                .Where(r => employeeIds.Contains(r.Id) && r.Status == EmployeeStatus.Active)
                .OrderBy(r => r.Code)
                .ToList();
        }

        return query.Where(r => r.Status == EmployeeStatus.Active).OrderBy(r => r.Code).ToList();
    }

    #endregion

    #region << Execution log >>

    private async Task<int> AddExecutionLogAsync(TimeEngineRunInput input)
    {
        var log = new ExecutionLog
        {
            UserName = input.CurrentUserName,
            ActionDateTime = Clock.Now,
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.TimeEngine,
            ScreenName = TimeEngineScreenName,
            Parameters = BuildExecutionLogParameters(input),
            ExecutionStatus = ExecutionLogStatus.INPRECESS,
            TotalRecords = 0,
            Successfull = 0,
            Failed = 0
        };

        return await _executionLogRepository.InsertAndGetIdAsync(log);
    }

    /// <summary>
    /// Legacy parameter string: "Payroll Area : x | Start Date : dd/MM/yyyy | End Date : dd/MM/yyyy |
    /// Employee Code : y". Extracted so the live progress row can carry the identical text as the
    /// <c>ExecutionLog</c> row -- the operator screen shows one, reports show the other, and they must
    /// agree.
    /// </summary>
    /// <summary>
    /// The run's parameters in the execution log's words, for a caller that has to describe a run the engine
    /// refused before it began (see <c>TimeEngineBackgroundJob</c>).
    /// </summary>
    public static string DescribeRun(TimeEngineRunInput input)
    {
        return BuildExecutionLogParameters(input);
    }

    private static string BuildExecutionLogParameters(TimeEngineRunInput input)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(input.PayrollAreaName))
        {
            parts.Add("Payroll Area : " + input.PayrollAreaName);
        }

        parts.Add("Start Date : " + (input.StartDate ?? default(DateTime)).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        parts.Add("End Date : " + (input.EndDate ?? default(DateTime)).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));

        if (!string.IsNullOrEmpty(input.EmployeeCodes))
        {
            parts.Add("Employee Code : " + input.EmployeeCodes);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Ported from legacy <c>CreateTimeRegister</c>'s <c>finally</c> block. Legacy counts ALL TimeLog rows in
    /// the database (not just this run's) -- a known legacy quirk that relies on the TimeLog table being
    /// truncated before each run. Preserved as-is rather than "improved".
    /// </summary>
    /// <summary>
    /// <b>Scoped to THIS run, which legacy achieved a different way.</b> Legacy counted every
    /// <c>TimeLog</c> row in the table (legacy <c>Program.cs</c> ~1570) and got away with it because it had
    /// truncated the whole table at the start of the run -- so "every row" and "this run's rows" were the same
    /// thing. This port cannot truncate: <c>TimeLog</c> is multi-tenant, audited and soft-deletable, and other
    /// tenants' runs write to it concurrently.
    ///
    /// <para>Counting unscoped therefore inflated the totals -- every past run's rows were added to this run's
    /// summary, so the Time Engine's run history showed failure counts that grew forever and a status that
    /// drifted to PARTIAL once any historical row had failed. The <paramref name="runStartedAt"/> filter
    /// restores legacy's INTENT ("this run only") without destroying anything.</para>
    ///
    /// <para>The <c>StopFlag</c> test is scoped the same way and for the same reason: legacy's truncation meant
    /// it too only ever saw the current run.</para>
    /// </summary>
    private async Task<TimeEngineRunResult> FinalizeExecutionLogAsync(int executionLogId, bool aborted, DateTime runStartedAt)
    {
        var thisRunLogs = _timeLogRepository.GetAll().Where(r => r.RunTimeStamp >= runStartedAt);
        var summaryAll = thisRunLogs.Count();
        var summarySuccess = thisRunLogs.Count(r => r.Status == 0);
        var summaryFailed = (summaryAll - summarySuccess) > 0 ? (summaryAll - summarySuccess) : 0;

        var isAborted = aborted || thisRunLogs.Any(x => x.StopFlag);

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

        return new TimeEngineRunResult
        {
            ExecutionLogId = executionLogId,
            TotalRecords = summaryAll,
            Successfull = summarySuccess,
            Failed = summaryFailed,
            ExecutionStatus = status,
            Aborted = isAborted
        };
    }

    private async Task AddTimeLogAsync(int? employeeId, DateTime dayDate, string message, int status, int employeeCount)
    {
        await _timeLogRepository.InsertAsync(new TimeLog
        {
            EmployeeId = employeeId,
            DayDate = dayDate,
            Message = message,
            Status = status,
            RunTimeStamp = Clock.Now,
            NoOfEmployee = employeeCount
        });
    }

    #endregion
}
