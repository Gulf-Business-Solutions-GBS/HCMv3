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
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Dto;
using HCMv3.EngineRuns.Services;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement.Dto;
using HCMv3.ProjectManagement.Exporting;
using HCMv3.ProjectManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.ProjectManagement;

/// <inheritdoc cref="IProjectCostEngineAppService"/>
/// <remarks>
/// Ported from legacy <c>HCM10.Web.UserControls.ProjectManagement.uscProjectCosting</c> as reworked in July-August
/// 2026. The screen name is preserved verbatim on the <c>ExecutionLog</c> row the engine writes (see
/// <c>ProjectCostEngineService</c>) so existing execution-log reports keep working.
///
/// <para>Every read here is gated on the Project Cost Engine VIEW permission and every change on its Run permission,
/// so the screen needs neither the Project Logs nor the Payroll Engine permissions. That is why the period and the
/// employee picker are worked out here (from the same control record, <c>PayrollPeriodShiftCalculator</c> and
/// project registers legacy read) rather than by calling <c>PayrollRunSetupAppService</c>, whose methods require the
/// Payroll Engine permission.</para>
///
/// <para>Queries are synchronous LINQ over <c>IRepository.GetAll()</c>, as the rest of this class and the engine.</para>
///
/// <para>NOTE on the legacy clone: <c>uscProjectCosting2.ascx.cs</c> is an older copy of this screen (July 7) and is
/// deliberately NOT ported a second time.</para>
/// </remarks>
public class ProjectCostEngineAppService : HCMv3AppServiceBase, IProjectCostEngineAppService
{
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IRepository<ProjectLog> _projectLogRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly EngineRunProgressReader _progressReader;
    private readonly IEngineRunProgressWriter _progressWriter;
    private readonly IProjectCostEngineCandidateSelector _candidateSelector;
    private readonly IProjectCostEngineLogExcelExporter _logExcelExporter;

    public ProjectCostEngineAppService(
        IBackgroundJobManager backgroundJobManager,
        IRepository<ProjectLog> projectLogRepository,
        IRepository<Employee> employeeRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        EngineRunProgressReader progressReader,
        IEngineRunProgressWriter progressWriter,
        IProjectCostEngineCandidateSelector candidateSelector,
        IProjectCostEngineLogExcelExporter logExcelExporter)
    {
        _backgroundJobManager = backgroundJobManager;
        _projectLogRepository = projectLogRepository;
        _employeeRepository = employeeRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _progressReader = progressReader;
        _progressWriter = progressWriter;
        _candidateSelector = candidateSelector;
        _logExcelExporter = logExcelExporter;
    }

    #region << Run / Stop / Clear (legacy ASPxCallbackPanel1_Callback, lines 1512-1560) >>

    /// <summary>
    /// Queues the run. Deliberately <b>not</b> synchronous: a run covers every employee x every project x every wage
    /// type, so executing it inline would hold an HTTP request open for the duration -- legacy itself pushed it onto
    /// a background thread (line 1521).
    ///
    /// <para>Only the validations legacy performs BEFORE it touches the database are reproduced here
    /// (<c>ExecuteLongOperation</c> lines 116-120 and <c>Main</c> lines 207-212), so the caller gets a 400 instead
    /// of a job that fails on the queue. Every other guard stays in the domain service where legacy has it.</para>
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run)]
    public async Task<RunProjectCostEngineOutput> RunProjectCostEngine(RunProjectCostEngineInput input)
    {
        // Legacy line 116: neither a payroll area nor an employee code is fatal.
        if (string.IsNullOrWhiteSpace(input.PayrollAreaName) && string.IsNullOrWhiteSpace(input.EmployeeCodes))
        {
            throw new UserFriendlyException(L("ProjectCostEnginePayrollAreaOrEmployeeCodeRequired"));
        }

        // Legacy's screen hid the run type and always sent Regular (uscProjectCosting.ascx line 53); the engine has
        // no off-cycle arm. Refused rather than queued as a run that would do nothing.
        if (input.PayrollRunType != PayrollRunType.Regular)
        {
            throw new UserFriendlyException(L("ProjectCostEngineOffCycleNotSupported"));
        }

        // Legacy line 207.
        if (input.PayInDate == default(DateTime))
        {
            throw new UserFriendlyException(L("ProjectCostEnginePayInDateRequired"));
        }

        // Legacy line 223 needs a payroll area to find the control record at all (line 134 reads it unguarded).
        if (string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            throw new UserFriendlyException(L("ProjectCostEnginePayrollAreaRequired"));
        }

        // ONE RUN AT A TIME, told straight away instead of by a job that fails minutes later. The engine re-checks to
        // catch two jobs queued together. Not a port -- see ProjectCostEngineService.RunKey.
        var activeRun = await _progressWriter.GetActiveRunAsync(EngineType.ProjectCostEngine, ProjectCostEngineService.RunKey);
        if (activeRun != null)
        {
            throw new UserFriendlyException(L(
                "ProjectCostEngineAlreadyRunning",
                string.IsNullOrWhiteSpace(activeRun.StartedByUserName) ? L("AnotherUser") : activeRun.StartedByUserName,
                activeRun.StartedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)));
        }

        await _backgroundJobManager.EnqueueAsync<ProjectCostEngineBackgroundJob, ProjectCostEngineJobArgs>(
            new ProjectCostEngineJobArgs
            {
                TenantId = AbpSession.GetTenantId(),
                UserId = AbpSession.UserId,
                CurrentUserName = await GetCurrentUserNameOrNullAsync(),
                PayrollRunType = PayrollRunType.Regular,
                EmployeeCodes = input.EmployeeCodes,
                PayInDate = input.PayInDate.Date,
                PayrollAreaName = input.PayrollAreaName,
                // Legacy's off-cycle boxes were hidden: no payment date (txtOffPaymentDate empty) and the reason
                // normalised to "" (line 140, the hidden combo's first item "Advance Leave").
                OffCyclePaymentDate = default(DateTime),
                OffCycleReason = string.Empty
            });

        return new RunProjectCostEngineOutput { Queued = true };
    }

    /// <summary>
    /// Legacy <c>btnStopProjectCosting</c> (lines 1523-1535) flagged the FIRST <c>ProjectLog</c> row. Here the flag is
    /// set on the run itself (its progress row), which the engine re-reads between employees on its own connection,
    /// so the current employee completes and the run then stops. Writing <c>ProjectLog</c> from outside would contend
    /// with the batch writing it; the engine still honours a <c>ProjectLog.StopFlag</c> too.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run)]
    public async Task StopProjectCostEngine()
    {
        await _progressReader.RequestStopAsync(EngineType.ProjectCostEngine);
    }

    /// <summary>
    /// Legacy <c>btnClearStatus</c> (lines 1536-1559): <c>TruncateLog</c> and the status reset. Clears this engine's
    /// progress history (the run in flight is kept) and the <c>ProjectLog</c> -- but not while a run is going, whose
    /// log and stop checks live in that table (legacy cleared it regardless, and a running engine then counted the
    /// wrong rows).
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run)]
    public async Task ClearProjectCostEngineProgress()
    {
        await _progressReader.ClearAsync(EngineType.ProjectCostEngine);

        var activeRun = await _progressWriter.GetActiveRunAsync(EngineType.ProjectCostEngine, ProjectCostEngineService.RunKey);
        if (activeRun == null)
        {
            // Raw TRUNCATE has no place in an app service; the rows are deleted through the repository.
            await _projectLogRepository.DeleteAsync(x => x.Id > 0);
        }
    }

    /// <summary>The explicit form of the crash handling every new run also does on its own.</summary>
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine_Run)]
    public async Task<int> AbandonStaleProjectCostEngineRuns()
    {
        return await _progressReader.AbandonStaleRunsAsync(EngineType.ProjectCostEngine);
    }

    #endregion

    #region << Progress (shared engine-run mechanism; legacy CalculateProcessStatus) >>

    /// <summary>
    /// Live status poll. Parameterless on purpose so it stays an HTTP GET, as <c>GetPayrollEngineStatus</c>. Replaces
    /// legacy's <c>CalculateProcessStatus</c> (lines 1593-1678), whose speed and ETA came from timestamp deltas between
    /// <c>ProjectLog</c> rows; the shared reader measures them per employee instead.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    public async Task<EngineRunStatusDto> GetProjectCostEngineStatus()
    {
        return await _progressReader.GetStatusAsync(EngineType.ProjectCostEngine);
    }

    /// <summary>Per-employee grid. <c>[HttpPost]</c>: custom input DTO (NSwag rule).</summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    public async Task<PagedResultDto<EngineRunEmployeeDto>> GetProjectCostEngineRunEmployees(GetEngineRunEmployeesInput input)
    {
        return await _progressReader.GetEmployeesAsync(EngineType.ProjectCostEngine, input);
    }

    #endregion

    #region << Period (legacy SetCurrentDates / SetDates) >>

    /// <summary>
    /// Legacy <c>SetCurrentDates</c> (lines 1317-1330, null-guarded on 7 August 2026): the area's control record gives
    /// month, year, start and end. No control record: nothing filled.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    public Task<ProjectCostEnginePeriodDto> GetProjectCostEngineContext(string payrollAreaName)
    {
        var period = new ProjectCostEnginePeriodDto { PayrollAreaName = payrollAreaName };

        if (payrollAreaName.IsNullOrWhiteSpace())
        {
            period.Message = L("PayrollRunPleaseSelectPayrollArea");
            return Task.FromResult(period);
        }

        var record = FindControlRecord(payrollAreaName);
        if (record == null)
        {
            period.Message = L("PayrollRunControlRecordIsEmpty");
            return Task.FromResult(period);
        }

        FillControlRecord(period, record);
        period.PayrollMonth = record.PayrollMonth;
        period.PayrollYear = record.PayrollYear;
        period.CalendarMonthStart = record.CalendarMonthStart.Date;
        period.CalendarMonthEnd = record.CalendarMonthEnd.Date;
        period.RunAllowed = record.PeriodLocked;

        return Task.FromResult(period);
    }

    /// <summary>
    /// Legacy <c>SetDates</c> (lines 1307-1316): the typed month and year mapped to calendar dates through the area's
    /// payroll year (<c>PayrollControlRecord.GetStartEndDateFromPayrollPeriod</c>, ported as
    /// <see cref="PayrollPeriodShiftCalculator"/>). Legacy's callback swallowed any error and left the dates as they
    /// were (line 1436-1439); here the dates come back empty with the reason.
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    public Task<ProjectCostEnginePeriodDto> GetProjectCostEnginePeriodDates(string payrollAreaName, int payrollMonth, int payrollYear)
    {
        var period = new ProjectCostEnginePeriodDto
        {
            PayrollAreaName = payrollAreaName,
            PayrollMonth = payrollMonth,
            PayrollYear = payrollYear
        };

        if (payrollAreaName.IsNullOrWhiteSpace())
        {
            period.Message = L("PayrollRunPleaseSelectPayrollArea");
            return Task.FromResult(period);
        }

        var record = FindControlRecord(payrollAreaName);
        if (record != null)
        {
            FillControlRecord(period, record);
        }

        var payrollPeriodCode = _payrollAreaRepository.GetAll()
            .Where(a => a.Name == payrollAreaName && a.PayrollPeriod != null)
            .Select(a => a.PayrollPeriod.Code)
            .FirstOrDefault();

        try
        {
            if (payrollPeriodCode.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("No payroll period.");
            }

            var (start, end) = PayrollPeriodShiftCalculator.GetCalendarMonthRange(payrollPeriodCode, payrollMonth, payrollYear);
            period.CalendarMonthStart = start.Date;
            period.CalendarMonthEnd = end.Date;
        }
        catch (ArgumentException)
        {
            // Includes a month outside 1-12 (DateTime throws ArgumentOutOfRangeException, an ArgumentException).
            period.Message = L("IncorrectPayrollPeriod");
            return Task.FromResult(period);
        }

        period.RunAllowed = record != null
                            && record.PeriodLocked
                            && record.CalendarMonthEnd.Date == period.CalendarMonthEnd;

        return Task.FromResult(period);
    }

    private PayrollControlRecord FindControlRecord(string payrollAreaName)
    {
        // Legacy: PayrollControlRecord.Where(r => r.PayrollArea.Name == name).FirstOrDefault() -- the engine's lookup too.
        return _payrollControlRecordRepository.GetAll()
            .Where(r => r.PayrollArea != null && r.PayrollArea.Name == payrollAreaName)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
    }

    private static void FillControlRecord(ProjectCostEnginePeriodDto period, PayrollControlRecord record)
    {
        period.HasControlRecord = true;
        period.PeriodLocked = record.PeriodLocked;
        period.PeriodClosed = record.PeriodClosed;
    }

    #endregion

    #region << Employee picker (legacy BuildEmployeePopupList + filters, lines 1713-2264) >>

    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    public async Task<PayrollRunCandidatesOutput> GetProjectCostEngineCandidates(GetProjectCostEngineCandidatesInput input)
    {
        var all = await BuildCandidateListAsync(input);
        var filtered = ApplyCandidateFilters(all, input);

        var page = filtered
            .AsQueryable()
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "EmployeeCode" : input.Sorting)
            .PageBy(input)
            .ToList();

        // Legacy UpdateFilterCounts (lines 2179-2206): counted over the whole list, whatever is filtered.
        return new PayrollRunCandidatesOutput
        {
            TotalCount = filtered.Count,
            Items = page,
            AllCount = all.Count,
            ProcessedCount = all.Count(c => c.PayrollStatus == PayrollRunCandidateStatus.Processed),
            PendingCount = all.Count(c => c.PayrollStatus == PayrollRunCandidateStatus.Pending),
            SelectedCount = all.Count(c => c.IsSelected),
            PayrollDate = input.PayrollDate == default(DateTime) ? null : input.PayrollDate.Date
        };
    }

    /// <summary>
    /// The codes the picker's current filter and search show, on every page -- legacy <c>SelectAllEmployees</c>
    /// ticks every visible row (lines 2154-2178), and OK writes back the ticked ones (lines 2131-2145).
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    public async Task<List<string>> GetProjectCostEngineCandidateCodes(GetProjectCostEngineCandidatesInput input)
    {
        var all = await BuildCandidateListAsync(input);

        return ApplyCandidateFilters(all, input)
            .OrderBy(c => c.EmployeeCode)
            .Select(c => c.EmployeeCode)
            .ToList();
    }

    private async Task<List<PayrollRunCandidateDto>> BuildCandidateListAsync(GetProjectCostEngineCandidatesInput input)
    {
        if (input == null || input.PayrollAreaName.IsNullOrWhiteSpace() || input.PayrollDate == default(DateTime))
        {
            return new List<PayrollRunCandidateDto>();
        }

        var candidates = await _candidateSelector.GetCandidatesAsync(input.PayrollAreaName, input.PayrollDate);

        var selectedCodes = SplitCodes(input.SelectedEmployeeCodes);
        var arabic = IsArabic();

        return candidates.Select(c => new PayrollRunCandidateDto
        {
            EmployeeId = c.EmployeeId,
            EmployeeCode = c.EmployeeCode,
            // Legacy shows FullNameEn (line 1950); the Arabic name under an Arabic UI matches the log grid's rule.
            EmployeeName = arabic && !c.FullNameAr.IsNullOrWhiteSpace() ? c.FullNameAr : c.FullNameEn,
            PayrollStatus = c.IsProcessed ? PayrollRunCandidateStatus.Processed : PayrollRunCandidateStatus.Pending,
            IsSelected = c.EmployeeCode != null && selectedCodes.Contains(c.EmployeeCode)
        }).ToList();
    }

    /// <summary>Legacy gridEmployees_DataBinding (lines 1959-1995) plus the grid's own text filter.</summary>
    private static List<PayrollRunCandidateDto> ApplyCandidateFilters(List<PayrollRunCandidateDto> all, GetProjectCostEngineCandidatesInput input)
    {
        IEnumerable<PayrollRunCandidateDto> query = all;

        switch (input?.StatusFilter)
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

        if (input != null && !input.Filter.IsNullOrWhiteSpace())
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
        // Legacy lines 1764-1769: split on ',' and trim, case-insensitive.
        return codes.IsNullOrWhiteSpace()
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                codes.Split(',').Select(c => c.Trim()).Where(c => c.Length > 0),
                StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region << Log grid + export (legacy ExecutionLog popup, lines 2266-2352) >>

    /// <summary>
    /// The run log -- legacy's log popup grid (<c>SetLogGridDataSource</c>, lines 2317-2339). Holds the current run
    /// only: every run starts with an empty log, as legacy's Run button truncated it. Earlier runs stay on their
    /// progress rows (the panel above) and their ExecutionLog rows.
    /// </summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    public Task<ProjectCostEngineLogsOutput> GetProjectCostEngineLogs(GetProjectCostEngineLogsInput input)
    {
        input ??= new GetProjectCostEngineLogsInput();

        // Legacy RefreshPayrollLogCounts (lines 1679-1691), over the whole log whatever is filtered.
        var counted = _projectLogRepository.GetAll().Where(r => r.Status == 0 || r.Status == 1);
        var successCount = counted.Count(r => r.Status == 0);
        var failedCount = counted.Count(r => r.Status == 1);

        var query = BuildLogQuery(input.OnlyFailed);
        var totalCount = query.Count();

        var items = query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToList();

        return Task.FromResult(new ProjectCostEngineLogsOutput
        {
            TotalCount = totalCount,
            Items = items,
            AllCount = successCount + failedCount,
            SuccessCount = successCount,
            FailedCount = failedCount
        });
    }

    /// <summary>Legacy <c>btnExportTimeLog_Click</c> (lines 2340-2351): the log grid as filtered, every page.</summary>
    [HttpPost]
    [AbpAuthorize(AppPermissions.Pages_ProjectManagement_ProjectCostEngine)]
    public async Task<FileDto> GetProjectCostEngineLogsToExcel(GetProjectCostEngineLogsToExcelInput input)
    {
        var rows = BuildLogQuery(input?.OnlyFailed).OrderBy(r => r.Id).ToList();
        return await _logExcelExporter.ExportToFile(rows);
    }

    private IQueryable<ProjectCostEngineLogDto> BuildLogQuery(bool? onlyFailed)
    {
        var arabic = IsArabic();

        // Legacy: Status 0 or 1 only, then the filter (lines 2321-2327).
        var logs = _projectLogRepository.GetAll()
            .Where(r => r.Status == 0 || r.Status == 1)
            .WhereIf(onlyFailed == true, r => r.Status == 1)
            .WhereIf(onlyFailed == false, r => r.Status == 0);

        // A left join rather than an Include, so the run-level failure row (no employee) is kept.
        return from log in logs
               join employee in _employeeRepository.GetAll() on log.EmployeeId equals (int?)employee.Id into matched
               from employee in matched.DefaultIfEmpty()
               select new ProjectCostEngineLogDto
               {
                   Id = log.Id,
                   EmployeeCode = employee == null ? string.Empty : employee.Code,
                   EmployeeName = employee == null
                       ? string.Empty
                       : (arabic && employee.FullNameAr != null && employee.FullNameAr != "" ? employee.FullNameAr : employee.FullNameEn),
                   RunTimeStamp = log.RunTimeStamp,
                   Status = log.Status,
                   Message = log.Message,
                   InPeriod = log.InPeriod
               };
    }

    #endregion

    private async Task<string> GetCurrentUserNameOrNullAsync()
    {
        // UserManager is property-injected; a directly constructed service (unit tests) has none.
        if (UserManager == null || !AbpSession.UserId.HasValue)
        {
            return null;
        }

        var user = await UserManager.FindByIdAsync(AbpSession.UserId.Value.ToString(CultureInfo.InvariantCulture));
        return user?.UserName;
    }

    private static bool IsArabic()
    {
        return CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
    }
}
