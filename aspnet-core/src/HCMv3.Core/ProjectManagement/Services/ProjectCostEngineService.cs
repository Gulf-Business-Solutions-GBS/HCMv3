using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using Abp.UI;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Services;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;

namespace HCMv3.ProjectManagement.Services;

/// <inheritdoc cref="IProjectCostEngineService"/>
[UnitOfWork]
public partial class ProjectCostEngineService : HCMv3DomainServiceBase, IProjectCostEngineService
{
    /// <summary>
    /// Preserved verbatim on the <see cref="ExecutionLog"/> row (legacy line 328) so existing
    /// execution-log reports keep working.
    /// </summary>
    private const string ProjectCostEngineScreenName = "HCM10.Web.UserControls.ProjectManagement.uscProjectCosting";

    /// <summary>Legacy static field <c>endOfServiceOffcycle</c> (line 34).</summary>
    internal const string EndOfServiceOffCycleReason = "End Of Service";

    /// <summary>
    /// The progress-row key of every Project Cost Engine run: ONE RUN AT A TIME per tenant, not per payroll area.
    /// Each run starts by clearing the whole <see cref="ProjectLog"/> table (legacy <c>TruncateLog</c> on the Run
    /// button), so a second run of another area would wipe the first one's log and stop flags.
    /// </summary>
    public const string RunKey = "ProjectCostEngine";

    /// <summary>
    /// Set in <see cref="Exception.Data"/> on a run-level failure the engine has ALREADY written onto its
    /// <see cref="EngineRun"/> as Failed. A failure without it happened before the run's progress row was opened
    /// (a refused pay date, control record or calendar, an off-cycle run) -- see
    /// <c>ProjectCostEngineBackgroundJob</c>.
    /// </summary>
    public const string RunFailureRecordedKey = "HCMv3.ProjectCostEngine.RunFailureRecorded";

    /// <summary>Refusal for an off-cycle run. Legacy hid the run type (always Regular) and had no off-cycle arm.</summary>
    public const string OffCycleNotSupportedMessage =
        "The Project Cost Engine runs Regular payroll only; an off-cycle run is not supported.";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<EmployeePayrollStatus> _employeePayrollStatusRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeOvertime> _employeeOvertimeRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<PayrollCalendar> _payrollCalendarRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<TicketType> _ticketTypeRepository;
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<ExecutionLog> _executionLogRepository;

    private readonly IRepository<ProjectLog> _projectLogRepository;
    private readonly IRepository<ProjectRegister> _projectRegisterRepository;
    private readonly IRepository<ProjectRegisterDetail> _projectRegisterDetailRepository;
    private readonly IRepository<EmployeeProject> _employeeProjectRepository;
    private readonly IRepository<EmployeeProjectDetail> _employeeProjectDetailRepository;
    private readonly IRepository<ProjectWageType> _projectWageTypeRepository;

    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IPayrollRoundingService _payrollRoundingService;
    private readonly IProjectCostOvertimeWageTypeResolver _overtimeWageTypeResolver;

    /// <summary>
    /// Live progress publisher, the same shared mechanism the Time and Payroll engines report through. OPTIONAL
    /// property injection defaulting to a no-op -- see <see cref="NullEngineRunProgressWriter"/> for why it is not
    /// a constructor dependency. Every call on it commits in its OWN unit of work.
    /// </summary>
    public IEngineRunProgressWriter ProgressWriter { get; set; } = NullEngineRunProgressWriter.Instance;

    public ProjectCostEngineService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeePayrollStatus> employeePayrollStatusRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeOvertime> employeeOvertimeRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<PayrollCalendar> payrollCalendarRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<TicketType> ticketTypeRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<ExecutionLog> executionLogRepository,
        IRepository<ProjectLog> projectLogRepository,
        IRepository<ProjectRegister> projectRegisterRepository,
        IRepository<ProjectRegisterDetail> projectRegisterDetailRepository,
        IRepository<EmployeeProject> employeeProjectRepository,
        IRepository<EmployeeProjectDetail> employeeProjectDetailRepository,
        IRepository<ProjectWageType> projectWageTypeRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IPayrollRoundingService payrollRoundingService,
        IProjectCostOvertimeWageTypeResolver overtimeWageTypeResolver)
    {
        _employeeRepository = employeeRepository;
        _employeeContractRepository = employeeContractRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _employeePayrollStatusRepository = employeePayrollStatusRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeOvertimeRepository = employeeOvertimeRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _payrollCalendarRepository = payrollCalendarRepository;
        _wageTypeRepository = wageTypeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _timeRegisterRepository = timeRegisterRepository;
        _executionLogRepository = executionLogRepository;
        _projectLogRepository = projectLogRepository;
        _projectRegisterRepository = projectRegisterRepository;
        _projectRegisterDetailRepository = projectRegisterDetailRepository;
        _employeeProjectRepository = employeeProjectRepository;
        _employeeProjectDetailRepository = employeeProjectDetailRepository;
        _projectWageTypeRepository = projectWageTypeRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _payrollRoundingService = payrollRoundingService;
        _overtimeWageTypeResolver = overtimeWageTypeResolver;
    }

    #region << Run (legacy uscProjectCosting.Main, lines 155-296) >>

    /// <inheritdoc/>
    public async Task<ProjectCostEngineRunResult> RunAsync(ProjectCostEngineRunInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        // Captured once: the batches and the failure row run in units of work of their own, which must write
        // into the same tenant as the run.
        var tenantId = UnitOfWorkManager?.Current?.GetTenantId();

        // ==============================================================================================
        // ONE RUN AT A TIME. Checked BEFORE the log is cleared and outside the failure-row catch below: a refusal
        // must neither wipe the running run's log nor add a failed row to it (that row would be counted as a
        // failed employee when the running run finalises its ExecutionLog). The app service checks first so the
        // operator is told at once; this catches two jobs queued together. Fails OPEN, as the Payroll Engine's.
        // ==============================================================================================
        await EnsureNoRunInProgressAsync();

        // Legacy ASPxCallbackPanel1_Callback "btnCalculateProjectCosting" (lines 1514-1521): TruncateLog, THEN
        // the run. Each run therefore starts with an empty ProjectLog; the history of earlier runs is kept on
        // their EngineRun progress rows and ExecutionLog rows, which are not touched.
        await ClearProjectLogForNewRunAsync(tenantId);

        try
        {
            return await RunCoreAsync(input, tenantId);
        }
        catch (Exception ex)
        {
            // ==========================================================================================
            // WHOLE-RUN FAILURE LOG -- legacy lines 266-282 (Main's catch, which also receives everything
            // CreateProjectRegister re-throws). Recorded as an EMPLOYEE-LESS ProjectLog row in its OWN unit of
            // work, so it survives whatever the run's own unit of work does with the failure.
            //
            // DELIBERATE DEVIATION kept from wave P8: legacy SWALLOWED the exception after logging; the engine
            // re-throws so its caller knows. The background job is that caller and it never re-throws (a retried
            // job would be a second run), so end to end the run still "fails quietly" as legacy's did.
            // ==========================================================================================
            await AddRunFailureProjectLogAsync(input.PayInDate, ex, tenantId);
            throw;
        }
    }

    private async Task<ProjectCostEngineRunResult> RunCoreAsync(ProjectCostEngineRunInput input, int? tenantId)
    {
        var payInDate = input.PayInDate;

        // Legacy lines 207-208.
        if (payInDate == default(DateTime))
        {
            throw new UserFriendlyException("No Pay Date defined for Payroll Calculation");
        }

        // Legacy lines 211-212.
        if (input.PayrollRunType == PayrollRunType.OffCycle && input.OffCyclePaymentDate == default(DateTime))
        {
            throw new UserFriendlyException("No Offcycle Payment Date selected for Offcycle Payroll.");
        }

        // Legacy line 216: GetInitialVariables.
        var settings = await LoadSettingsAsync();

        // NOT a reported gap. Wave P8 restored OvertimeType.WageTypeId and
        // ProjectCostOvertimeWageTypeResolver hardcodes IsAvailable => true, so this cannot fire in
        // production. It survives only to catch a TEST DOUBLE that reports the mapping as unavailable,
        // because such a run silently pro-rates overtime wage types by working days instead of costing
        // them by hours -- right-looking numbers that are wrong, which is the worst way to be wrong.
        if (!_overtimeWageTypeResolver.IsAvailable)
        {
            Logger.Warn(
                "ProjectCostEngine: the OvertimeType->WageType mapping is unavailable from this resolver, "
                + "so wage types legacy costs by overtime hours (uscProjectCosting.ascx.cs lines 809-818) "
                + "are being pro-rated by working days instead. In production this indicates a container "
                + "misconfiguration, not a porting gap -- the real resolver is always available.");
        }

        // Legacy line 223.
        var controlRecord = _payrollControlRecordRepository.GetAll()
            .FirstOrDefault(x => (x.PayrollArea != null ? x.PayrollArea.Name : "") == input.PayrollAreaName);

        if (controlRecord == null)
        {
            // Legacy line 260.
            throw new UserFriendlyException("Payroll Control Record not found, Cannot Run Project Cost Engine.");
        }

        // Legacy lines 226-231.
        var payrollCalendar = _payrollCalendarRepository.GetAll()
            .FirstOrDefault(a => a.PayrollMonth == payInDate.Month && a.PayrollYear == payInDate.Year);

        if (payrollCalendar == null)
        {
            throw new UserFriendlyException("Payroll Calender does not have record for Payroll month you are running.");
        }

        // Legacy lines 232-250: only a REGULAR run does anything. Legacy has no off-cycle arm at all -- and its
        // screen hid the run type, so it always sent Regular. An off-cycle run that reaches here is refused
        // instead of returning an empty, apparently successful run.
        if (input.PayrollRunType != PayrollRunType.Regular)
        {
            throw new UserFriendlyException(OffCycleNotSupportedMessage);
        }

        // Legacy line 235. Legacy compared `PeriodLocked == PeriodLocked.Yes`; this port's
        // PayrollControlRecord.PeriodLocked is a bool.
        if (controlRecord.CalendarMonthEnd.Date != payInDate.Date || !controlRecord.PeriodLocked)
        {
            throw new UserFriendlyException("Payroll Control Record not locked or is not current.");
        }

        return await CreateProjectRegisterAsync(input, payInDate, settings, tenantId);
    }

    #endregion

    #region << CreateProjectRegister (legacy lines 297-654) >>

    private async Task<ProjectCostEngineRunResult> CreateProjectRegisterAsync(
        ProjectCostEngineRunInput input,
        DateTime payInDate,
        ProjectCostEngineSettings settings,
        int? tenantId)
    {
        // Opened on its own committed transaction, so the screen sees "Running" at once.
        var engineRunId = await ProgressWriter.BeginRunAsync(new EngineRunStartInfo
        {
            EngineType = EngineType.ProjectCostEngine,
            RunKey = RunKey,
            StartedByUserName = input.CurrentUserName,
            Parameters = BuildExecutionLogParameters(input, payInDate)
        });

        var executionLogId = await AddExecutionLogAsync(input, payInDate);

        var result = new ProjectCostEngineRunResult { PayInDate = payInDate };
        string runFailureMessage = null;

        try
        {
            // Legacy line 339.
            var employees = GetEmployeeList(input.PayrollAreaName, input.EmployeeCodes, payInDate);

            // Legacy line 347: payInDate is normalised to its month end (time zeroed) before the loop.
            payInDate = MonthEnd(payInDate);
            result.PayInDate = payInDate;

            var loop = new EmployeeLoopState
            {
                EngineRunId = engineRunId,
                PayInDate = payInDate,
                Settings = settings,
                TenantId = tenantId,
                // Legacy line 353: noofEmp, decremented by every skip below.
                EmployeeCount = employees.Count,
                // Legacy `empCode == ""` (lines 386, 403, 418): typed codes turn a skip into an error.
                EmployeeCodesTyped = HasTypedEmployeeCodes(input.EmployeeCodes)
            };

            await ProgressWriter.SetTotalEmployeesAsync(engineRunId, loop.EmployeeCount);

            if (input.CommitBatchSize <= 0)
            {
                await ProcessEmployeesAsync(input, employees, loop, result);
            }
            else
            {
                await ProcessEmployeesInBatchesAsync(input, employees, loop, result);
            }
        }
        catch (Exception ex)
        {
            // Run-level failure (employee selection, a batch that could not be saved) as opposed to the
            // per-employee isolation below. Batches already saved stay saved; the progress row is closed as
            // Failed with the reason in the finally, and the caller is told not to record a second one.
            runFailureMessage = TruncateMessage(ex.Message);
            ex.Data[RunFailureRecordedKey] = true;
            throw;
        }
        finally
        {
            // Legacy lines 621-653: the execution log is finalised even when the run blew up.
            await FinalizeExecutionLogAsync(executionLogId, result.Aborted);

            await ProgressWriter.CompleteRunAsync(
                engineRunId,
                ResolveEngineRunStatus(runFailureMessage, result),
                executionLogId,
                runFailureMessage);
        }

        return result;
    }

    /// <summary>The counters and fixed values the per-employee loop shares across employees and batches.</summary>
    private sealed class EmployeeLoopState
    {
        public long EngineRunId { get; set; }

        public DateTime PayInDate { get; set; }

        public ProjectCostEngineSettings Settings { get; set; }

        public int? TenantId { get; set; }

        /// <summary>Legacy <c>noofEmp</c>: written on every ProjectLog row, decremented by each skip.</summary>
        public int EmployeeCount { get; set; }

        public bool EmployeeCodesTyped { get; set; }

        public int SequenceNo { get; set; }
    }

    /// <summary>
    /// SAVE IN BATCHES. The run's employees, <see cref="ProjectCostEngineRunInput.CommitBatchSize"/> at a time, each
    /// batch in its own <c>RequiresNew</c> unit of work that is completed -- saved and committed -- before the next
    /// starts. Same shape as <c>PayrollEngineService.ProcessEmployeesInBatchesAsync</c> (Go-Live Setup's IT11 fix):
    /// the change tracker no longer grows with every employee, a late failure no longer rolls back everyone already
    /// done, and Stop is still honoured between employees.
    ///
    /// <para>Legacy saved per employee (a fresh object space each, lines 357-613); a batch of ten is coarser, so an
    /// employee's rows become visible with its batch rather than on its own. No employee's costing reads another
    /// employee's rows, so the figures are the same.</para>
    /// </summary>
    private async Task ProcessEmployeesInBatchesAsync(
        ProjectCostEngineRunInput input,
        List<Employee> employees,
        EmployeeLoopState loop,
        ProjectCostEngineRunResult result)
    {
        foreach (var batch in employees.Select(e => e.Id).Chunk(input.CommitBatchSize))
        {
            if (result.Aborted)
            {
                break;
            }

            using (var uow = UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
            using (loop.TenantId.HasValue ? UnitOfWorkManager.Current.SetTenantId(loop.TenantId) : null)
            {
                // Each employee is re-read inside its batch, so nothing a batch tracks outlives it.
                var loaded = _employeeRepository.GetAll().Where(e => batch.Contains(e.Id)).ToList();
                var batchEmployees = batch
                    .Select(id => loaded.FirstOrDefault(e => e.Id == id))
                    .Where(e => e != null)
                    .ToList();

                await ProcessEmployeesAsync(input, batchEmployees, loop, result);

                // MUST stay inside the SetTenantId scope -- see ProjectCostEngineBackgroundJob.
                await uow.CompleteAsync();
            }
        }
    }

    /// <summary>
    /// The per-employee loop of legacy <c>CreateProjectRegister</c> (lines 355-614), for the employees given. Sets
    /// <see cref="ProjectCostEngineRunResult.Aborted"/> and returns when a Stop is found.
    /// </summary>
    private async Task ProcessEmployeesAsync(
        ProjectCostEngineRunInput input,
        List<Employee> employees,
        EmployeeLoopState loop,
        ProjectCostEngineRunResult result)
    {
        foreach (var emp in employees)
        {
            // Legacy lines 374-379, the FIRST thing done for each employee: any ProjectLog row with StopFlag
            // breaks the loop. Every run starts with an empty log, so this sees only flags set during this run.
            if (_projectLogRepository.GetAll().Any(x => x.StopFlag))
            {
                result.Aborted = true;
                return;
            }

            // The screen's Stop, set on THIS run's progress row and read on the writer's own connection -- the
            // check above cannot see a flag written after this unit of work began.
            if (await ProgressWriter.IsStopRequestedAsync(loop.EngineRunId))
            {
                result.Aborted = true;
                return;
            }

            var employeeResult = new ProjectCostEmployeeRunResult
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.Code
            };

            loop.SequenceNo = loop.SequenceNo + 1;
            var sequenceNo = loop.SequenceNo;

            // Measured, not estimated: the screen's average and ETA are only as honest as this stopwatch.
            var employeeStartedAt = Clock.Now;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var outcome = await ProcessEmployeeAsync(input, emp, loop.PayInDate, loop.Settings, loop.EmployeeCodesTyped);

                stopwatch.Stop();

                if (outcome == EmployeeRunOutcome.Skipped)
                {
                    // Legacy "noofEmp = noofEmp - 1; continue;" (lines 388, 405, 420): no ProjectLog row at all.
                    loop.EmployeeCount = loop.EmployeeCount - 1;
                    await ProgressWriter.RecordSkippedEmployeeAsync(loop.EngineRunId);
                    continue;
                }

                // Legacy lines 565-579.
                employeeResult.Succeeded = true;
                employeeResult.Message = "Successfull";
                await AddProjectLogAsync(emp.Id, loop.PayInDate, "Successfull", 0, loop.EmployeeCount, loop.TenantId);

                await RecordEmployeeProgressAsync(loop.EngineRunId, sequenceNo, emp, true, "Successfull", employeeStartedAt, stopwatch);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // ==========================================================================
                // PER-EMPLOYEE ERROR ISOLATION -- legacy lines 581-605. One bad employee must
                // never abort the run.
                // ==========================================================================
                var message = TruncateMessage(ex.Message);

                employeeResult.Succeeded = false;
                employeeResult.Message = message;
                await AddProjectLogAsync(emp.Id, loop.PayInDate, message, 1, loop.EmployeeCount, loop.TenantId);

                await RecordEmployeeProgressAsync(loop.EngineRunId, sequenceNo, emp, false, message, employeeStartedAt, stopwatch);
            }

            result.EmployeeResults.Add(employeeResult);
        }
    }

    private enum EmployeeRunOutcome
    {
        Processed,
        Skipped
    }

    /// <summary>
    /// Legacy's per-employee body (lines 381-563): validations, then the Regular-run register creation.
    /// </summary>
    /// <param name="employeeCodesTyped">
    /// Legacy <c>empCode != ""</c>. With codes typed, an employee who would be skipped is an ERROR instead -- the
    /// operator asked for that employee by name (legacy 27 July 2026, lines 381-426).
    /// </param>
    private async Task<EmployeeRunOutcome> ProcessEmployeeAsync(
        ProjectCostEngineRunInput input,
        Employee emp,
        DateTime payInDate,
        ProjectCostEngineSettings settings,
        bool employeeCodesTyped)
    {
        // Legacy lines 381-394: Check JoiningDate. payInDate is already the month end here (line 347).
        if (emp.JoiningDate == default(DateTime))
        {
            throw new UserFriendlyException("JoiningDate not found.");
        }

        if (emp.JoiningDate > payInDate)
        {
            if (!employeeCodesTyped)
            {
                return EmployeeRunOutcome.Skipped;
            }

            throw new UserFriendlyException(string.Format(
                "JoiningDate : '{0}' > PayInDate : {1}.",
                FormatDate(emp.JoiningDate),
                FormatDate(payInDate)));
        }

        // Legacy lines 396-410: the FIRST contract (earliest start) must have started by the period end.
        var empContractStart = _employeeContractRepository.GetAll()
            .Where(r => r.EmployeeId == emp.Id)
            .OrderBy(r => r.StartDate)
            .Select(r => new { r.StartDate, r.EndDate })
            .FirstOrDefault();

        if (empContractStart == null)
        {
            throw new UserFriendlyException(string.Format("Employee Contract not found for Employee {0}", emp.Code));
        }

        if (empContractStart.StartDate > payInDate)
        {
            if (!employeeCodesTyped)
            {
                return EmployeeRunOutcome.Skipped;
            }

            throw new UserFriendlyException(string.Format(
                "Contract Started on '{0}'. Can't run Payroll for Date : '{1}'",
                FormatDate(empContractStart.StartDate),
                FormatDate(payInDate)));
        }

        // Legacy lines 412-425: the LAST contract (latest end) must not have ended before the period starts,
        // unless the run is an End Of Service off-cycle.
        var empContract = _employeeContractRepository.GetAll()
            .Where(r => r.EmployeeId == emp.Id)
            .OrderByDescending(r => r.EndDate)
            .Select(r => new { r.StartDate, r.EndDate })
            .FirstOrDefault();

        if (empContract == null)
        {
            throw new UserFriendlyException(string.Format("Employee Contract not found for Employee {0}", emp.Code));
        }

        if (empContract.EndDate < new DateTime(payInDate.Year, payInDate.Month, 1, 0, 0, 0)
            && input.OffCycleReason != EndOfServiceOffCycleReason)
        {
            if (!employeeCodesTyped)
            {
                return EmployeeRunOutcome.Skipped;
            }

            throw new UserFriendlyException(string.Format(
                "Contract EndDate on '{0}'. Can't run Payroll for Date : '{1}'",
                FormatDate(empContract.EndDate),
                FormatDate(payInDate)));
        }

        // Legacy lines 444-446.
        var empOrgAssignment = _employeeOrgAssignmentRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == emp.Id && r.StartDate <= payInDate && r.EndDate >= payInDate);

        if (empOrgAssignment == null)
        {
            throw new UserFriendlyException(string.Format("Org Assignment not found for Employee {0}", emp.Code));
        }

        // Legacy lines 448-450.
        var payrollControlRecord = _payrollControlRecordRepository.GetAll()
            .FirstOrDefault(r => r.PayrollAreaId == empOrgAssignment.PayrollAreaId);

        if (payrollControlRecord == null)
        {
            throw new UserFriendlyException(string.Format(
                "Payroll Control Record not found for PayrollArea {0}",
                empOrgAssignment.PayrollArea != null ? empOrgAssignment.PayrollArea.Code : string.Empty));
        }

        // Legacy lines 453-454.
        if (payInDate < payrollControlRecord.CalendarMonthEnd)
        {
            throw new UserFriendlyException("Payroll Control Record set to run for period later than payroll run date.");
        }

        // Legacy lines 457-462.
        if (input.PayrollRunType == PayrollRunType.OffCycle
            && MonthEnd(payrollControlRecord.CalendarMonthEnd.AddDays(1)) != payInDate)
        {
            throw new UserFriendlyException("Mismatch between Payroll Control Record and Payroll run date.");
        }

        // Legacy lines 464-468.
        var empPayrollStatus = _employeePayrollStatusRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == emp.Id);

        if (empPayrollStatus == null)
        {
            throw new UserFriendlyException(string.Format(
                "Payroll Status not found for Employee :{0} {1}", emp.Code, emp.FullNameEn));
        }

        if (empPayrollStatus.NextTimeEvaluationFrom < input.OffCyclePaymentDate
            && input.OffCycleReason != EndOfServiceOffCycleReason)
        {
            throw new UserFriendlyException(string.Format(
                "Time Engine not run for Employee :{0} {1}", emp.Code, emp.FullNameEn));
        }

        // Legacy lines 472-562. Only the Regular arm exists in legacy (and RunAsync refuses anything else).
        if (input.PayrollRunType != PayrollRunType.Regular)
        {
            return EmployeeRunOutcome.Processed;
        }

        // Legacy lines 476-479: the retro-period collection was removed from legacy on 27 July 2026; the only
        // statement left is "payrollForPeriods.Add(payInDate)". Kept as a one-item list so the loop shape
        // stays recognisable.
        var payrollForPeriods = new List<DateTime> { payInDate };

        foreach (var payForDate in payrollForPeriods)
        {
            // Legacy lines 487-493.
            if (empPayrollStatus.DoNoAccountAfter < payForDate
                && empPayrollStatus.DoNoAccountAfter != new DateTime(1, 1, 1))
            {
                continue;
            }

            // Legacy lines 495-499.
            var projRegisterList = _projectRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == emp.Id && r.Period == payForDate)
                .ToList();

            var payDate = payForDate;

            // Legacy lines 506-515.
            var salaryRegister = _salaryRegisterRepository.GetAll()
                .FirstOrDefault(r => r.EmployeeId == emp.Id
                                     && r.ForPeriod == payInDate
                                     && r.InPeriod == payInDate
                                     && r.PayrollRunType == input.PayrollRunType);

            if (salaryRegister == null)
            {
                throw new UserFriendlyException(string.Format(
                    "Salary Register not found not found for Employee {0} for Period '{1}' ",
                    emp.Code,
                    FormatDate(payInDate)));
            }

            // Legacy lines 518-525.
            var empProject = _employeeProjectRepository.GetAll()
                .FirstOrDefault(r => r.EmployeeId == emp.Id && r.StartDate <= payDate && r.EndDate >= payDate);

            if (empProject == null)
            {
                throw new UserFriendlyException(string.Format(
                    "Project not found for Employee {0} for Period '{1}' ",
                    emp.Code,
                    FormatDate(payInDate)));
            }

            // Legacy lines 527-528.
            if (projRegisterList.Count > 1)
            {
                throw new UserFriendlyException(string.Format(
                    "Duplicate Project Register found for {0}", payDate.ToShortDateString()));
            }

            ProjectRegister projectRegister;

            if (projRegisterList.Count == 1)
            {
                // Legacy lines 530-540: the existing register is REUSED and all its detail rows deleted.
                projectRegister = projRegisterList[0];

                var existingDetails = _projectRegisterDetailRepository.GetAll()
                    .Where(r => r.ProjectRegisterId == projectRegister.Id)
                    .ToList();

                foreach (var existingDetail in existingDetails)
                {
                    await _projectRegisterDetailRepository.DeleteAsync(existingDetail);
                }

                // The calculation below find-or-creates detail rows by (Project, WageType); the deletes
                // must be visible to those lookups first (legacy's Session.Save at line 534).
                await CurrentUnitOfWork.SaveChangesAsync();
            }
            else
            {
                // Legacy lines 542-551.
                projectRegister = new ProjectRegister
                {
                    EmployeeId = emp.Id,
                    Period = payForDate,
                    TenantId = emp.TenantId
                };

                // InsertAndGetIdAsync, never InsertAsync: every ProjectRegisterDetail written below needs
                // this id, and ABP only assigns it on flush.
                projectRegister.Id = await _projectRegisterRepository.InsertAndGetIdAsync(projectRegister);
            }

            await RunProjectCalculationAsync(
                salaryRegister, projectRegister, emp, payDate, settings);

            // Legacy line 559: SetRoundoff, AFTER the period's register is committed.
            await ApplyRoundingAsync(projectRegister, emp);
        }

        return EmployeeRunOutcome.Processed;
    }

    #endregion

    #region << Employee selection (legacy GetEmployeeList, lines 2355-2393) >>

    /// <summary>
    /// Legacy <c>GetEmployeeList</c>. Note the precedence, preserved exactly: an employee-code list WINS
    /// over the payroll area; only when no codes are given is the payroll area used; when neither is given
    /// EVERY employee is selected.
    /// </summary>
    private List<Employee> GetEmployeeList(string payrollArea, string empCode, DateTime dayDate)
    {
        var query = _employeeRepository.GetAll();

        if (HasTypedEmployeeCodes(empCode))
        {
            // Legacy GetEmpCodeList (lines 2383-2393): comma separated, each trimmed.
            var employees = empCode.Split(',').Select(s => s.Trim()).ToList();
            return query.Where(r => employees.Contains(r.Code)).OrderBy(r => r.Code).ToList();
        }

        if (!string.IsNullOrWhiteSpace(payrollArea))
        {
            var orgAssignmentQuery = _employeeOrgAssignmentRepository.GetAll()
                .Where(rd => rd.StartDate <= dayDate
                             && rd.EndDate >= dayDate
                             && rd.PayrollArea != null
                             && rd.PayrollArea.Name == payrollArea)
                .Select(rd => rd.EmployeeId);

            return query.Where(r => orgAssignmentQuery.Contains(r.Id)).OrderBy(r => r.Code).ToList();
        }

        return query.OrderBy(r => r.Code).ToList();
    }

    /// <summary>
    /// Legacy <c>empCode != ""</c>. Blank-only text counts as "no codes" -- the same test
    /// <see cref="GetEmployeeList"/> uses to choose between the codes and the payroll area, so the two never disagree.
    /// </summary>
    internal static bool HasTypedEmployeeCodes(string employeeCodes)
    {
        return !string.IsNullOrWhiteSpace(employeeCodes);
    }

    #endregion

    #region << Settings snapshot (legacy GetInitialVariables, lines 1291-1303) >>

    private async Task<ProjectCostEngineSettings> LoadSettingsAsync()
    {
        var securitySettings = await _sysSettingsAccessor.GetSecuritySettingsAsync();
        var projectCostingSettings = await _sysSettingsAccessor.GetProjectCostingSettingsAsync();
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();

        var settings = new ProjectCostEngineSettings();

        if (securitySettings != null)
        {
            settings.EnableAuditLogInProjectCostEngine = securitySettings.EnableAuditLogInProjectCostEngine;
        }

        if (projectCostingSettings != null)
        {
            settings.CalculateNetPayProjectWise = projectCostingSettings.CalculateNetPayProjectWise;
        }

        if (timeSettings != null)
        {
            settings.EnableTimeRegisterApproval = timeSettings.EnableTimeRegisterApproval;
            settings.EarliestEvaluationDate = timeSettings.EarliestEvaluationDate;
        }

        return settings;
    }

    #endregion

    #region << Rounding (legacy SetRoundoff, lines 1374-1416) >>

    /// <summary>
    /// In-memory re-expression of legacy's raw-SQL <c>UPDATE ProjectRegisterDetail SET Amount =
    /// ROUND(Amount, n), Number = ROUND(Number, n)</c> for one employee+period. The digit count <c>n</c>
    /// comes from <see cref="IPayrollRoundingService.GetCurrencyDecimalRoundingAsync"/>, which is the port
    /// of the identical <c>CurrentCompany.PayrollArea.Currency.DecimalCount</c> lookup legacy performs at
    /// lines 1383-1392 (including its fallback to the default currency).
    ///
    /// <para>Legacy swallows every exception here ("//exception while rounding .. skip", lines 1406-1409);
    /// preserved.</para>
    /// </summary>
    private async Task ApplyRoundingAsync(ProjectRegister projectRegister, Employee emp)
    {
        try
        {
            var digits = await _payrollRoundingService.GetCurrencyDecimalRoundingAsync(emp.Id);

            var details = _projectRegisterDetailRepository.GetAll()
                .Where(r => r.ProjectRegisterId == projectRegister.Id)
                .ToList();

            foreach (var detail in details)
            {
                detail.Amount = Math.Round(detail.Amount, digits, MidpointRounding.AwayFromZero);
                detail.Number = Math.Round(detail.Number, digits, MidpointRounding.AwayFromZero);
                await _projectRegisterDetailRepository.UpdateAsync(detail);
            }
        }
        catch (Exception)
        {
            // Legacy lines 1406-1409: swallowed.
        }
    }

    #endregion

    #region << Concurrency guard / execution log / project log / progress >>

    /// <summary>
    /// ONE RUN AT A TIME (per tenant). Not a port: legacy had no guard -- its Run button truncated the log and
    /// queued another thread, so a second run wiped the first one's log and their counters merged. Stale runs are
    /// aged out first by the writer, so a killed process cannot block the engine for ever; the check fails OPEN.
    /// </summary>
    private async Task EnsureNoRunInProgressAsync()
    {
        var alreadyRunning = await ProgressWriter.GetActiveRunAsync(EngineType.ProjectCostEngine, RunKey);
        if (alreadyRunning != null)
        {
            throw new UserFriendlyException(string.Format(
                "The Project Cost Engine is already running (started by {0} at {1}). Wait for it to finish, or stop it, before running it again.",
                string.IsNullOrWhiteSpace(alreadyRunning.StartedByUserName) ? "another user" : alreadyRunning.StartedByUserName,
                alreadyRunning.StartedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)));
        }
    }

    /// <summary>
    /// Legacy <c>TruncateLog</c> (lines 1706-1710, <c>TRUNCATE TABLE [dbo].[ProjectLog]</c>), run by the Run button
    /// before the engine starts. Raw DDL is not allowed here, so every row is deleted through the repository, in
    /// its own committed unit of work so the run's batches and the screen see an empty log straight away.
    /// Like legacy's TRUNCATE this also clears rows the Costing File screen wrote -- the two share the table.
    /// </summary>
    private async Task ClearProjectLogForNewRunAsync(int? tenantId)
    {
        using (var uow = UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
        using (tenantId.HasValue ? UnitOfWorkManager.Current.SetTenantId(tenantId) : null)
        {
            await _projectLogRepository.DeleteAsync(x => x.Id > 0);
            await uow.CompleteAsync();
        }
    }

    /// <summary>Legacy's "Execution Log" region (lines 302-334), including the exact pipe-separated
    /// parameter string.</summary>
    private async Task<int> AddExecutionLogAsync(ProjectCostEngineRunInput input, DateTime payInDate)
    {
        return await _executionLogRepository.InsertAndGetIdAsync(new ExecutionLog
        {
            ActionDateTime = Clock.Now,
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.ProjectEngine,
            ScreenName = ProjectCostEngineScreenName,
            Parameters = BuildExecutionLogParameters(input, payInDate),
            ExecutionStatus = ExecutionLogStatus.INPRECESS,
            TotalRecords = 0,
            Successfull = 0,
            Failed = 0
        });
    }

    /// <summary>
    /// The run's parameters in the execution log's words (legacy lines 303-322). Also shown on the progress row and
    /// used by the background job to describe a run the engine refused before it began.
    /// </summary>
    public static string BuildExecutionLogParameters(ProjectCostEngineRunInput input, DateTime payInDate)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(input.PayrollAreaName))
        {
            parts.Add("Payroll Area : " + input.PayrollAreaName);
        }

        parts.Add("Period : " + FormatDate(payInDate));
        parts.Add("Payroll RunType : " + input.PayrollRunType);

        if (!string.IsNullOrEmpty(input.OffCycleReason))
        {
            parts.Add("OffCycle Reasons : " + input.OffCycleReason);
        }

        if (input.OffCyclePaymentDate != default(DateTime))
        {
            parts.Add("OffCycle Payment Date : " + FormatDate(input.OffCyclePaymentDate));
        }

        if (!string.IsNullOrEmpty(input.EmployeeCodes))
        {
            parts.Add("Employee Code : " + input.EmployeeCodes);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Legacy's <c>finally</c> block (lines 623-652). Legacy counts ALL <see cref="ProjectLog"/> rows in the
    /// database rather than only this run's; that is right because every run starts by clearing the log
    /// (<see cref="ClearProjectLogForNewRunAsync"/>), exactly as legacy's Run button truncated it. Preserved.
    /// </summary>
    private async Task FinalizeExecutionLogAsync(int executionLogId, bool aborted)
    {
        try
        {
            var allLogs = _projectLogRepository.GetAll();
            var summaryAll = allLogs.Count();
            var summarySuccess = allLogs.Count(r => r.Status == 0);
            var summaryFailed = (summaryAll - summarySuccess) > 0 ? (summaryAll - summarySuccess) : 0;

            var isAborted = aborted || _projectLogRepository.GetAll().Any(x => x.StopFlag);

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
            // Legacy lines 649-651: swallowed.
        }
    }

    /// <summary>
    /// <paramref name="employeeId"/> is nullable so legacy's EMPLOYEE-LESS run-level failure row (legacy lines
    /// 270-278, where <c>pl.Employee</c> is never assigned) can be written -- see <see cref="ProjectLog.EmployeeId"/>.
    /// </summary>
    private async Task AddProjectLogAsync(int? employeeId, DateTime inPeriod, string message, int status, int employeeCount, int? tenantId)
    {
        var log = new ProjectLog
        {
            EmployeeId = employeeId,
            InPeriod = inPeriod,
            Message = message,
            Status = status,
            RunTimeStamp = Clock.Now,
            NoOfEmployee = employeeCount
        };

        if (tenantId.HasValue)
        {
            log.TenantId = tenantId.Value;
        }

        await _projectLogRepository.InsertAsync(log);
    }

    /// <summary>
    /// Legacy lines 270-278: the run-level failure row. No Employee, no NoOfEmployee, Status 1, and the message
    /// NOT truncated (unlike the per-employee row at lines 590-591) -- preserved. Written in its OWN unit of work,
    /// so a run whose own unit of work is rolled back still leaves the reason on the screen. Its own failure must
    /// never mask the original exception, so it is swallowed.
    /// </summary>
    private async Task AddRunFailureProjectLogAsync(DateTime payInDate, Exception ex, int? tenantId)
    {
        try
        {
            using (var uow = UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
            using (tenantId.HasValue ? UnitOfWorkManager.Current.SetTenantId(tenantId) : null)
            {
                await AddProjectLogAsync(null, payInDate, ex.Message, 1, 0, tenantId);
                await uow.CompleteAsync();
            }
        }
        catch (Exception logFailure)
        {
            // Never let the log write replace the real error.
            Logger.Warn("ProjectCostEngine: could not write the run-level failure ProjectLog row.", logFailure);
        }
    }

    /// <summary>Maps the run's outcome onto the operator-facing <see cref="EngineRunStatus"/>.</summary>
    private static EngineRunStatus ResolveEngineRunStatus(string runFailureMessage, ProjectCostEngineRunResult result)
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

    /// <summary>Legacy truncates the logged message to 1000 characters (lines 590-591). Preserved.</summary>
    private static string TruncateMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        return message.Length > 1000 ? message.Substring(0, 1000) : message;
    }

    #endregion

    /// <summary>Legacy's <c>ToString("dd/MM/yyyy")</c>, with the separator fixed to '/' whatever the culture.</summary>
    private static string FormatDate(DateTime date)
    {
        return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    /// <summary>Legacy's repeated "last day of that month, time zeroed" expression (lines 347, 458-461).</summary>
    internal static DateTime MonthEnd(DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 0, 0, 0);
    }
}
