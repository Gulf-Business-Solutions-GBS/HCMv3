using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Timing;
using HCMv3.Configuration;
using HCMv3.EngineRuns;
using HCMv3.GoLiveSetup.Dto;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HCMv3.GoLiveSetup;

/// <summary>
/// Where the company's background payroll run stands, kept on the server so a reload, a locked screen or a
/// new login finds the same run instead of losing it.
///
/// <para>The state is one tenant setting -- what was asked for, when, and how it ended. How far each engine
/// has got is not copied into it: the engines already write that into their own <c>EngineRun</c> rows as they
/// go, and each batch is one row, so the progress is the sum of the rows written since the run started.</para>
/// </summary>
public class GoLiveRunTracker : ITransientDependency
{
    /// <summary>A run with no sign of life for this long has died under the server, not slowed down.</summary>
    private static readonly TimeSpan Silence = TimeSpan.FromMinutes(10);

    private readonly ISettingManager _settings;
    private readonly IAbpSession _session;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRepository<EngineRun, long> _engineRuns;

    public GoLiveRunTracker(
        ISettingManager settings,
        IAbpSession session,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<EngineRun, long> engineRuns)
    {
        _settings = settings;
        _session = session;
        _unitOfWorkManager = unitOfWorkManager;
        _engineRuns = engineRuns;
    }

    public async Task<GoLivePayrollRunState> LoadAsync()
    {
        var json = await _settings.GetSettingValueForTenantAsync(AppSettings.GoLive.PayrollRun, _session.GetTenantId());
        return string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<GoLivePayrollRunState>(json);
    }

    /// <summary>Saved and committed at once, so the screen polling for progress sees it while the run goes on.</summary>
    public async Task SaveAsync(GoLivePayrollRunState state)
    {
        using var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew);
        await _settings.ChangeSettingForTenantAsync(
            _session.GetTenantId(),
            AppSettings.GoLive.PayrollRun,
            JsonConvert.SerializeObject(state));
        await uow.CompleteAsync();
    }

    /// <summary>No run at all, as if none had ever started.</summary>
    public async Task ForgetAsync()
    {
        using var uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew);
        await _settings.ChangeSettingForTenantAsync(_session.GetTenantId(), AppSettings.GoLive.PayrollRun, string.Empty);
        await uow.CompleteAsync();
    }

    public async Task UpdateAsync(Action<GoLivePayrollRunState> change)
    {
        var state = await LoadAsync();
        if (state == null)
        {
            return;
        }

        change(state);
        await SaveAsync(state);
    }

    /// <summary>The last engine run written before this one started: everything after it is this run's.</summary>
    public async Task<long> LastEngineRunIdAsync() =>
        await _engineRuns.GetAll().Select(r => (long?)r.Id).MaxAsync() ?? 0;

    /// <summary>Honoured between batches, so what already ran stays saved.</summary>
    public async Task<bool> IsStopRequestedAsync()
    {
        var state = await LoadAsync();
        return state != null && state.StopRequested && IsGoing(state.Status);
    }

    public async Task<GoLivePayrollRunProgressDto> ProgressAsync()
    {
        var state = await LoadAsync();
        if (state == null)
        {
            return new GoLivePayrollRunProgressDto { Status = GoLiveRunStatus.None };
        }

        var runs = await _engineRuns.GetAll()
            .Where(r => r.Id > state.BaselineEngineRunId)
            .Select(r => new
            {
                r.EngineType,
                r.SucceededEmployees,
                r.FailedEmployees,
                r.SkippedEmployees,
                r.TotalProcessingMilliseconds,
                r.LastHeartbeatAt,
                r.StartedAt,
            })
            .ToListAsync();

        var time = runs.Where(r => r.EngineType == EngineType.TimeEngine).ToList();
        var payroll = runs.Where(r => r.EngineType == EngineType.PayrollEngine).ToList();
        var done = runs.Sum(r => r.SucceededEmployees + r.FailedEmployees + r.SkippedEmployees);

        var progress = new GoLivePayrollRunProgressDto
        {
            Status = state.Status,
            PayrollAreaName = state.PayrollAreaName,
            PayInDate = state.PayInDate,
            StartedAt = state.StartedAt,
            FinishedAt = state.FinishedAt,
            TotalEmployees = state.TotalEmployees,
            TimeDone = time.Sum(r => r.SucceededEmployees + r.FailedEmployees + r.SkippedEmployees),
            TimeFailed = time.Sum(r => r.FailedEmployees),
            PayrollDone = payroll.Sum(r => r.SucceededEmployees + r.FailedEmployees + r.SkippedEmployees),
            PayrollFailed = payroll.Sum(r => r.FailedEmployees),
            AverageMilliseconds = done == 0 ? 0 : (double)runs.Sum(r => r.TotalProcessingMilliseconds) / done,
            StopRequested = state.StopRequested,
            Result = state.Result,
        };

        // A run that has gone quiet died with the server: say so, and let a new one start.
        if (IsGoing(state.Status))
        {
            var lastSign = runs.Select(r => (DateTime?)r.LastHeartbeatAt).Max() ?? runs.Select(r => (DateTime?)r.StartedAt).Max() ?? state.StartedAt;
            if (lastSign.HasValue && Clock.Now - lastSign.Value > Silence)
            {
                progress.Status = GoLiveRunStatus.Failed;
            }
        }

        return progress;
    }

    public static bool IsGoing(GoLiveRunStatus status) =>
        status is GoLiveRunStatus.Queued or GoLiveRunStatus.Running;
}

/// <summary>The company's background payroll run, as kept in its tenant setting.</summary>
public class GoLivePayrollRunState
{
    public GoLiveRunStatus Status { get; set; }

    public string PayrollAreaName { get; set; }

    public DateTime PayInDate { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int TotalEmployees { get; set; }

    public long BaselineEngineRunId { get; set; }

    public bool StopRequested { get; set; }

    /// <summary>What Start already did before queuing -- the month set up, the leave pay links added -- to put back into the result.</summary>
    public GoLiveSetUpPayrollMonthOutput MonthSetUp { get; set; }

    public int LeavePayLinksAdded { get; set; }

    public GoLiveRunPayrollOutput Result { get; set; }
}
