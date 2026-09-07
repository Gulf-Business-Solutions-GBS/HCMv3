using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.BusinessRules;

/// <summary>
/// Binds a <see cref="BusinessRule"/> to a base object and a lifecycle event. This is the row that makes a
/// rule live, and it is what the "Assigned" column of the rule list reports.
///
/// <para>
/// Kept separate from the rule itself because the relationship is genuinely many-to-many in practice: one
/// validation rule is commonly armed on both <see cref="RuleEventType.BeforeSave"/> and
/// <see cref="RuleEventType.OnValidate"/>, and the same derivation may be wanted on
/// <see cref="RuleEventType.OnInit"/> as a default and again on save as a guard. Folding the event onto the
/// rule would force authors to copy-paste the tree, and copies drift.
/// </para>
/// </summary>
public class BusinessRuleAssignment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int BusinessRuleId { get; set; }

    public virtual BusinessRule BusinessRule { get; set; }

    /// <summary>
    /// Must equal the rule's own <see cref="BusinessRule.BaseObject"/>. Denormalised onto the assignment so
    /// the runner's hot-path lookup -- "every rule armed on Employee/BeforeSave" -- is a single indexed
    /// query with no join back to the rule table.
    /// </summary>
    [Required]
    [StringLength(BusinessRuleConsts.MaxBaseObjectLength)]
    public virtual string BaseObject { get; set; }

    public virtual RuleEventType EventType { get; set; }

    /// <summary>
    /// Ascending. Rules on the same event run in this order and see each other's writes, which is what lets
    /// one rule derive a value and the next validate it. Ties break on Id so the order is at least stable.
    /// </summary>
    public virtual int ExecutionOrder { get; set; }

    /// <summary>
    /// Disarms this binding without deleting it, so an assignment can be switched off and back on without
    /// losing its execution order.
    /// </summary>
    public virtual bool IsActive { get; set; }

    /// <summary>
    /// When true, a failure inside this rule is logged and execution continues to the next rule instead of
    /// aborting the operation.
    ///
    /// <para>
    /// Defaults to false, and that default matters: a rule that silently does nothing is how a probation
    /// date quietly stops being computed for six months. Opt in only where a rule is genuinely advisory.
    /// </para>
    /// </summary>
    public virtual bool ContinueOnError { get; set; }

    /// <summary>
    /// Write an execution log row for every run of this assignment, not just its failures.
    ///
    /// <para>
    /// Off by default and meant to be switched on temporarily. Tracing a BeforeSave assignment on Employee
    /// writes one row per employee save, which is why it is a per-assignment switch rather than a global
    /// one -- somebody investigating a probation-date complaint can trace exactly that rule for an
    /// afternoon without the log filling with everything else.
    /// </para>
    /// </summary>
    public virtual bool IsTraced { get; set; }
}
