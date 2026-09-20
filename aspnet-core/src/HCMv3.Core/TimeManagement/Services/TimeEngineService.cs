using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
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
/// <item><description><b>ObjectSpace lifecycle NOT ported</b> -- legacy created/committed/disposed an
/// <c>IObjectSpace</c> per employee and force-committed every 500 modified objects. ABP's unit of work owns
/// this; the <c>[UnitOfWork]</c> attribute below matches <c>AccrualService</c>/<c>SysSettingsAccessor</c>.</description></item>
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

    /// <summary>
    /// Live progress publisher. OPTIONAL property injection (Windsor fills it when the implementation is
    /// registered), defaulting to a no-op -- see <see cref="NullEngineRunProgressWriter"/> for why this is
    /// not a constructor dependency.
    ///
    /// <para>Every call on it commits in its OWN unit of work. That is essential and not incidental: this
    /// whole service runs inside one <c>[UnitOfWork]</c> that completes only when the last employee is done,
    /// so anything written through the engine's own repositories -- <c>TimeLog</c> rows included -- stays
    /// invisible to other connections until the run ends.</para>
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
        IPunchResolutionStrategyResolver punchStrategyResolver)
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

    public async Task<TimeEngineRunResult> CreateTimeRegisterAsync(TimeEngineRunInput input)
    {
        // It creates tickets for employees whose payroll area may be locked. See PayrollLockGuard.
        PayrollLockGuard?.ExemptCurrentUnitOfWork();

        // Legacy Main() validation: "No End Date defined for Time Calculation or Check Date format."
        if (input?.EndDate == null || input.EndDate.Value == default(DateTime))
        {
            throw new ArgumentException("No End Date defined for Time Calculation or Check Date format.");
        }

        // Opened FIRST, before any of the (slow) settings/master-data loading, so the screen shows "Running"
        // the moment the job starts rather than minutes later. It is written on its own connection, so it is
        // visible immediately -- unlike the ExecutionLog row created a few lines below.
        var engineRunId = await ProgressWriter.BeginRunAsync(new EngineRunStartInfo
        {
            EngineType = EngineType.TimeEngine,
            StartedByUserName = input.CurrentUserName,
            Parameters = BuildExecutionLogParameters(input)
        });

        _currentEngineRunId = engineRunId;

        var settings = await LoadSettingsAsync();
        var masterData = LoadMasterData(settings);

        var executionLogId = await AddExecutionLogAsync(input);

        // Stamped BEFORE the first day is evaluated, so the run summary can be scoped to the rows this run
        // wrote. See FinalizeExecutionLogAsync for why that matters.
        var runStartedAt = Clock.Now;

        // Legacy: f_timeStartDate defaults to the 1st of the current month when no start date was passed.
        var runStartDate = input.StartDate == null || input.StartDate.Value == default(DateTime)
            ? new DateTime(Clock.Now.Year, Clock.Now.Month, 1, 0, 0, 0)
            : input.StartDate.Value.Date;
        var runEndDate = input.EndDate.Value.Date;

        var employees = GetEmployeeList(input.PayrollAreaName, input.EmployeeCodes, runEndDate);

        // Legacy hoists cutoffDate/OldPayrollArea OUTSIDE the employee loop and only recomputes when the
        // payroll area changes, so consecutive employees in the same area reuse the previous value. Preserved.
        var cursor = new RunCursor { CutoffDate = Clock.Now.AddDays(-1).Date, OldPayrollAreaId = 0 };

        var employeeCount = employees.Count;
        var aborted = false;

        await ProgressWriter.SetTotalEmployeesAsync(engineRunId, employeeCount);

        var sequenceNo = 0;

        foreach (var employee in employees)
        {
            // Legacy "Check TE Stopped Or Not": a TimeLog row with StopFlag aborts the whole run. Preserved.
            if (_timeLogRepository.GetAll().Any(x => x.StopFlag))
            {
                aborted = true;
                break;
            }

            // The API-initiated Stop. Read through the progress writer's OWN connection: the legacy check
            // above cannot see a StopFlag set after this run's transaction opened, and writing that flag
            // from outside would contend with the locks this transaction holds on TimeLog.
            if (await ProgressWriter.IsStopRequestedAsync(engineRunId))
            {
                aborted = true;
                break;
            }

            sequenceNo = sequenceNo + 1;

            // Measured, not estimated: the ETA is only as honest as this stopwatch.
            var employeeStartedAt = Clock.Now;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var processed = await ProcessEmployeeAsync(
                    input, employee, runStartDate, runEndDate, settings, masterData, cursor, employeeCount);

                stopwatch.Stop();

                if (!processed)
                {
                    // Legacy "NoofEmp = NoofEmp - 1; continue;" -- employee skipped, not counted. It gets no
                    // grid row (it was never processed) but must shrink the remaining count or the ETA would
                    // never reach zero.
                    employeeCount = employeeCount - 1;
                    await ProgressWriter.RecordSkippedEmployeeAsync(engineRunId);
                    continue;
                }

                await RecordEmployeeProgressAsync(
                    engineRunId, sequenceNo, employee, true, "Successfull", employeeStartedAt, stopwatch);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Legacy per-employee catch: log and carry on, never rethrow.
                await AddTimeLogAsync(employee.Id, runStartDate, ex.Message, 1, employeeCount);

                await RecordEmployeeProgressAsync(
                    engineRunId, sequenceNo, employee, false, ex.Message, employeeStartedAt, stopwatch);
            }
        }

        var result = await FinalizeExecutionLogAsync(executionLogId, aborted, runStartedAt);

        await ProgressWriter.CompleteRunAsync(
            engineRunId,
            ResolveEngineRunStatus(result.Aborted, result.Failed),
            executionLogId,
            null);

        return result;
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
    private List<Employee> GetEmployeeList(string payrollAreaName, string employeeCodes, DateTime dayDate)
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
            // Legacy navigates r.OrgAssignment (a bidirectional XPO collection this port does not have) and
            // filters on rd.PayrollArea.Name. EmployeeOrgAssignment here has no PayrollArea FK either, so the
            // area is resolved through PayrollArea.Name -> PayrollControlRecord is NOT usable; instead the
            // org-assignment date window is applied and the payroll area matched by name via PayrollArea.
            var payrollAreaIds = _payrollAreaRepository.GetAll()
                .Where(r => r.Name == payrollAreaName).Select(r => r.Id).ToList();

            if (!payrollAreaIds.Any())
            {
                return new List<Employee>();
            }

            var employeeIds = _employeeOrgAssignmentRepository.GetAll()
                .Where(r => r.StartDate <= dayDate && r.EndDate >= dayDate)
                .Select(r => r.EmployeeId)
                .Distinct()
                .ToList();

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
    private static string BuildExecutionLogParameters(TimeEngineRunInput input)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(input.PayrollAreaName))
        {
            parts.Add("Payroll Area : " + input.PayrollAreaName);
        }

        parts.Add("Start Date : " + (input.StartDate ?? default(DateTime)).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        parts.Add("End Date : " + input.EndDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));

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
