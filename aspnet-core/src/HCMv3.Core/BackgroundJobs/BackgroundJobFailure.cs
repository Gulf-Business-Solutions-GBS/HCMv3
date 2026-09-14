using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.BackgroundJobs;

/// <summary>
/// One failed attempt at a queued background job -- a payroll run, a time engine run, a bulk import.
///
/// <para>
/// <b>Why it has to exist.</b> ABP keeps queued jobs in AbpBackgroundJobs only until they finish, and does not keep
/// the error at all: a failed attempt just raises the retry count, and the reason goes to the log. Nobody reading the
/// app could tell why a payroll run is retrying, or that one gave up yesterday. This row is written from the event ABP
/// raises on each failure, so the reason survives the job.
/// </para>
///
/// <para>
/// <b>Belongs to the job's company.</b> AbpBackgroundJobs has no company column; the company is read from the job's
/// own arguments, every one of which carries a TenantId. <see cref="IMayHaveTenant"/> then keeps each company to its
/// own failures without any filtering code, and a host-level job stays with the host.
/// </para>
/// </summary>
public class BackgroundJobFailure : FullAuditedEntity<long>, IMayHaveTenant
{
    public virtual int? TenantId { get; set; }

    /// <summary>The AbpBackgroundJobs row that failed. It may already be gone.</summary>
    public virtual long JobId { get; set; }

    /// <summary>The job's class name, e.g. <c>PayrollEngineBackgroundJob</c>.</summary>
    [Required]
    [StringLength(BackgroundJobsConsts.MaxNameLength)]
    public virtual string JobName { get; set; }

    /// <summary>Which attempt this was. The first attempt is 1.</summary>
    public virtual int TryCount { get; set; }

    public virtual DateTime FailedTime { get; set; }

    [StringLength(BackgroundJobsConsts.MaxErrorLength)]
    public virtual string Error { get; set; }
}
