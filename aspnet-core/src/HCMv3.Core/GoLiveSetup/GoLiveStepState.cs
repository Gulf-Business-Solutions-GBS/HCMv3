using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GoLiveSetup;

/// <summary>
/// What the customer has entered on one Go-Live Setup step, so it survives a refresh.
///
/// <para><b>Why a JSON payload rather than columns.</b> The steps are declared in a client-side registry
/// that changes as questions are reworded, added and reordered; a schema mirroring it would need a
/// migration every time. Nothing here is queried by the payroll engine either -- it is a draft the
/// customer is still editing. It becomes real entities only when Apply runs, and that is where the
/// typed schema lives. Until then this is deliberately opaque storage.</para>
///
/// <para>One row per tenant per step rather than one blob per tenant, so a 2,000-row employee sheet is
/// not rewritten every time somebody answers a question two steps away, and so two people editing
/// different steps do not overwrite each other.</para>
/// </summary>
public class GoLiveStepState : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>Registry step id, e.g. <c>S1.2</c> or <c>S3.1</c>. Unique per tenant.</summary>
    [Required]
    [StringLength(GoLiveSetupConsts.MaxStepIdLength)]
    public virtual string StepId { get; set; }

    /// <summary>
    /// The step's contents as JSON. A questions step stores <c>{ answers, notes }</c>; a data step
    /// stores <c>{ rows }</c>. Shape is owned by the client and validated when Apply reads it, not here.
    /// </summary>
    public virtual string Payload { get; set; }
}
