using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Timing;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;

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
/// <item><description><b>TimeRegister approval "Reopened vs Opened" branch is collapsed</b> (legacy lines
/// ~1473-1511). Legacy distinguished the two by whether <c>TimeRegister.WfRequest</c> was non-null, and wrote a
/// <c>TimeLogHistory</c> row in the Reopened case. This port's <see cref="TimeRegister"/> has NO
/// <c>WfRequest</c> FK and there is NO <c>TimeLogHistory</c> entity, so neither the discriminator nor the
/// history row can be reproduced. A previously-Approved row whose AbsentDay/ActualWorkingDay/
/// DedLateInEarlyOutMins changed is set to <see cref="TimeRegisterStatus.Reopened"/> -- the status this port's
/// enum exists to express for exactly this case. <b>This is a judgement call, not a faithful port.</b></description></item>
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

    public TimeEngineService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<EmployeePayrollStatus> employeePayrollStatusRepository,
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<TimeLog> timeLogRepository,
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

    #region << Entry point >>

    public async Task<TimeEngineRunResult> CreateTimeRegisterAsync(TimeEngineRunInput input)
    {
        // Legacy Main() validation: "No End Date defined for Time Calculation or Check Date format."
        if (input?.EndDate == null || input.EndDate.Value == default(DateTime))
        {
            throw new ArgumentException("No End Date defined for Time Calculation or Check Date format.");
        }

        var settings = await LoadSettingsAsync();
        var masterData = LoadMasterData(settings);

        var executionLogId = await AddExecutionLogAsync(input);

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

        foreach (var employee in employees)
        {
            // Legacy "Check TE Stopped Or Not": a TimeLog row with StopFlag aborts the whole run.
            if (_timeLogRepository.GetAll().Any(x => x.StopFlag))
            {
                aborted = true;
                break;
            }

            try
            {
                var processed = await ProcessEmployeeAsync(
                    input, employee, runStartDate, runEndDate, settings, masterData, cursor, employeeCount);

                if (!processed)
                {
                    // Legacy "NoofEmp = NoofEmp - 1; continue;" -- employee skipped, not counted.
                    employeeCount = employeeCount - 1;
                }
            }
            catch (Exception ex)
            {
                // Legacy per-employee catch: log and carry on, never rethrow.
                await AddTimeLogAsync(employee.Id, runStartDate, ex.Message, 1, employeeCount);
            }
        }

        return await FinalizeExecutionLogAsync(executionLogId, aborted);
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
        // Legacy parameter string: "Payroll Area : x | Start Date : dd/MM/yyyy | End Date : dd/MM/yyyy | Employee Code : y"
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

        var log = new ExecutionLog
        {
            UserName = input.CurrentUserName,
            ActionDateTime = Clock.Now,
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.TimeEngine,
            ScreenName = TimeEngineScreenName,
            Parameters = string.Join(" | ", parts),
            ExecutionStatus = ExecutionLogStatus.INPRECESS,
            TotalRecords = 0,
            Successfull = 0,
            Failed = 0
        };

        return await _executionLogRepository.InsertAndGetIdAsync(log);
    }

    /// <summary>
    /// Ported from legacy <c>CreateTimeRegister</c>'s <c>finally</c> block. Legacy counts ALL TimeLog rows in
    /// the database (not just this run's) -- a known legacy quirk that relies on the TimeLog table being
    /// truncated before each run. Preserved as-is rather than "improved".
    /// </summary>
    private async Task<TimeEngineRunResult> FinalizeExecutionLogAsync(int executionLogId, bool aborted)
    {
        var allLogs = _timeLogRepository.GetAll();
        var summaryAll = allLogs.Count();
        var summarySuccess = allLogs.Count(r => r.Status == 0);
        var summaryFailed = (summaryAll - summarySuccess) > 0 ? (summaryAll - summarySuccess) : 0;

        var isAborted = aborted || _timeLogRepository.GetAll().Any(x => x.StopFlag);

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
