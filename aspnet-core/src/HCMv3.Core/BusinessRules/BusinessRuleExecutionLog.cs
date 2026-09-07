using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.BusinessRules;

/// <summary>
/// One recorded execution of a rule.
///
/// <para>
/// Configurable behaviour is undebuggable without it. When a user reports "the probation date is wrong",
/// the question is which rule fired, in what order, against what values -- and with the logic living in a
/// database row rather than in source, there is no stack trace and no commit to read. This table is the
/// substitute.
/// </para>
///
/// <para><b>It is deliberately not written for every execution.</b> Logging every BeforeSave rule on every
/// employee save would out-write the audit log. The runner records failures always, and successes only when
/// the assignment is being traced -- see <c>IBusinessRuleRunner</c>.</para>
/// </summary>
public class BusinessRuleExecutionLog : CreationAuditedEntity<long>, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int BusinessRuleId { get; set; }

    /// <summary>
    /// Copied, not joined. A log entry has to stay readable after its rule is renamed or deleted -- the
    /// whole point is explaining behaviour that has since been changed.
    /// </summary>
    [Required]
    [StringLength(BusinessRuleConsts.MaxCodeLength)]
    public virtual string RuleCode { get; set; }

    [Required]
    [StringLength(BusinessRuleConsts.MaxBaseObjectLength)]
    public virtual string BaseObject { get; set; }

    public virtual RuleEventType EventType { get; set; }

    /// <summary>Primary key of the row the rule ran against, when there was one. Null for a test run.</summary>
    public virtual int? TargetEntityId { get; set; }

    public virtual bool Succeeded { get; set; }

    /// <summary>Whether the condition held. A rule that ran and did nothing is a different answer from a rule that never ran.</summary>
    public virtual bool ConditionMatched { get; set; }

    /// <summary>
    /// Human-readable trace: the fields read, the values seen, the actions applied. Truncated rather than
    /// allowed to grow without bound.
    /// </summary>
    [StringLength(BusinessRuleConsts.MaxDescriptionLength)]
    public virtual string Message { get; set; }

    public virtual int DurationMs { get; set; }

    public virtual DateTime ExecutedAt { get; set; }
}
