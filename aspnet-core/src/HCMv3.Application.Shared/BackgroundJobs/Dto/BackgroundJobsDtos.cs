using System;
using System.Collections.Generic;

namespace HCMv3.BackgroundJobs.Dto;

/// <summary>Everything the Background Jobs screen shows, in one call.</summary>
public class BackgroundJobsOverviewDto
{
    /// <summary>
    /// True when the caller is the host. Company names, and the errors of system-wide tasks, are only filled in for
    /// the host; the screen uses this to say why those columns are empty for a company.
    /// </summary>
    public bool IsHost { get; set; }

    public DateTime GeneratedTime { get; set; }

    public List<ScheduledTaskDto> ScheduledTasks { get; set; } = new();

    public List<QueuedJobDto> QueuedJobs { get; set; } = new();

    public List<BackgroundJobFailureDto> RecentFailures { get; set; } = new();
}

/// <summary>A task that runs on a timer, and how its latest run went.</summary>
public class ScheduledTaskDto
{
    public string WorkerName { get; set; }

    public string DisplayName { get; set; }

    /// <summary>How often it runs, in minutes. Null when it has not run since recording began.</summary>
    public double? IntervalMinutes { get; set; }

    /// <summary>False until the task's first recorded run.</summary>
    public bool HasRun { get; set; }

    public DateTime? LastStartTime { get; set; }

    public DateTime? LastEndTime { get; set; }

    public bool? LastSucceeded { get; set; }

    public DateTime? LastSuccessTime { get; set; }

    public DateTime? LastFailureTime { get; set; }

    /// <summary>The most recent failure's message. Host only: a system-wide task's error can name any company.</summary>
    public string LastError { get; set; }

    public long RunCount { get; set; }

    public long FailureCount { get; set; }
}

/// <summary>Where a queued job stands. A job that finished is gone from the queue, so it never appears here.</summary>
public enum QueuedJobState
{
    /// <summary>Not tried yet.</summary>
    Waiting = 1,

    /// <summary>Failed at least once and will be tried again.</summary>
    Retrying = 2,

    /// <summary>Failed too often, or for too long, and will not be tried again.</summary>
    Abandoned = 3
}

/// <summary>A job waiting in, or stuck in, the queue.</summary>
public class QueuedJobDto
{
    public long Id { get; set; }

    public string JobName { get; set; }

    public string DisplayName { get; set; }

    public int? TenantId { get; set; }

    /// <summary>Host only.</summary>
    public string TenantName { get; set; }

    public QueuedJobState State { get; set; }

    public int TryCount { get; set; }

    public DateTime CreationTime { get; set; }

    public DateTime? LastTryTime { get; set; }

    public DateTime NextTryTime { get; set; }

    /// <summary>The error of this job's latest failed attempt, when one was recorded.</summary>
    public string LastError { get; set; }
}

/// <summary>One failed attempt at a queued job, kept after the job itself is gone.</summary>
public class BackgroundJobFailureDto
{
    public long JobId { get; set; }

    public string JobName { get; set; }

    public string DisplayName { get; set; }

    public int? TenantId { get; set; }

    /// <summary>Host only.</summary>
    public string TenantName { get; set; }

    public int TryCount { get; set; }

    public DateTime FailedTime { get; set; }

    public string Error { get; set; }
}
