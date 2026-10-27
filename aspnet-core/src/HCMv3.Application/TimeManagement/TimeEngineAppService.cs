using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Dto;
using HCMv3.EngineRuns.Services;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement.Dto;
using HCMv3.TimeManagement.Exporting;
using HCMv3.TimeManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <inheritdoc cref="ITimeEngineAppService"/>
/// <remarks>
/// Ported from legacy's Time Engine screen <c>HCM10.Web.UserControls.PayrollManagement.uscTimeEngine</c>,
/// which collected employee code / start date / end date / payroll area and shelled out to the
/// <c>HCM10.TimeEngine</c> console app. The screen name is preserved verbatim on the <c>ExecutionLog</c> row
/// the engine writes (see <c>TimeEngineService.AddExecutionLogAsync</c>) so existing execution-log reports
/// keep working.
/// </remarks>
public class TimeEngineAppService : HCMv3AppServiceBase, ITimeEngineAppService
{
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly EngineRunProgressReader _progressReader;
    private readonly IEngineRunProgressWriter _progressWriter;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<ExecutionLog> _executionLogRepository;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly ITimeEngineRunCandidateSelector _candidateSelector;
    private readonly TimeEngineRunLogReader _runLogReader;
    private readonly ITimeEngineRunLogExcelExporter _runLogExcelExporter;

    public TimeEngineAppService(
        IBackgroundJobManager backgroundJobManager,
        EngineRunProgressReader progressReader,
        IEngineRunProgressWriter progressWriter,
        ISysSettingsAccessor sysSettingsAccessor,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<Employee> employeeRepository,
        IRepository<ExecutionLog> executionLogRepository,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        ITimeEngineRunCandidateSelector candidateSelector,
        TimeEngineRunLogReader runLogReader,
        ITimeEngineRunLogExcelExporter runLogExcelExporter)
    {
        _backgroundJobManager = backgroundJobManager;
        _progressReader = progressReader;
        _progressWriter = progressWriter;
        _sysSettingsAccessor = sysSettingsAccessor;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _employeeRepository = employeeRepository;
        _executionLogRepository = executionLogRepository;
        _payrollAreaResolver = payrollAreaResolver;
        _candidateSelector = candidateSelector;
        _runLogReader = runLogReader;
        _runLogExcelExporter = runLogExcelExporter;
    }

    #region << Run >>

    /// <summary>
    /// Queues the run. Deliberately <b>not</b> synchronous: a run covers every employee x every day in the
    /// window (legacy's own console app took minutes to hours), so executing it inline would hold an HTTP
    /// request open for the duration.
    ///
    /// <para><c>[HttpPost]</c> is required by this repo's NSwag rule for methods taking a custom input DTO --
    /// without it the input would be flattened into query-string parameters and
    /// <c>RunTimeEngineInput</c> would never be generated into the client proxies.</para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine_Run)]
    public async Task<RunTimeEngineOutput> RunTimeEngine(RunTimeEngineInput input)
    {
        // Legacy Main()'s single up-front validation (Program.cs ~line 175). Reproduced here so the caller
        // gets a 400 instead of a job that fails on the queue.
        if (input.EndDate == default(DateTime))
        {
            throw new UserFriendlyException(L("TimeEngineEndDateRequired"));
        }

        if (input.StartDate.HasValue && input.StartDate.Value.Date > input.EndDate.Date)
        {
            throw new UserFriendlyException(L("TimeEngineStartDateAfterEndDate"));
        }

        var runPayrollAreaName = await ValidateAsLegacyScreenAsync(input);

        await EnsureNoRunInProgressAsync(runPayrollAreaName);

        await _backgroundJobManager.EnqueueAsync<TimeEngineBackgroundJob, TimeEngineJobArgs>(
            new TimeEngineJobArgs
            {
                TenantId = AbpSession.GetTenantId(),
                UserId = AbpSession.UserId,
                EmployeeCodes = input.EmployeeCodes,
                StartDate = input.StartDate?.Date,
                EndDate = input.EndDate.Date,
                PayrollAreaName = input.PayrollAreaName,
                // Legacy args[4]: the console app was handed a user name by its caller; here it is the
                // authenticated user, which is strictly better provenance for the ExecutionLog row.
                CurrentUserName = (await GetCurrentUserAsync()).UserName,
                // Legacy args[5]. Runs arriving through this API are never payroll-engine-initiated; the
                // payroll engine calls ITimeEngineService directly with the flag set.
                IsRanFromPayrollEngine = false,
                EmployeeSubGroupIds = input.EmployeeSubGroupIds
            });

        return new RunTimeEngineOutput { Queued = true };
    }

    /// <summary>
    /// Legacy <c>uscTimeEngine.ascx.cs</c> lines 173-290, the checks the screen made before it queued a run, in
    /// legacy's order. Returns the payroll area the run belongs to (for the one-run-per-area guard).
    /// <list type="number">
    /// <item><description><b>A payroll area is selected:</b> its control record must exist ("Payroll Control
    /// Record not found."), then the two time-setting limits below.</description></item>
    /// <item><description><b>No area, employee codes typed:</b> the area of the FIRST code's org assignment
    /// covering today must exist ("Employee payroll area not found."), that area's control record must exist
    /// ("Payroll control record not found for selected employee payroll area."), then the two limits.</description></item>
    /// <item><description><b>Neither:</b> refused -- "Cannot have Payroll Area unselected if the Employee Code is
    /// blank."</description></item>
    /// </list>
    /// The limits: the End Date may not be more than <c>ManageTimeSettings.FutureEvaluationInDays</c> (legacy
    /// <c>TE_MAX_LIMIT_DAYS</c>) past the control record's <c>CalendarMonthEnd</c>, and the Start Date may not be
    /// before <c>ManageTimeSettings.EarliestEvaluationDate</c> (legacy <c>TIME_EVAL_EARLIEST_DATE</c>), which must be
    /// configured.
    ///
    /// <para><b>Two deliberate readings of legacy.</b> (1) With no earliest date configured legacy refused the
    /// codes path ("Configuration for TIME_EVAL_EARLIEST_DATE not done.") but silently did NOTHING on the area path
    /// -- no run, no message. Here both paths refuse with that message: a run that does not happen must say why.
    /// (2) Legacy compared the screen's Start Date, which it always had; here a blank Start Date is allowed (the
    /// engine then starts on the 1st of the current month, legacy's own default), so that date is what is
    /// compared.</para>
    /// </summary>
    private async Task<string> ValidateAsLegacyScreenAsync(RunTimeEngineInput input)
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();

        if (!input.PayrollAreaName.IsNullOrWhiteSpace())
        {
            var areaName = input.PayrollAreaName;
            var controlRecord = await _payrollControlRecordRepository.GetAll()
                .Where(r => r.PayrollArea.Name == areaName)
                .OrderBy(r => r.Id)
                .FirstOrDefaultAsync();

            if (controlRecord == null)
            {
                throw new UserFriendlyException(L("TimeEnginePayrollControlRecordNotFound"));
            }

            CheckTimeSettingLimits(input, controlRecord, timeSettings);
            return areaName.Trim();
        }

        if (!input.EmployeeCodes.IsNullOrWhiteSpace())
        {
            var area = await FindFirstEmployeePayrollAreaAsync(input.EmployeeCodes);
            if (area == null)
            {
                throw new UserFriendlyException(L("TimeEngineEmployeePayrollAreaNotFound"));
            }

            var controlRecord = await _payrollControlRecordRepository.GetAll()
                .Where(r => r.PayrollAreaId == area.Id)
                .OrderBy(r => r.Id)
                .FirstOrDefaultAsync();

            if (controlRecord == null)
            {
                throw new UserFriendlyException(L("TimeEngineEmployeeControlRecordNotFound"));
            }

            CheckTimeSettingLimits(input, controlRecord, timeSettings);
            return area.Name;
        }

        throw new UserFriendlyException(L("TimeEnginePayrollAreaOrEmployeeCodeRequired"));
    }

    /// <summary>
    /// Legacy lines 211-216: the payroll area of the employee's org assignment covering TODAY. The lookup goes
    /// through <see cref="IEmployeePayrollAreaResolver"/> -- the assignment's own payroll area, falling back to its
    /// org unit's profile -- exactly as the engines resolve an employee's area.
    /// </summary>
    private async Task<PayrollArea> FindFirstEmployeePayrollAreaAsync(string employeeCodes)
    {
        var firstCode = employeeCodes
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .FirstOrDefault(c => c.Length > 0);

        if (firstCode == null)
        {
            return null;
        }

        var employeeId = await _employeeRepository.GetAll()
            .Where(e => e.Code == firstCode)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync();

        return employeeId == null
            ? null
            : await _payrollAreaResolver.GetEmployeePayrollAreaAsync(employeeId.Value, Clock.Now.Date);
    }

    private void CheckTimeSettingLimits(RunTimeEngineInput input, PayrollControlRecord controlRecord, ManageTimeSettings timeSettings)
    {
        // Legacy read the settings row without a null check; a tenant without one has nothing to compare with.
        if (timeSettings == null)
        {
            return;
        }

        var latestAllowed = controlRecord.CalendarMonthEnd.AddDays(timeSettings.FutureEvaluationInDays);
        if (input.EndDate.Date > latestAllowed.Date)
        {
            throw new UserFriendlyException(L("TimeEngineEndDateBeyondFutureLimit",
                latestAllowed.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), timeSettings.FutureEvaluationInDays));
        }

        if (timeSettings.EarliestEvaluationDate == default(DateTime))
        {
            throw new UserFriendlyException(L("TimeEngineEarliestEvaluationDateNotConfigured"));
        }

        var effectiveStartDate = input.StartDate?.Date ?? new DateTime(Clock.Now.Year, Clock.Now.Month, 1);
        if (effectiveStartDate < timeSettings.EarliestEvaluationDate.Date)
        {
            throw new UserFriendlyException(L("TimeEngineStartDateBeforeEarliestDate",
                timeSettings.EarliestEvaluationDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
        }
    }

    /// <summary>
    /// ONE TIME ENGINE RUN PER PAYROLL AREA, checked here so the operator is told straight away instead of finding
    /// out from a job that failed; the engine re-checks it to catch two jobs queued at once. Different areas may run
    /// together. Not a port: legacy had no guard (see <c>TimeEngineRunKeys</c>).
    /// </summary>
    private async Task EnsureNoRunInProgressAsync(string payrollAreaName)
    {
        var runKey = TimeEngineRunKeys.ForPayrollArea(payrollAreaName);
        if (runKey == null)
        {
            return;
        }

        var activeRun = await _progressWriter.GetActiveRunAsync(EngineType.TimeEngine, runKey);
        if (activeRun != null)
        {
            throw new UserFriendlyException(L(
                "TimeEngineAlreadyRunningForPayrollArea",
                payrollAreaName,
                string.IsNullOrWhiteSpace(activeRun.StartedByUserName) ? L("AnotherUser") : activeRun.StartedByUserName,
                activeRun.StartedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)));
        }
    }

    #endregion

    #region << Progress (status, per-employee grid, day log, export, stop) >>

    /// <summary>
    /// Live status poll. A plain string argument, so it stays an HTTP GET, matching
    /// <c>GetProjectCostEngineStatus</c> -- the repo's <c>[HttpPost]</c> rule applies only to <c>Get*</c> methods
    /// taking a custom input DTO, which this is not.
    ///
    /// <para>Gated on the VIEW permission, not Run: an operator who may watch a run need not be able to
    /// start one.</para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine)]
    public async Task<EngineRunStatusDto> GetTimeEngineStatus(string payrollAreaName = null)
    {
        return await _progressReader.GetStatusAsync(EngineType.TimeEngine, ScreenScope(payrollAreaName));
    }

    /// <summary>
    /// Per-employee grid. <c>[HttpPost]</c> is mandatory here: <see cref="GetTimeEngineRunEmployeesInput"/> is a
    /// custom input DTO, and without it NSwag flattens it into query-string parameters and never emits the
    /// class into the client proxies.
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine)]
    public async Task<PagedResultDto<EngineRunEmployeeDto>> GetTimeEngineRunEmployees(GetTimeEngineRunEmployeesInput input)
    {
        return await _progressReader.GetEmployeesAsync(EngineType.TimeEngine, input, ScreenScope(input.PayrollAreaName));
    }

    /// <inheritdoc/>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine)]
    public async Task<List<TimeEngineRunDayDto>> GetTimeEngineRunDays(GetTimeEngineRunDaysInput input)
    {
        return await _runLogReader.GetDaysAsync(input.RunId, input.EmployeeId, input.OnlyFailed, null);
    }

    /// <inheritdoc/>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine)]
    public async Task<FileDto> GetTimeEngineRunLogToExcel(GetTimeEngineRunLogToExcelInput input)
    {
        input ??= new GetTimeEngineRunLogToExcelInput();

        // The same run the screen shows when none is named.
        var runId = input.RunId;
        if (runId == null)
        {
            var status = await _progressReader.GetStatusAsync(EngineType.TimeEngine, ScreenScope(input.PayrollAreaName));
            runId = status.HasRun ? status.RunId : (long?)null;
        }

        var days = runId == null
            ? new List<TimeEngineRunDayDto>()
            : await _runLogReader.GetDaysAsync(runId.Value, null, input.OnlyFailed, input.Filter);

        return await _runLogExcelExporter.ExportToFile(days);
    }

    /// <summary>
    /// Legacy's Stop button. Sets a flag the engine re-reads between employees on its own connection; the
    /// engine finishes the employee in hand and then stops, so nothing is left half-written. Only the selected
    /// area's run is stopped.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine_Run)]
    public async Task StopTimeEngine(string payrollAreaName = null)
    {
        await _progressReader.RequestStopAsync(EngineType.TimeEngine, ScreenScope(payrollAreaName));
    }

    /// <summary>Legacy's Clear Status button.</summary>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine_Run)]
    public async Task ClearTimeEngineProgress()
    {
        await _progressReader.ClearAsync(EngineType.TimeEngine);
    }

    /// <inheritdoc/>
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine_Run)]
    public async Task<int> AbandonStaleTimeEngineRuns()
    {
        return await _progressReader.AbandonStaleRunsAsync(EngineType.TimeEngine);
    }

    /// <summary>
    /// The runs this screen follows: the selected area's (any area when blank), never the per-employee runs the
    /// payroll engine makes for itself.
    /// </summary>
    private static EngineRunScope ScreenScope(string payrollAreaName)
    {
        return new EngineRunScope
        {
            RunKey = TimeEngineRunKeys.ForPayrollArea(payrollAreaName),
            ExcludedRunKeyPrefix = TimeEngineRunKeys.FromPayrollEnginePrefix
        };
    }

    #endregion

    #region << Employee picker (legacy BuildTimeEmployeePopupList + filters) >>

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine)]
    public async Task<TimeEngineRunCandidatesOutput> GetTimeEngineRunCandidates(GetTimeEngineRunCandidatesInput input)
    {
        var all = await BuildCandidateListAsync(input);
        var filtered = ApplyFilters(all, input);

        var page = filtered
            .AsQueryable()
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "EmployeeCode" : input.Sorting)
            .PageBy(input)
            .ToList();

        var (start, end) = _candidateSelector.ResolveWindow(input.StartDate, input.EndDate, Clock.Now);

        // Legacy UpdateTimeEmployeeFilterCounts (lines 832-842): counted over the whole list, whatever is filtered.
        return new TimeEngineRunCandidatesOutput
        {
            TotalCount = filtered.Count,
            Items = page,
            AllCount = all.Count,
            ProcessedCount = all.Count(c => c.PayrollStatus == PayrollRunCandidateStatus.Processed),
            PendingCount = all.Count(c => c.PayrollStatus == PayrollRunCandidateStatus.Pending),
            SelectedCount = all.Count(c => c.IsSelected),
            // Legacy GetTimeEmployeePopupPeriodText (lines 961-968).
            PeriodText = start.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                         + " - "
                         + end.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
        };
    }

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine)]
    public async Task<List<string>> GetTimeEngineRunCandidateCodes(GetTimeEngineRunCandidatesInput input)
    {
        var all = await BuildCandidateListAsync(input);

        return ApplyFilters(all, input)
            .OrderBy(c => c.EmployeeCode)
            .Select(c => c.EmployeeCode)
            .ToList();
    }

    private async Task<List<PayrollRunCandidateDto>> BuildCandidateListAsync(GetTimeEngineRunCandidatesInput input)
    {
        if (input == null || input.PayrollAreaName.IsNullOrWhiteSpace())
        {
            return new List<PayrollRunCandidateDto>();
        }

        var candidates = await _candidateSelector.GetCandidatesAsync(
            input.PayrollAreaName, input.StartDate, input.EndDate, Clock.Now);

        var selectedCodes = SplitCodes(input.SelectedEmployeeCodes);
        var arabic = IsArabic();

        return candidates.Select(c => new PayrollRunCandidateDto
        {
            EmployeeId = c.EmployeeId,
            EmployeeCode = c.EmployeeCode,
            EmployeeName = arabic && !c.FullNameAr.IsNullOrWhiteSpace() ? c.FullNameAr : c.FullNameEn,
            PayrollStatus = c.IsProcessed ? PayrollRunCandidateStatus.Processed : PayrollRunCandidateStatus.Pending,
            IsSelected = c.EmployeeCode != null && selectedCodes.Contains(c.EmployeeCode)
        }).ToList();
    }

    /// <summary>Legacy gridTimeEmployees_DataBinding (lines 652-680) plus the grid's own text filter.</summary>
    private static List<PayrollRunCandidateDto> ApplyFilters(List<PayrollRunCandidateDto> all, GetTimeEngineRunCandidatesInput input)
    {
        IEnumerable<PayrollRunCandidateDto> query = all;

        switch (input.StatusFilter)
        {
            case PayrollRunCandidateFilter.Processed:
                query = query.Where(c => c.PayrollStatus == PayrollRunCandidateStatus.Processed);
                break;
            case PayrollRunCandidateFilter.Pending:
                query = query.Where(c => c.PayrollStatus == PayrollRunCandidateStatus.Pending);
                break;
            case PayrollRunCandidateFilter.Selected:
                query = query.Where(c => c.IsSelected);
                break;
        }

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter.Trim();
            query = query.Where(c =>
                (c.EmployeeCode ?? string.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (c.EmployeeName ?? string.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    private static HashSet<string> SplitCodes(string codes)
    {
        // Legacy lines 590-596: split on ',' and trim, case-insensitive.
        return codes.IsNullOrWhiteSpace()
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                codes.Split(',').Select(c => c.Trim()).Where(c => c.Length > 0),
                StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsArabic()
    {
        return CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region << Run history >>

    /// <summary>
    /// The Time Engine's execution log, for the screen's history table. Leaves out the rows of the per-employee
    /// runs the payroll engine makes for itself (one per employee paid, user name
    /// <see cref="TimeEngineRunKeys.PayrollEngineOriginatorUserName"/>) -- legacy's screen never listed them, and
    /// they would bury the operator's own runs. They stay in the execution log itself, as legacy wrote them.
    /// Gated on the screen's own permission, so watching runs does not need the execution-log screen.
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEngine)]
    public async Task<PagedResultDto<ExecutionLogDto>> GetTimeEngineRunHistory(GetTimeEngineRunHistoryInput input)
    {
        input ??= new GetTimeEngineRunHistoryInput();

        var query = _executionLogRepository.GetAll()
            .Where(l => l.Module == ExecutionLogModule.TimeEngine
                        && (l.UserName == null || l.UserName != TimeEngineRunKeys.PayrollEngineOriginatorUserName));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "ActionDateTime DESC, Id DESC" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ExecutionLogDto>(totalCount, items.Select(l => new ExecutionLogDto
        {
            Id = l.Id,
            UserName = l.UserName,
            ActionDateTime = l.ActionDateTime,
            Action = l.Action,
            Module = l.Module,
            ScreenName = l.ScreenName,
            ScreenCaption = l.ScreenCaption,
            Parameters = l.Parameters,
            ExecutionStatus = l.ExecutionStatus,
            TotalRecords = l.TotalRecords,
            Successfull = l.Successfull,
            Failed = l.Failed
        }).ToList());
    }

    #endregion
}
