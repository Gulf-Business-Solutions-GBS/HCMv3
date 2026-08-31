using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using Abp.UI;
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
    /// Preserved verbatim on the <see cref="ExecutionLog"/> row (legacy line 340) so existing
    /// execution-log reports keep working.
    /// </summary>
    private const string ProjectCostEngineScreenName = "HCM10.Web.UserControls.ProjectManagement.uscProjectCosting";

    /// <summary>Legacy static field <c>endOfServiceOffcycle</c> (line 34).</summary>
    internal const string EndOfServiceOffCycleReason = "End Of Service";

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

    #region << Run (legacy uscProjectCosting.Main, lines 167-308) >>

    /// <inheritdoc/>
    public async Task<ProjectCostEngineRunResult> RunAsync(ProjectCostEngineRunInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        try
        {
            return await RunCoreAsync(input);
        }
        catch (Exception ex)
        {
            // ==========================================================================================
            // WHOLE-RUN FAILURE LOG -- legacy lines 278-293. Legacy catches everything the pre-flight
            // block can throw and records it as an EMPLOYEE-LESS ProjectLog row (`pl.Employee` is never
            // assigned; NoOfEmployee is left at its default). Wave P8 made ProjectLog.EmployeeId nullable
            // so this row can finally be written.
            //
            // DELIBERATE DEVIATION: legacy SWALLOWED the exception after logging. This port RE-THROWS,
            // because the ABP background job (ProjectCostEngineBackgroundJob) and the app service are the
            // caller's only failure channel and several shipped tests assert that pre-flight problems
            // surface as UserFriendlyException. Logging is additive; the throw contract is unchanged.
            // ==========================================================================================
            await AddRunFailureProjectLogAsync(input.PayInDate, ex);
            throw;
        }
    }

    private async Task<ProjectCostEngineRunResult> RunCoreAsync(ProjectCostEngineRunInput input)
    {
        var payInDate = input.PayInDate;

        // Legacy lines 219-220.
        if (payInDate == default(DateTime))
        {
            throw new UserFriendlyException("No Pay Date defined for Payroll Calculation");
        }

        // Legacy lines 223-224.
        if (input.PayrollRunType == PayrollRunType.OffCycle && input.OffCyclePaymentDate == default(DateTime))
        {
            throw new UserFriendlyException("No Offcycle Payment Date selected for Offcycle Payroll.");
        }

        // Legacy line 228: GetInitialVariables.
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

        // Legacy line 235.
        var controlRecord = _payrollControlRecordRepository.GetAll()
            .FirstOrDefault(x => (x.PayrollArea != null ? x.PayrollArea.Name : "") == input.PayrollAreaName);

        if (controlRecord == null)
        {
            // Legacy line 272.
            throw new UserFriendlyException("Payroll Control Record not found, Cannot Run Project Cost Engine.");
        }

        // Legacy lines 238-243.
        var payrollCalendar = _payrollCalendarRepository.GetAll()
            .FirstOrDefault(a => a.PayrollMonth == payInDate.Month && a.PayrollYear == payInDate.Year);

        if (payrollCalendar == null)
        {
            throw new UserFriendlyException("Payroll Calender does not have record for Payroll month you are running.");
        }

        // ---------------------------------------------------------------------------------------------
        // Legacy lines 244-262. NOTE, deliberately preserved: legacy only ever runs the engine for a
        // REGULAR run. There is no `else` for PayrollRunType.OffCycle at this level -- an off-cycle run
        // validates its arguments, finds the control record, and then does nothing at all. That is legacy
        // behaviour, not an omission in this port; the off-cycle inputs are still carried through because
        // the per-employee guards below read them.
        // ---------------------------------------------------------------------------------------------
        if (input.PayrollRunType == PayrollRunType.Regular)
        {
            // Legacy line 247. Legacy compared `PeriodLocked == PeriodLocked.Yes`; this port's
            // PayrollControlRecord.PeriodLocked is a bool.
            if (controlRecord.CalendarMonthEnd.Date != payInDate.Date || !controlRecord.PeriodLocked)
            {
                throw new UserFriendlyException("Payroll Control Record not locked or is not current.");
            }

            return await CreateProjectRegisterAsync(input, payInDate, settings);
        }

        return new ProjectCostEngineRunResult { PayInDate = payInDate };
    }

    #endregion

    #region << CreateProjectRegister (legacy lines 309-671) >>

    private async Task<ProjectCostEngineRunResult> CreateProjectRegisterAsync(
        ProjectCostEngineRunInput input,
        DateTime payInDate,
        ProjectCostEngineSettings settings)
    {
        var executionLogId = await AddExecutionLogAsync(input, payInDate);

        var result = new ProjectCostEngineRunResult { PayInDate = payInDate };

        try
        {
            // Legacy line 351.
            var employees = GetEmployeeList(input.PayrollAreaName, input.EmployeeCodes, payInDate);

            // Legacy line 359: payInDate is normalised to its month end (time zeroed) before the loop.
            payInDate = MonthEnd(payInDate);
            result.PayInDate = payInDate;

            // Legacy line 365: computed once, and (unlike the Payroll Engine) never decremented.
            var employeeCount = employees.Count;

            foreach (var employee in employees)
            {
                var employeeResult = new ProjectCostEmployeeRunResult
                {
                    EmployeeId = employee.Id,
                    EmployeeCode = employee.Code
                };

                try
                {
                    var processed = await ProcessEmployeeAsync(input, employee, payInDate, settings);

                    if (processed == EmployeeRunOutcome.Aborted)
                    {
                        // Legacy lines 397-400: a ProjectLog row with StopFlag breaks the whole loop.
                        result.Aborted = true;
                        break;
                    }

                    if (processed == EmployeeRunOutcome.Skipped)
                    {
                        // Legacy line 395: `continue` -- the employee gets NO ProjectLog row at all.
                        continue;
                    }

                    // Legacy lines 584-593.
                    employeeResult.Succeeded = true;
                    employeeResult.Message = "Successfull";
                    await AddProjectLogAsync(employee.Id, payInDate, "Successfull", 0, employeeCount);
                }
                catch (Exception ex)
                {
                    // ==========================================================================
                    // PER-EMPLOYEE ERROR ISOLATION -- legacy lines 598-622. One bad employee must
                    // never abort the run.
                    // ==========================================================================
                    var message = TruncateMessage(ex.Message);

                    employeeResult.Succeeded = false;
                    employeeResult.Message = message;
                    await AddProjectLogAsync(employee.Id, payInDate, message, 1, employeeCount);
                }

                result.EmployeeResults.Add(employeeResult);
            }
        }
        finally
        {
            // Legacy lines 638-670: the execution log is finalised even when the run blew up.
            await FinalizeExecutionLogAsync(executionLogId, result.Aborted);
        }

        return result;
    }

    private enum EmployeeRunOutcome
    {
        Processed,
        Skipped,
        Aborted
    }

    /// <summary>
    /// Legacy's per-employee body (lines 383-597): validations, then the Regular-run register creation.
    /// </summary>
    private async Task<EmployeeRunOutcome> ProcessEmployeeAsync(
        ProjectCostEngineRunInput input,
        Employee emp,
        DateTime payInDate,
        ProjectCostEngineSettings settings)
    {
        // Legacy lines 388-392.
        var empContract = _employeeContractRepository.GetAll()
            .Where(r => r.EmployeeId == emp.Id)
            .OrderByDescending(r => r.EndDate)
            .FirstOrDefault();

        if (empContract == null)
        {
            throw new UserFriendlyException(string.Format("Employee Contract not found for Employee {0}", emp.Code));
        }

        // Legacy lines 394-395.
        if (empContract.EndDate < new DateTime(payInDate.Year, payInDate.Month, 1, 0, 0, 0)
            && input.OffCycleReason != EndOfServiceOffCycleReason)
        {
            return EmployeeRunOutcome.Skipped;
        }

        // Legacy lines 397-400.
        if (_projectLogRepository.GetAll().Any(x => x.StopFlag))
        {
            return EmployeeRunOutcome.Aborted;
        }

        // Legacy lines 406-408.
        var empOrgAssignment = _employeeOrgAssignmentRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == emp.Id && r.StartDate <= payInDate && r.EndDate >= payInDate);

        if (empOrgAssignment == null)
        {
            throw new UserFriendlyException(string.Format("Org Assignment not found for Employee {0}", emp.Code));
        }

        // Legacy lines 410-412.
        var payrollControlRecord = _payrollControlRecordRepository.GetAll()
            .FirstOrDefault(r => r.PayrollAreaId == empOrgAssignment.PayrollAreaId);

        if (payrollControlRecord == null)
        {
            throw new UserFriendlyException(string.Format(
                "Payroll Control Record not found for PayrollArea {0}",
                empOrgAssignment.PayrollArea != null ? empOrgAssignment.PayrollArea.Code : string.Empty));
        }

        // Legacy lines 415-416.
        if (payInDate < payrollControlRecord.CalendarMonthEnd)
        {
            throw new UserFriendlyException("Payroll Control Record set to run for period later than payroll run date.");
        }

        // Legacy lines 419-424.
        if (input.PayrollRunType == PayrollRunType.OffCycle
            && MonthEnd(payrollControlRecord.CalendarMonthEnd.AddDays(1)) != payInDate)
        {
            throw new UserFriendlyException("Mismatch between Payroll Control Record and Payroll run date.");
        }

        // Legacy lines 426-430.
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

        // Legacy lines 435-579. Only the Regular arm exists in legacy; there is no OffCycle arm here
        // either (and RunAsync never reaches this method for an off-cycle run anyway).
        if (input.PayrollRunType != PayrollRunType.Regular)
        {
            return EmployeeRunOutcome.Processed;
        }

        // Legacy lines 437-497: the retro-period collection is ENTIRELY commented out in legacy; the only
        // live statement is "payrollForPeriods.Add(payInDate)" (lines 494-495). Preserved as a single-item
        // list so the surrounding loop shape (and any future retro work) stays recognisable.
        var payrollForPeriods = new List<DateTime> { payInDate };

        foreach (var payForDate in payrollForPeriods)
        {
            // Legacy lines 503-509.
            if (empPayrollStatus.DoNoAccountAfter < payForDate
                && empPayrollStatus.DoNoAccountAfter != new DateTime(1, 1, 1))
            {
                continue;
            }

            // Legacy lines 511-515.
            var projRegisterList = _projectRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == emp.Id && r.Period == payForDate)
                .ToList();

            var payDate = payForDate;

            // Legacy lines 522-531.
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
                    payInDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
            }

            // Legacy lines 534-541.
            var empProject = _employeeProjectRepository.GetAll()
                .FirstOrDefault(r => r.EmployeeId == emp.Id && r.StartDate <= payDate && r.EndDate >= payDate);

            if (empProject == null)
            {
                throw new UserFriendlyException(string.Format(
                    "Project not found for Employee {0} for Period '{1}' ",
                    emp.Code,
                    payInDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
            }

            // Legacy lines 543-544.
            if (projRegisterList.Count > 1)
            {
                throw new UserFriendlyException(string.Format(
                    "Duplicate Project Register found for {0}", payDate.ToShortDateString()));
            }

            ProjectRegister projectRegister;

            if (projRegisterList.Count == 1)
            {
                // Legacy lines 546-556: the existing register is REUSED and all its detail rows deleted.
                projectRegister = projRegisterList[0];

                var existingDetails = _projectRegisterDetailRepository.GetAll()
                    .Where(r => r.ProjectRegisterId == projectRegister.Id)
                    .ToList();

                foreach (var existingDetail in existingDetails)
                {
                    await _projectRegisterDetailRepository.DeleteAsync(existingDetail);
                }

                // The calculation below find-or-creates detail rows by (Project, WageType); the deletes
                // must be visible to those lookups first (legacy's Session.Save at line 550).
                await CurrentUnitOfWork.SaveChangesAsync();
            }
            else
            {
                // Legacy lines 558-566.
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

            // Legacy line 575: SetRoundoff, AFTER the period's register is committed.
            await ApplyRoundingAsync(projectRegister, emp);
        }

        return EmployeeRunOutcome.Processed;
    }

    #endregion

    #region << Employee selection (legacy GetEmployeeList, lines 1747-1774) >>

    /// <summary>
    /// Legacy <c>GetEmployeeList</c>. Note the precedence, preserved exactly: an employee-code list WINS
    /// over the payroll area; only when no codes are given is the payroll area used; when neither is given
    /// EVERY employee is selected.
    /// </summary>
    private List<Employee> GetEmployeeList(string payrollArea, string empCode, DateTime dayDate)
    {
        var query = _employeeRepository.GetAll();

        if (!string.IsNullOrWhiteSpace(empCode))
        {
            // Legacy GetEmpCodeList (lines 1775-1785): comma separated, each trimmed.
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

    #endregion

    #region << Settings snapshot (legacy GetInitialVariables, lines 1308-1320) >>

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

    #region << Rounding (legacy SetRoundoff, lines 1385-1427) >>

    /// <summary>
    /// In-memory re-expression of legacy's raw-SQL <c>UPDATE ProjectRegisterDetail SET Amount =
    /// ROUND(Amount, n), Number = ROUND(Number, n)</c> for one employee+period. The digit count <c>n</c>
    /// comes from <see cref="IPayrollRoundingService.GetCurrencyDecimalRoundingAsync"/>, which is the port
    /// of the identical <c>CurrentCompany.PayrollArea.Currency.DecimalCount</c> lookup legacy performs at
    /// lines 1394-1403 (including its fallback to the default currency).
    ///
    /// <para>Legacy swallows every exception here ("//exception while rounding .. skip", lines 1417-1420);
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
            // Legacy lines 1417-1420: swallowed.
        }
    }

    #endregion

    #region << Execution log / project log >>

    /// <summary>Legacy's "Execution Log" region (lines 314-346), including the exact pipe-separated
    /// parameter string.</summary>
    private async Task<int> AddExecutionLogAsync(ProjectCostEngineRunInput input, DateTime payInDate)
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
            parts.Add("OffCycle Payment Date : "
                      + input.OffCyclePaymentDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(input.EmployeeCodes))
        {
            parts.Add("Employee Code : " + input.EmployeeCodes);
        }

        return await _executionLogRepository.InsertAndGetIdAsync(new ExecutionLog
        {
            ActionDateTime = Clock.Now,
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.ProjectEngine,
            ScreenName = ProjectCostEngineScreenName,
            Parameters = string.Join(" | ", parts),
            ExecutionStatus = ExecutionLogStatus.INPRECESS,
            TotalRecords = 0,
            Successfull = 0,
            Failed = 0
        });
    }

    /// <summary>
    /// Legacy's <c>finally</c> block (lines 640-669). Legacy counts ALL <see cref="ProjectLog"/> rows in
    /// the database rather than only this run's -- the same known quirk <c>PayrollEngineService</c>
    /// documents for <c>PayrollLog</c> (legacy's screen truncated the table before each run, line 1609,
    /// which is why the count worked). Preserved as-is.
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
            // Legacy lines 666-668: swallowed.
        }
    }

    /// <summary>
    /// <paramref name="employeeId"/> is nullable so legacy's EMPLOYEE-LESS run-level failure row (legacy
    /// <c>uscProjectCosting.ascx.cs</c> lines 280-293, where <c>pl.Employee</c> is never assigned) can be
    /// written -- see <see cref="ProjectLog.EmployeeId"/>.
    /// </summary>
    private async Task AddProjectLogAsync(int? employeeId, DateTime inPeriod, string message, int status, int employeeCount)
    {
        await _projectLogRepository.InsertAsync(new ProjectLog
        {
            EmployeeId = employeeId,
            InPeriod = inPeriod,
            Message = message,
            Status = status,
            RunTimeStamp = Clock.Now,
            NoOfEmployee = employeeCount
        });
    }

    /// <summary>
    /// Legacy lines 280-292: the run-level failure row. No Employee, no NoOfEmployee, Status 1.
    /// Legacy does NOT truncate this one (unlike the per-employee row at lines 607-608) -- preserved.
    /// Its own failure must never mask the original exception, so it is swallowed.
    /// </summary>
    private async Task AddRunFailureProjectLogAsync(DateTime payInDate, Exception ex)
    {
        try
        {
            await AddProjectLogAsync(null, payInDate, ex.Message, 1, 0);
        }
        catch (Exception)
        {
            // Never let the log write replace the real error.
        }
    }

    /// <summary>Legacy truncates the logged message to 1000 characters (lines 607-608). Preserved.</summary>
    private static string TruncateMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        return message.Length > 1000 ? message.Substring(0, 1000) : message;
    }

    #endregion

    /// <summary>Legacy's repeated "last day of that month, time zeroed" expression (lines 359, 420-423).</summary>
    internal static DateTime MonthEnd(DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 0, 0, 0);
    }
}
