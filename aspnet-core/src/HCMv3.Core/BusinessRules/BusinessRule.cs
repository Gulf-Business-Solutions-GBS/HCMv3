using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.BusinessRules;

/// <summary>
/// A business rule authored by a configurer rather than a developer: a condition tree and the actions to
/// take when it holds, stored as JSON in <see cref="DefinitionJson"/> and evaluated by
/// <c>IRuleEvaluator</c>.
///
/// <para>
/// This is HCMv3's answer to the same problem SAP SuccessFactors solves with its Business Rules admin
/// screen. Rules that today are compiled-in one-liners -- probation end date derived in
/// <c>EmployeesAppService.ApplyComputedFields</c>, approval chains chosen in <c>RequestMasterResolver</c> --
/// become configuration a tenant can change without a release. The point is not to make everything
/// configurable; it is to move the handful of rules that genuinely differ per customer out of the
/// deployment cycle.
/// </para>
///
/// <para><b>A rule on its own does nothing.</b> It is inert until a <see cref="BusinessRuleAssignment"/>
/// binds it to a base object and a lifecycle event. Authoring and arming are separate permissions
/// (<c>Pages.BusinessRules.Edit</c> vs <c>Pages.BusinessRules.Assign</c>) for exactly that reason: drafting
/// a rule must never be able to change production behaviour by accident.</para>
///
/// <para><b>Effective dating.</b> <see cref="EffectiveFrom"/>/<see cref="EffectiveTo"/> follow the same
/// convention as the rest of the port -- inclusive start, inclusive end, null end meaning open. The
/// authoring screen's "As Of Date" filter reads them, and the runner evaluates them against the execution
/// date so a rule can be scheduled ahead of a policy change rather than switched over by hand at midnight.
/// </para>
///
/// <para><b>Why the definition is JSON and not columns.</b> A rule tree is recursive and its shape varies
/// per rule; normalising it would mean a self-referencing node table and a join per node on a code path
/// that runs inside every save. The trade-off accepted here is that the database cannot validate the tree --
/// so <c>IRuleDefinitionParser</c> is the only thing that may construct one, and it rejects unknown node
/// kinds, unknown fields and over-deep nesting rather than tolerating them.</para>
/// </summary>
public class BusinessRule : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>
    /// Stable identifier used by seed data, imports and any code that needs to find a specific rule
    /// (<c>PROBATION_END_DATE</c>). Unique per tenant. Business-visible, so it is chosen by the author
    /// rather than generated.
    /// </summary>
    [Required]
    [StringLength(BusinessRuleConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(BusinessRuleConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [StringLength(BusinessRuleConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    [StringLength(BusinessRuleConsts.MaxDescriptionLength)]
    public virtual string Description { get; set; }

    /// <summary>
    /// The <c>Code</c> of the <c>RuleBaseObjectDescriptor</c> this rule is written against -- "Employee",
    /// "EmployeeAbsence". Determines which fields and parameters the author may reference, and an assignment
    /// may only bind the rule to that same base object.
    /// </summary>
    [Required]
    [StringLength(BusinessRuleConsts.MaxBaseObjectLength)]
    public virtual string BaseObject { get; set; }

    /// <summary>
    /// What the rule is allowed to do. Enforced by the runner, not merely documented: a
    /// <see cref="RulePurpose.Validate"/> rule's writes are discarded even when it is assigned to a saving
    /// event, so re-purposing a rule by moving its assignment cannot turn a check into a mutation.
    /// </summary>
    public virtual RulePurpose Purpose { get; set; }

    /// <summary>The authored tree. Only <c>IRuleDefinitionParser</c> may interpret this.</summary>
    [Required]
    [StringLength(BusinessRuleConsts.MaxDefinitionLength)]
    public virtual string DefinitionJson { get; set; }

    /// <summary>Inclusive. Rules default to 1900-01-01 the way the rest of the port dates open-ended rows.</summary>
    public virtual DateTime EffectiveFrom { get; set; }

    /// <summary>Inclusive; null means open-ended.</summary>
    public virtual DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// A master off switch independent of dating, so a misbehaving rule can be stopped instantly without
    /// anyone having to reason about effective dates under pressure.
    /// </summary>
    public virtual bool IsActive { get; set; }

    public virtual ICollection<BusinessRuleAssignment> Assignments { get; set; }
}
