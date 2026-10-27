using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.EngineRuns;

/// <summary>
/// Run header for a long-running calculation engine (Time Engine / Payroll Engine), maintained LIVE while
/// the run is in flight.
///
/// <para><b>Why this is not <c>ExecutionLog</c>.</b> <c>ExecutionLog</c> is inserted with
/// <c>INPRECESS</c> at the start of a run and updated in the <c>finally</c>, but both writes happen inside
/// the run's single outer unit of work -- so no other connection can see the row until the run commits, i.e.
/// exactly when progress stops being useful. <c>ExecutionLog</c> also has no heartbeat, no expected-employee
/// total, and no measured timings. This table is written through
/// <see cref="Services.IEngineRunProgressWriter"/>, which commits every write in its OWN unit of work
/// (<c>TransactionScopeOption.RequiresNew</c>), so a concurrent reader sees progress mid-run.</para>
///
/// <para><b>Locking contract -- do not break it.</b> Nothing inside the engines' main transaction may read
/// or write this table or <see cref="EngineRunEmployee"/>. If the main transaction ever touched them, the
/// status endpoint would block on its locks and the progress screen would hang, which is worse than having
/// no progress at all.</para>
///
/// <para><see cref="ExecutionLogId"/> is a plain int, deliberately NOT an EF navigation/FK: the
/// <c>ExecutionLog</c> row is created inside the engine's outer transaction and is therefore not yet visible
/// (and not yet committed) to the separate connection this row is written on. A real FK would fail the
/// constraint check. It is a soft correlation, populated when the run finalises.</para>
/// </summary>
public class EngineRun : FullAuditedEntity<long>, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual EngineType EngineType { get; set; }

    /// <summary>Correlation id the caller can hold on to across polls. Opaque to the server.</summary>
    [StringLength(64)]
    public virtual string RunKey { get; set; }

    /// <summary>Soft correlation to <c>SystemSecurity.ExecutionLog</c>. See class remarks for why it is not an FK.</summary>
    public virtual int? ExecutionLogId { get; set; }

    [StringLength(256)]
    public virtual string StartedByUserName { get; set; }

    /// <summary>The same pipe-separated parameter string the engines write onto <c>ExecutionLog.Parameters</c>.</summary>
    public virtual string Parameters { get; set; }

    public virtual DateTime StartedAt { get; set; }

    /// <summary>
    /// Refreshed by the engine as it works. A <see cref="EngineRunStatus.Running"/> row whose heartbeat is
    /// older than <c>EngineRunConsts.StaleHeartbeatSeconds</c> is a crashed/killed run -- see
    /// <see cref="EngineRunStatus.Abandoned"/>.
    /// </summary>
    public virtual DateTime LastHeartbeatAt { get; set; }

    public virtual DateTime? CompletedAt { get; set; }

    public virtual EngineRunStatus Status { get; set; }

    /// <summary>
    /// Employees selected for the run. Known only after employee selection, so it is 0 for the brief window
    /// between the run starting and the selection query returning.
    /// </summary>
    public virtual int TotalEmployees { get; set; }

    public virtual int SucceededEmployees { get; set; }

    public virtual int FailedEmployees { get; set; }

    /// <summary>
    /// Employees the engine decided not to process at all (legacy's "NoofEmp = NoofEmp - 1; continue;"
    /// branches). They are neither successes nor failures and get no <see cref="EngineRunEmployee"/> row,
    /// but they must be subtracted from the remaining count or the ETA never reaches zero.
    /// </summary>
    public virtual int SkippedEmployees { get; set; }

    /// <summary>
    /// Sum of measured per-employee processing time. Divided by
    /// <see cref="SucceededEmployees"/> + <see cref="FailedEmployees"/> this is the plain average the ETA is
    /// based on -- measured, never assumed.
    /// </summary>
    public virtual long TotalProcessingMilliseconds { get; set; }

    /// <summary>
    /// Set by the operator's Stop button. Read by the engine loop through its OWN unit of work, so the
    /// request is seen mid-run. This supersedes the legacy <c>TimeLog.StopFlag</c>/<c>PayrollLog.StopFlag</c>
    /// mechanism for API-initiated stops -- writing those tables from outside would contend with the engine's
    /// open transaction. The legacy flag checks are still in place and still honoured.
    /// </summary>
    public virtual bool StopRequested { get; set; }

    /// <summary>Run-level failure message (not per-employee) when <see cref="Status"/> is <see cref="EngineRunStatus.Failed"/>.</summary>
    public virtual string FailureMessage { get; set; }
}
