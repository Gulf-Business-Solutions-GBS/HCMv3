using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Caching;
using Abp.Runtime.Session;
using Abp.Timing;
using HCMv3.PayrollManagement.Costing;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;
using HCMv3.SystemSecurity;

namespace HCMv3.PayrollManagement;

/// <inheritdoc cref="IFinanceRunLog"/>
public class FinanceRunLog : AbpServiceBase, IFinanceRunLog, ITransientDependency
{
    /// <summary>
    /// A run that is still INPRECESS but has written nothing for this long is treated as dead (the server was
    /// restarted, or the job was lost). One D365 call may take up to five minutes (legacy's
    /// <c>client.Timeout</c>), and a journal makes three calls before its first log row, so this is well clear
    /// of a slow but healthy posting.
    /// </summary>
    public const int StaleAfterMinutes = 20;

    /// <summary>Legacy <c>lblProgressCurrentEmp</c> cut-off (line 2163).</summary>
    public const int CurrentEmployeeMaxLength = 30;

    /// <summary>Legacy <c>ProjectLog.Message</c> truncation for a failed employee (lines 639, 1007, 1056).</summary>
    public const int MaxEmployeeMessageLength = 1000;

    public const string CostEngineScreenName = nameof(CostingFileAppService);
    public const string PostJvScreenName = nameof(PostJvAppService);

    private const string StopCacheName = "HCMv3.FinanceRunStopRequests";
    private const string ClearedCacheName = "HCMv3.FinanceRunCleared";

    private readonly IRepository<ProjectLog> _projectLogRepository;
    private readonly IRepository<ExecutionLog> _executionLogRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ICacheManager _cacheManager;
    private readonly IAbpSession _session;

    public FinanceRunLog(
        IRepository<ProjectLog> projectLogRepository,
        IRepository<ExecutionLog> executionLogRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWorkManager unitOfWorkManager,
        ICacheManager cacheManager,
        IAbpSession session)
    {
        _projectLogRepository = projectLogRepository;
        _executionLogRepository = executionLogRepository;
        _employeeRepository = employeeRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _cacheManager = cacheManager;
        _session = session;
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }

    public static string ScreenNameOf(FinanceRunProcess process) =>
        process == FinanceRunProcess.PostJv ? PostJvScreenName : CostEngineScreenName;

    private static readonly string[] ScreenNames = { CostEngineScreenName, PostJvScreenName };

    #region << Run record (ExecutionLog) >>

    /// <summary>Legacy parameter string (lines 397-424): same labels, same order, same separator.</summary>
    public static string DescribeParameters(CostingRunInput input)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            parts.Add("Payroll Area : " + input.PayrollAreaName);
        }

        parts.Add("Payroll RunType : " + input.PayrollRunType);
        parts.Add("Period : " + input.PayInDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));

        if (input.PostingDate != default)
        {
            parts.Add("Posting Date : " + input.PostingDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    public async Task<int> OpenAsync(FinanceRunProcess process, CostingRunInput input, string userName)
    {
        var id = await _executionLogRepository.InsertAndGetIdAsync(new ExecutionLog
        {
            TenantId = _session.GetTenantId(),
            UserName = userName,
            ActionDateTime = Clock.Now,
            Action = ExecutionLogAction.RUN,
            Module = ExecutionLogModule.CostEngine,
            ScreenName = ScreenNameOf(process),
            ScreenCaption = L(process == FinanceRunProcess.PostJv ? "PostJv" : "CostingFile"),
            Parameters = DescribeParameters(input),
            ExecutionStatus = ExecutionLogStatus.INPRECESS,
            TotalRecords = 0,
            Successfull = 0,
            Failed = 0
        });

        await SaveAsync();
        return id;
    }

    public async Task<bool> TryClaimAsync(int executionLogId)
    {
        var log = await _executionLogRepository.FirstOrDefaultAsync(executionLogId);

        // Never modified since the screen opened it = this job has not started before. Anything else means
        // the job is being executed a second time (ABP re-runs a job left in its store by a restart).
        if (log == null || log.ExecutionStatus != ExecutionLogStatus.INPRECESS || log.LastModificationTime != null)
        {
            return false;
        }

        var now = Clock.Now;
        log.ActionDateTime = now;
        log.LastModificationTime = now;
        await _executionLogRepository.UpdateAsync(log);
        await SaveAsync();
        return true;
    }

    public async Task MarkInterruptedAsync(int executionLogId, DateTime inPeriod)
    {
        try
        {
            var log = await _executionLogRepository.FirstOrDefaultAsync(executionLogId);
            if (log == null || log.ExecutionStatus != ExecutionLogStatus.INPRECESS)
            {
                return;
            }

            await WriteAsync(executionLogId, null, inPeriod, L("FinanceRunInterrupted"), 1, 0);

            log.ExecutionStatus = ExecutionLogStatus.FAILED;
            await _executionLogRepository.UpdateAsync(log);
            await SaveAsync();
            await ForgetStopRequestAsync(executionLogId);
        }
        catch (Exception ex)
        {
            Logger.Warn("Finance run log: could not mark run " + executionLogId + " as interrupted.", ex);
        }
    }

    public async Task WriteAsync(int executionLogId, int? employeeId, DateTime inPeriod, string message, int status,
                                 int noOfEmployee)
    {
        await _projectLogRepository.InsertAsync(new ProjectLog
        {
            TenantId = _session.GetTenantId(),
            EmployeeId = employeeId,
            InPeriod = inPeriod,
            Message = message,
            Status = status,
            RunTimeStamp = Clock.Now,
            NoOfEmployee = noOfEmployee,
            StopFlag = false
        });

        // Saving here also commits whatever the run did just before (an employee's cost register), which is
        // legacy's per-employee CommitChanges.
        await SaveAsync();
        await CountOnRunRecordAsync(executionLogId, status);
    }

    /// <summary>
    /// Counts the row on the run's OWN record, and refreshes its modification time -- the "still alive" signal
    /// <see cref="GetActiveRunAsync"/> relies on. Never throws.
    ///
    /// <para><b>Why the run record and not a recount of <c>ProjectLog</c>.</b> <c>ProjectLog</c> is shared with
    /// the Project Cost Engine, exactly as in legacy, and that engine empties it when it starts. Recounting the
    /// table (what legacy did) would then lose this run's counts mid-run or at its close. With nothing else
    /// writing the table the two give the same numbers.</para>
    /// </summary>
    private async Task CountOnRunRecordAsync(int executionLogId, int status)
    {
        try
        {
            var log = await _executionLogRepository.FirstOrDefaultAsync(executionLogId);
            if (log == null || log.ExecutionStatus != ExecutionLogStatus.INPRECESS)
            {
                return;
            }

            log.TotalRecords++;
            if (status == 0)
            {
                log.Successfull++;
            }
            else
            {
                log.Failed++;
            }

            log.LastModificationTime = Clock.Now;
            await _executionLogRepository.UpdateAsync(log);
            await SaveAsync();
        }
        catch (Exception ex)
        {
            Logger.Warn("Finance run log: heartbeat for run " + executionLogId + " failed.", ex);
        }
    }

    public async Task<bool> IsStopRequestedAsync(int executionLogId)
    {
        try
        {
            if (_projectLogRepository.GetAll().Any(l => l.StopFlag))
            {
                return true;
            }

            var item = await StopCache().GetOrDefaultAsync(StopKey(executionLogId));
            return item != null && item.Requested;
        }
        catch (Exception ex)
        {
            // A log hiccup must not stop a run, nor keep it from stopping on the legacy flag next time.
            Logger.Warn("Finance run log: could not read the stop request.", ex);
            return false;
        }
    }

    public async Task CloseAsync(int executionLogId)
    {
        try
        {
            var log = await _executionLogRepository.FirstOrDefaultAsync(executionLogId);
            if (log == null)
            {
                return;
            }

            // Legacy lines 669-686: a stop wins, then all-success / all-failed / partial. The counts are the
            // run's own (see CountOnRunRecordAsync) rather than a recount of the shared ProjectLog table.
            var isAborted = await IsStopRequestedAsync(executionLogId);

            log.ExecutionStatus = isAborted
                ? ExecutionLogStatus.ABORTED
                : log.TotalRecords == log.Successfull
                    ? ExecutionLogStatus.SUCCESS
                    : log.TotalRecords == log.Failed
                        ? ExecutionLogStatus.FAILED
                        : ExecutionLogStatus.PARTIAL;

            log.LastModificationTime = Clock.Now;

            await _executionLogRepository.UpdateAsync(log);
            await SaveAsync();
            await ForgetStopRequestAsync(executionLogId);
        }
        catch (Exception ex)
        {
            // Legacy swallowed this too (lines 687-689).
            Logger.Warn("Finance run log: could not close run " + executionLogId + ".", ex);
        }
    }

    public Task<FinanceActiveRun> GetActiveRunAsync()
    {
        var latest = LatestRun(null);
        if (latest == null || latest.ExecutionStatus != ExecutionLogStatus.INPRECESS || IsStale(latest))
        {
            return Task.FromResult<FinanceActiveRun>(null);
        }

        return Task.FromResult(new FinanceActiveRun
        {
            ExecutionLogId = latest.Id,
            Process = latest.ScreenName == PostJvScreenName ? FinanceRunProcess.PostJv : FinanceRunProcess.CostEngine,
            StartedBy = latest.UserName,
            StartedAt = latest.ActionDateTime
        });
    }

    #endregion

    #region << Stop / clear >>

    public async Task RequestStopAsync()
    {
        // Legacy lines 2216-2223: StopFlag on the FIRST existing ProjectLog row; nothing is inserted.
        var first = _projectLogRepository.GetAll().OrderBy(l => l.Id).FirstOrDefault();
        if (first != null)
        {
            first.StopFlag = true;
            await _projectLogRepository.UpdateAsync(first);
            await SaveAsync();
        }

        // Before the first row exists legacy's button did nothing at all, so a Stop pressed in the first
        // seconds of a run was lost. The running job is told directly as well.
        var active = await GetActiveRunAsync();
        if (active != null)
        {
            await StopCache().SetAsync(StopKey(active.ExecutionLogId), new FinanceRunFlagCacheItem { Requested = true },
                                       TimeSpan.FromDays(1));
        }
    }

    public async Task ClearAsync()
    {
        // Legacy TruncateLog. Deleted one by one through the repository so ABP's soft delete and tenant
        // filter apply; a raw TRUNCATE would empty every tenant's log.
        var ids = _projectLogRepository.GetAll().Select(l => l.Id).ToList();
        foreach (var id in ids)
        {
            await _projectLogRepository.DeleteAsync(id);
        }

        await SaveAsync();

        // After Clear Status legacy's label went back to blank ("NotStarted"), even for a finished run.
        var latest = LatestRun(null);
        if (latest != null && latest.ExecutionStatus != ExecutionLogStatus.INPRECESS)
        {
            await ClearedCache().SetAsync(TenantKey(), new FinanceRunClearedCacheItem { UpToExecutionLogId = latest.Id },
                                          TimeSpan.FromDays(7));
        }
    }

    #endregion

    #region << Status panel (legacy CalculateProcessStatus) >>

    public async Task<CostingProcessStatusDto> GetStatusAsync(FinanceRunProcess process)
    {
        var rows = _projectLogRepository.GetAll()
            .Select(l => new LogRow
            {
                Id = l.Id,
                Status = l.Status,
                StopFlag = l.StopFlag,
                RunTimeStamp = l.RunTimeStamp,
                NoOfEmployee = l.NoOfEmployee,
                EmployeeId = l.EmployeeId
            })
            .ToList();

        var all = rows.Count;
        var success = rows.Count(r => r.Status == 0);

        var status = new CostingProcessStatusDto
        {
            TotalRecords = all,
            SuccessRecords = success,

            // Legacy line 2490 clamps the failed count at zero rather than counting Status != 0.
            FailedRecords = all - success > 0 ? all - success : 0,
            StopRequested = rows.Any(r => r.StopFlag)
        };

        var latest = LatestRun(process);
        if (latest != null)
        {
            status.ExecutionLogId = latest.Id;
            status.StartedAt = latest.ActionDateTime;
            status.StartedBy = latest.UserName;

            var stopItem = await StopCache().GetOrDefaultAsync(StopKey(latest.Id));
            status.StopRequested = status.StopRequested || (stopItem != null && stopItem.Requested);
        }

        status.RunStatus = await DeriveRunStatusAsync(latest, all);
        status.IsRunning = status.RunStatus == FinanceRunStatus.Running;

        // The All / Successful / Failed counters come from the run's own record, so a Project Cost Engine run
        // emptying the shared ProjectLog (as legacy's did) does not reset them. Legacy recounted the table.
        if (latest != null && status.RunStatus != FinanceRunStatus.NotStarted)
        {
            status.TotalRecords = latest.TotalRecords;
            status.SuccessRecords = latest.Successfull;
            status.FailedRecords = latest.Failed;
        }

        ApplyProgress(status, rows);
        return status;
    }

    private async Task<FinanceRunStatus> DeriveRunStatusAsync(ExecutionLogRow latest, int rowCount)
    {
        if (latest == null)
        {
            return FinanceRunStatus.NotStarted;
        }

        if (latest.ExecutionStatus == ExecutionLogStatus.INPRECESS)
        {
            return IsStale(latest) ? FinanceRunStatus.Interrupted : FinanceRunStatus.Running;
        }

        var cleared = await ClearedCache().GetOrDefaultAsync(TenantKey());
        if (cleared != null && latest.Id <= cleared.UpToExecutionLogId && rowCount == 0)
        {
            return FinanceRunStatus.NotStarted;
        }

        // Legacy lines 2123-2156: a stop wins; otherwise any failed row means "completed with some errors".
        return latest.ExecutionStatus switch
        {
            ExecutionLogStatus.ABORTED => FinanceRunStatus.Stopped,
            ExecutionLogStatus.SUCCESS => FinanceRunStatus.Completed,
            _ => FinanceRunStatus.CompletedWithErrors
        };
    }

    /// <summary>Legacy lines 2497-2567: progress ring, current employee, speed and estimate.</summary>
    private void ApplyProgress(CostingProcessStatusDto status, List<LogRow> rows)
    {
        if (!rows.Any())
        {
            return;
        }

        var newest = rows.OrderByDescending(r => r.Id).First();
        status.NoOfEmployee = newest.NoOfEmployee;

        if (newest.NoOfEmployee > 0)
        {
            status.ProgressPercent = Math.Min(100,
                Convert.ToInt32(Math.Round(status.TotalRecords * 100.0 / newest.NoOfEmployee)));
        }

        // Legacy line 2152: the newest row (by time) that names an employee.
        var current = rows.Where(r => r.EmployeeId != null).OrderByDescending(r => r.RunTimeStamp).ThenByDescending(r => r.Id)
                          .FirstOrDefault();
        if (current != null)
        {
            var employee = _employeeRepository.GetAll()
                .Where(e => e.Id == current.EmployeeId.Value)
                .Select(e => new { e.Code, e.FullNameEn })
                .FirstOrDefault();

            if (employee != null)
            {
                var text = string.IsNullOrWhiteSpace(employee.FullNameEn)
                    ? employee.Code
                    : employee.Code + " " + employee.FullNameEn;

                status.CurrentEmployee = text != null && text.Length > CurrentEmployeeMaxLength
                    ? text.Substring(0, CurrentEmployeeMaxLength - 3) + "..."
                    : text;
            }
        }

        // Legacy's speed: rows written in the last minute of activity, per minute.
        var procEnd = newest;
        var procStartId = rows.Where(r => r.RunTimeStamp < procEnd.RunTimeStamp.AddMinutes(-1))
                              .Select(r => r.Id)
                              .DefaultIfEmpty(0)
                              .Max();
        if (procStartId == 0)
        {
            procStartId = newest.Id;
        }

        var procStart = rows.First(r => r.Id == procStartId);
        var processedInWindow = rows.Count(r => r.Id >= procStartId && r.Id <= procEnd.Id);
        var totalSeconds = (procEnd.RunTimeStamp - procStart.RunTimeStamp).TotalSeconds;

        if (totalSeconds != 0)
        {
            status.SpeedPerMinute = Math.Round(60 * processedInWindow / totalSeconds);
            if (status.SpeedPerMinute != 0)
            {
                var remaining = Math.Round((newest.NoOfEmployee - status.TotalRecords) / status.SpeedPerMinute);
                status.RemainingMinutes = remaining;
                status.EstimatedCompletion = Clock.Now.AddMinutes(remaining);
            }
        }
    }

    #endregion

    #region << Show details grid >>

    public Task<FinanceRunLogOutput> GetLogAsync(GetFinanceRunLogInput input)
    {
        input ??= new GetFinanceRunLogInput();

        var all = LoadGridRows();
        var filtered = ApplyGridFilters(all, input);

        var page = filtered
            .AsQueryable()
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToList();

        return Task.FromResult(new FinanceRunLogOutput
        {
            TotalCount = filtered.Count,
            Items = page,
            AllCount = all.Count,
            SuccessCount = all.Count(r => r.Status == 0),
            FailedCount = all.Count(r => r.Status == 1)
        });
    }

    public Task<List<FinanceRunLogDto>> GetLogRowsAsync(GetFinanceRunLogInput input)
    {
        input ??= new GetFinanceRunLogInput();

        var rows = ApplyGridFilters(LoadGridRows(), input)
            .AsQueryable()
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .ToList();

        return Task.FromResult(rows);
    }

    /// <summary>Legacy <c>SetLogGridDataSource</c> (lines 2943-2962): status 0 or 1 only, Arabic name for Arabic users.</summary>
    private List<FinanceRunLogDto> LoadGridRows()
    {
        var logs = _projectLogRepository.GetAll()
            .Where(l => l.Status == 0 || l.Status == 1)
            .Select(l => new { l.Id, l.EmployeeId, l.RunTimeStamp, l.Status, l.Message, l.InPeriod })
            .ToList();

        var employeeIds = logs.Where(l => l.EmployeeId != null).Select(l => l.EmployeeId.Value).Distinct().ToList();

        var employees = _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr })
            .ToList()
            .ToDictionary(e => e.Id);

        var arabic = CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

        return logs.Select(l =>
        {
            var hasEmployee = l.EmployeeId != null && employees.ContainsKey(l.EmployeeId.Value);
            var employee = hasEmployee ? employees[l.EmployeeId.Value] : null;

            return new FinanceRunLogDto
            {
                Id = l.Id,
                EmpCode = employee?.Code ?? "",
                Employee = employee == null ? "" : arabic ? employee.FullNameAr : employee.FullNameEn,
                RunTimeStamp = l.RunTimeStamp,
                Status = l.Status,
                Message = l.Message,
                InPeriod = l.InPeriod
            };
        }).ToList();
    }

    private static List<FinanceRunLogDto> ApplyGridFilters(List<FinanceRunLogDto> rows, GetFinanceRunLogInput input)
    {
        IEnumerable<FinanceRunLogDto> query = rows;

        if (input.StatusFilter == FinanceRunLogFilter.Successful)
        {
            query = query.Where(r => r.Status == 0);
        }
        else if (input.StatusFilter == FinanceRunLogFilter.Failed)
        {
            query = query.Where(r => r.Status == 1);
        }

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            var filter = input.Filter.Trim();
            query = query.Where(r =>
                (r.EmpCode ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (r.Employee ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (r.Message ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    #endregion

    #region << Helpers >>

    private ExecutionLogRow LatestRun(FinanceRunProcess? process)
    {
        var screenNames = process == null ? ScreenNames : new[] { ScreenNameOf(process.Value) };

        return _executionLogRepository.GetAll()
            .Where(l => l.Module == ExecutionLogModule.CostEngine && screenNames.Contains(l.ScreenName))
            .OrderByDescending(l => l.Id)
            .Select(l => new ExecutionLogRow
            {
                Id = l.Id,
                ScreenName = l.ScreenName,
                UserName = l.UserName,
                ActionDateTime = l.ActionDateTime,
                LastModificationTime = l.LastModificationTime,
                ExecutionStatus = l.ExecutionStatus,
                TotalRecords = l.TotalRecords,
                Successfull = l.Successfull,
                Failed = l.Failed
            })
            .FirstOrDefault();
    }

    /// <summary>No word from the run -- neither its record nor any log row -- for <see cref="StaleAfterMinutes"/>.</summary>
    private bool IsStale(ExecutionLogRow run)
    {
        var lastActivity = run.LastModificationTime ?? run.ActionDateTime;

        var lastRow = _projectLogRepository.GetAll()
            .OrderByDescending(l => l.Id)
            .Select(l => (DateTime?)l.RunTimeStamp)
            .FirstOrDefault();

        if (lastRow != null && lastRow.Value > lastActivity)
        {
            lastActivity = lastRow.Value;
        }

        return lastActivity < Clock.Now.AddMinutes(-StaleAfterMinutes);
    }

    private async Task SaveAsync()
    {
        var current = _unitOfWorkManager.Current;
        if (current != null)
        {
            await current.SaveChangesAsync();
        }
    }

    private async Task ForgetStopRequestAsync(int executionLogId)
    {
        try
        {
            await StopCache().RemoveAsync(StopKey(executionLogId));
        }
        catch (Exception ex)
        {
            Logger.Warn("Finance run log: could not clear the stop request of run " + executionLogId + ".", ex);
        }
    }

    private ITypedCache<string, FinanceRunFlagCacheItem> StopCache() =>
        _cacheManager.GetCache<string, FinanceRunFlagCacheItem>(StopCacheName);

    private ITypedCache<string, FinanceRunClearedCacheItem> ClearedCache() =>
        _cacheManager.GetCache<string, FinanceRunClearedCacheItem>(ClearedCacheName);

    private string TenantKey() => (_session.TenantId ?? 0).ToString(CultureInfo.InvariantCulture);

    private string StopKey(int executionLogId) => TenantKey() + ":" + executionLogId.ToString(CultureInfo.InvariantCulture);

    private class LogRow
    {
        public int Id { get; set; }
        public int Status { get; set; }
        public bool StopFlag { get; set; }
        public DateTime RunTimeStamp { get; set; }
        public int NoOfEmployee { get; set; }
        public int? EmployeeId { get; set; }
    }

    private class ExecutionLogRow
    {
        public int Id { get; set; }
        public string ScreenName { get; set; }
        public string UserName { get; set; }
        public DateTime ActionDateTime { get; set; }
        public DateTime? LastModificationTime { get; set; }
        public ExecutionLogStatus ExecutionStatus { get; set; }
        public int TotalRecords { get; set; }
        public int Successfull { get; set; }
        public int Failed { get; set; }
    }

    #endregion
}

/// <summary>Cache entry: a Stop pressed for one run.</summary>
[Serializable]
public class FinanceRunFlagCacheItem
{
    public bool Requested { get; set; }
}

/// <summary>Cache entry: Clear Status was pressed after this run finished.</summary>
[Serializable]
public class FinanceRunClearedCacheItem
{
    public int UpToExecutionLogId { get; set; }
}
