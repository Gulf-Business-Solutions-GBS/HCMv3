using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.EngineRuns;

/// <summary>
/// One row per employee actually processed by an engine run -- succeeded AND failed -- written the moment
/// that employee finishes, in its own committed transaction. This is what backs the operator grid.
///
/// <para><b>Why not reuse <c>TimeLog</c>/<c>PayrollLog</c>.</b> Three concrete reasons, not preference:
/// <list type="number">
/// <item><description>They are not run-scoped. Both engines' finalisers deliberately count <i>every</i> row
/// in those tables rather than the current run's (a preserved legacy quirk that assumes the table is
/// truncated between runs), so there is no way to ask "which employees did THIS run process".</description></item>
/// <item><description><c>TimeLog</c> is written per employee PER DAY, not per employee. A 31-day run over
/// 500 employees writes 15 500 rows; counting them answers a different question than "how many employees are
/// done".</description></item>
/// <item><description>Neither carries a measured duration, so an average-per-employee could only be
/// guessed from timestamp deltas between unrelated rows.</description></item>
/// </list>
/// Those tables keep their existing meaning and are untouched by this feature.</para>
///
/// <para><see cref="EngineRunId"/> is a plain column rather than an EF relationship for the same reason
/// <see cref="EngineRun.ExecutionLogId"/> is: these rows are written on a short-lived independent
/// connection, and keeping the write dependency-free keeps it fast and non-blocking.</para>
///
/// <para>Plain <see cref="Abp.Domain.Entities.Entity{TPrimaryKey}"/>-style auditing via
/// <see cref="FullAuditedEntity{TPrimaryKey}"/> is used for consistency with the rest of the codebase; the
/// row is append-only in practice.</para>
/// </summary>
public class EngineRunEmployee : FullAuditedEntity<long>, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual long EngineRunId { get; set; }

    /// <summary>1-based position in the run's employee list. Gives the grid a stable "processing order" sort.</summary>
    public virtual int SequenceNo { get; set; }

    /// <summary>Nullable to match <c>TimeLog</c>/<c>PayrollLog</c>, which allow employee-less rows.</summary>
    public virtual int? EmployeeId { get; set; }

    [StringLength(64)]
    public virtual string EmployeeCode { get; set; }

    [StringLength(512)]
    public virtual string EmployeeName { get; set; }

    public virtual bool Succeeded { get; set; }

    /// <summary>
    /// "Successfull" on success (the engines' own literal, preserved), otherwise the exception message
    /// truncated to <c>EngineRunConsts.MaxMessageLength</c> exactly as the engines truncate their log rows.
    /// Unbounded column -- no <c>[StringLength]</c> -- same pattern as <c>PayrollLog.Message</c>.
    /// </summary>
    public virtual string Message { get; set; }

    public virtual DateTime StartedAt { get; set; }

    public virtual DateTime CompletedAt { get; set; }

    /// <summary>Measured with a <see cref="System.Diagnostics.Stopwatch"/> around the per-employee call.</summary>
    public virtual int ElapsedMilliseconds { get; set; }
}
