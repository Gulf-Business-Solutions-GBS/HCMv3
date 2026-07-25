using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Workflow;

/// <summary>
/// Per-step field visibility/edit/require configuration for a <see cref="WorkflowGroupDetail"/> (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkflowGroupFieldAccess</c>).
/// Legacy <c>PerformValidation</c> parity (Field/Step required, duplicate Step+Field check) is enforced in
/// <c>WorkflowGroupFieldAccessesAppService</c>, not here. The legacy dynamic "Field" dropdown (populated via
/// reflection over the resolved request type's writable properties) is dropped; <see cref="Field"/> is a
/// plain free-text property name here.
/// </summary>
public class WorkflowGroupFieldAccess : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WorkflowGroupDetailId { get; set; }

    public virtual WorkflowGroupDetail WorkflowGroupDetail { get; set; }

    public virtual int Step { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string Field { get; set; }

    public virtual bool IsVisible { get; set; }

    public virtual bool IsEditable { get; set; }

    public virtual bool IsRequired { get; set; }
}
