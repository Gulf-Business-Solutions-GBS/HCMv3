using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.BackgroundJobs;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Localization;
using Abp.Timing;
using HCMv3.Authorization;
using HCMv3.BackgroundJobs.Dto;
using HCMv3.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.BackgroundJobs;

/// <inheritdoc cref="IBackgroundJobsAppService"/>
[AbpAuthorize(AppPermissions.Pages_Administration_BackgroundJobs)]
public class BackgroundJobsAppService : HCMv3AppServiceBase, IBackgroundJobsAppService
{
    /// <summary>Enough to see a pattern; the rest of the month is in the log.</summary>
    private const int MaxFailuresShown = 100;

    private readonly IRepository<BackgroundWorkerStatus> _statusRepository;
    private readonly IRepository<BackgroundJobInfo, long> _jobRepository;
    private readonly IRepository<BackgroundJobFailure, long> _failureRepository;
    private readonly IRepository<Tenant> _tenantRepository;

    public BackgroundJobsAppService(
        IRepository<BackgroundWorkerStatus> statusRepository,
        IRepository<BackgroundJobInfo, long> jobRepository,
        IRepository<BackgroundJobFailure, long> failureRepository,
        IRepository<Tenant> tenantRepository)
    {
        _statusRepository = statusRepository;
        _jobRepository = jobRepository;
        _failureRepository = failureRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<BackgroundJobsOverviewDto> GetOverview()
    {
        var isHost = !AbpSession.TenantId.HasValue;

        var overview = new BackgroundJobsOverviewDto
        {
            IsHost = isHost,
            GeneratedTime = Clock.Now
        };

        overview.ScheduledTasks = await GetScheduledTasksAsync(isHost);

        var failures = await GetRecentFailuresAsync(isHost);
        overview.QueuedJobs = await GetQueuedJobsAsync(isHost, failures);
        overview.RecentFailures = failures;

        if (isHost)
        {
            await FillTenantNamesAsync(overview);
        }

        return overview;
    }

    // ------------------------------------------------------------------ scheduled tasks

    private async Task<List<ScheduledTaskDto>> GetScheduledTasksAsync(bool isHost)
    {
        List<BackgroundWorkerStatus> statuses;

        // The status rows are host-level; read them without the caller's company getting in the way.
        using (CurrentUnitOfWork.SetTenantId(null))
        {
            statuses = await _statusRepository.GetAll().ToListAsync();
        }

        var byName = statuses.ToDictionary(s => s.WorkerName, StringComparer.Ordinal);

        // Every recorded task, including the ones that have not run yet: a task missing from the list would look like
        // a task that does not exist, when the real news is that it has not run.
        var names = KnownWorkerNames().Union(byName.Keys).OrderBy(n => n, StringComparer.Ordinal);

        return names
            .Select(name =>
            {
                byName.TryGetValue(name, out var status);

                return new ScheduledTaskDto
                {
                    WorkerName = name,
                    DisplayName = DisplayName("BackgroundTask_", name, "BackgroundWorker", "Worker"),
                    HasRun = status != null,
                    IntervalMinutes = status == null ? null : Math.Round(status.IntervalMilliseconds / 60000d, 2),
                    LastStartTime = status?.LastStartTime,
                    LastEndTime = status?.LastEndTime,
                    LastSucceeded = status?.LastSucceeded,
                    LastSuccessTime = status?.LastSuccessTime,
                    LastFailureTime = status?.LastFailureTime,
                    LastError = isHost ? status?.LastError : null,
                    RunCount = status?.RunCount ?? 0,
                    FailureCount = status?.FailureCount ?? 0
                };
            })
            .ToList();
    }

    /// <summary>
    /// The tasks built on the recording base classes, found by type rather than listed by hand, so a new task appears on
    /// the screen without anybody remembering to add it here.
    /// </summary>
    public static IReadOnlyList<string> KnownWorkerNames()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("HCMv3", StringComparison.Ordinal) == true)
            .SelectMany(SafeTypes)
            .Where(t => !t.IsAbstract
                        && (typeof(RecordedPeriodicBackgroundWorkerBase).IsAssignableFrom(t)
                            || typeof(RecordedAsyncPeriodicBackgroundWorkerBase).IsAssignableFrom(t)))
            .Select(t => t.Name)
            .Distinct()
            .ToList();
    }

    private static IEnumerable<Type> SafeTypes(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null);
        }
    }

    // ------------------------------------------------------------------ queued jobs

    private async Task<List<QueuedJobDto>> GetQueuedJobsAsync(bool isHost, List<BackgroundJobFailureDto> failures)
    {
        // AbpBackgroundJobs has no company column, so the company is read from each job's arguments. The queue only
        // ever holds unfinished jobs -- ABP deletes a job once it succeeds -- so reading it whole is cheap.
        var jobs = await _jobRepository.GetAll().OrderBy(j => j.NextTryTime).ToListAsync();

        var lastErrorByJob = failures
            .GroupBy(f => f.JobId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.FailedTime).First().Error);

        return jobs
            .Select(job => new { Job = job, TenantId = BackgroundJobFailureRecorder.TenantIdOf(job.JobArgs) })
            // A company sees its own jobs only. A job whose company cannot be read counts as the host's.
            .Where(x => isHost || x.TenantId == AbpSession.TenantId)
            .Select(x =>
            {
                var name = BackgroundJobFailureRecorder.JobNameOf(x.Job.JobType);

                return new QueuedJobDto
                {
                    Id = x.Job.Id,
                    JobName = name,
                    DisplayName = DisplayName("BackgroundJob_", name, "BackgroundJob", "Job"),
                    TenantId = x.TenantId,
                    State = x.Job.IsAbandoned
                        ? QueuedJobState.Abandoned
                        : x.Job.TryCount > 0 ? QueuedJobState.Retrying : QueuedJobState.Waiting,
                    TryCount = x.Job.TryCount,
                    CreationTime = x.Job.CreationTime,
                    LastTryTime = x.Job.LastTryTime,
                    NextTryTime = x.Job.NextTryTime,
                    LastError = lastErrorByJob.TryGetValue(x.Job.Id, out var error) ? error : null
                };
            })
            .ToList();
    }

    // ------------------------------------------------------------------ failures

    private async Task<List<BackgroundJobFailureDto>> GetRecentFailuresAsync(bool isHost)
    {
        List<BackgroundJobFailure> rows;

        // For a company the tenant filter already keeps it to its own failures. The host sees them all -- by default the
        // filter would show the host only its own host-level rows.
        if (isHost)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                rows = await RecentFailuresQuery().ToListAsync();
            }
        }
        else
        {
            rows = await RecentFailuresQuery().ToListAsync();
        }

        return rows
            .Select(f => new BackgroundJobFailureDto
            {
                JobId = f.JobId,
                JobName = f.JobName,
                DisplayName = DisplayName("BackgroundJob_", f.JobName, "BackgroundJob", "Job"),
                TenantId = f.TenantId,
                TryCount = f.TryCount,
                FailedTime = f.FailedTime,
                Error = f.Error
            })
            .ToList();
    }

    private IQueryable<BackgroundJobFailure> RecentFailuresQuery()
    {
        var cutoff = Clock.Now.AddDays(-BackgroundJobsConsts.FailureRetentionDays);

        return _failureRepository.GetAll()
            .Where(f => f.FailedTime >= cutoff)
            .OrderByDescending(f => f.FailedTime)
            .Take(MaxFailuresShown);
    }

    private async Task FillTenantNamesAsync(BackgroundJobsOverviewDto overview)
    {
        var ids = overview.QueuedJobs.Select(j => j.TenantId)
            .Concat(overview.RecentFailures.Select(f => f.TenantId))
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return;
        }

        var names = await _tenantRepository.GetAll()
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name);

        foreach (var job in overview.QueuedJobs.Where(j => j.TenantId.HasValue))
        {
            job.TenantName = names.TryGetValue(job.TenantId.Value, out var name) ? name : null;
        }

        foreach (var failure in overview.RecentFailures.Where(f => f.TenantId.HasValue))
        {
            failure.TenantName = names.TryGetValue(failure.TenantId.Value, out var name) ? name : null;
        }
    }

    // ------------------------------------------------------------------ names

    /// <summary>
    /// The translated name when there is one; otherwise the class name without its technical suffix, spaced out --
    /// "TimeEngineAutorunBackgroundWorker" reads as "Time Engine Autorun".
    /// </summary>
    private string DisplayName(string keyPrefix, string typeName, params string[] suffixes)
    {
        var translated = LocalizationManager.GetSource(HCMv3Consts.LocalizationSourceName).GetStringOrNull(keyPrefix + typeName);

        if (translated != null)
        {
            return translated;
        }

        var name = typeName;

        foreach (var suffix in suffixes)
        {
            if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - suffix.Length);
                break;
            }
        }

        return Humanize(name);
    }

    public static string Humanize(string name)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            var startsWord = i > 0 && char.IsUpper(c)
                             && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1])));

            if (startsWord)
            {
                builder.Append(' ');
            }

            builder.Append(c);
        }

        return builder.ToString();
    }
}
