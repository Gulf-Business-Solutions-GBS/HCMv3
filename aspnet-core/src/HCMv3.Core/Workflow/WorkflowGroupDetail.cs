using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Workflow;

/// <summary>
/// Child of <see cref="WorkflowGroup"/>: assigns a <see cref="RequestMaster"/> to a given request type
/// (+ optional sub-type/custom-sub-type) for that group (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkflowGroupDetail</c>).
///
/// DELIBERATE SIMPLIFICATION: the legacy entity resolves <c>RequestType</c>/<c>RequestSubType</c>/
/// <c>CustomSubType</c> dynamically via DevExpress XPO type metadata (<c>XPObjectType</c>) against dozens
/// of concrete <c>WorkflowRequest</c> subclasses spread across many feature modules (Leave, Document,
/// Disciplinary, HRService, Loan, Attendance, ...), most of which are themselves out of scope for this
/// standalone Workflow-module port (see <see cref="WorkflowRequest"/> remarks). That XPO-metadata
/// mechanism has no EF Core/relational equivalent, so it is replaced here with a plain
/// <see cref="RequestTypeName"/> string (the legacy concrete type's short name, e.g. "LeaveRequest") plus
/// generic nullable int sub-type ids. Consuming feature modules are expected to interpret
/// <see cref="RequestTypeName"/>/<see cref="RequestSubTypeId"/>/<see cref="CustomSubTypeId"/> themselves
/// once (if ever) they adopt workflow integration.
/// </summary>
public class WorkflowGroupDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WorkflowGroupId { get; set; }

    public virtual WorkflowGroup WorkflowGroup { get; set; }

    public virtual int RequestMasterId { get; set; }

    public virtual RequestMaster RequestMaster { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string RequestTypeName { get; set; }

    public virtual int? RequestSubTypeId { get; set; }

    public virtual int? CustomSubTypeId { get; set; }

    public virtual ICollection<WorkflowGroupFieldAccess> WorkflowGroupFieldAccesses { get; set; }
}
