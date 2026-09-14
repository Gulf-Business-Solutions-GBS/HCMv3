using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.BackgroundJobs;

/// <summary>
/// The latest run of one scheduled background task -- the time engine auto-run, report distribution, workflow
/// escalation and the rest.
///
/// <para>
/// <b>One row per task, updated in place, not a row per run.</b> Two of these tasks run every few minutes, so a
/// history would grow by thousands of rows a week to answer a question -- "did it run, and did it work?" -- that
/// the latest run already answers. The counts keep the long view without the rows.
/// </para>
///
/// <para>
/// Host-level, with no tenant: each task runs once for the whole system and loops over the companies itself.
/// That is why a company admin sees when a task ran but not its error, which can name another company.
/// </para>
/// </summary>
public class BackgroundWorkerStatus : FullAuditedEntity
{
    /// <summary>The worker's class name, e.g. <c>TimeEngineAutorunBackgroundWorker</c>. Unique.</summary>
    [Required]
    [StringLength(BackgroundJobsConsts.MaxNameLength)]
    public virtual string WorkerName { get; set; }

    /// <summary>How often the task is scheduled to run, in milliseconds, as it was on its last run.</summary>
    public virtual int IntervalMilliseconds { get; set; }

    public virtual DateTime? LastStartTime { get; set; }

    public virtual DateTime? LastEndTime { get; set; }

    public virtual bool? LastSucceeded { get; set; }

    public virtual DateTime? LastSuccessTime { get; set; }

    public virtual DateTime? LastFailureTime { get; set; }

    /// <summary>The message of the most recent failure. Kept after later successes, so a recurring fault stays visible.</summary>
    [StringLength(BackgroundJobsConsts.MaxErrorLength)]
    public virtual string LastError { get; set; }

    public virtual long RunCount { get; set; }

    public virtual long FailureCount { get; set; }
}
