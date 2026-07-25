using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow;

/// <summary>
/// CC-receiver remark on a <see cref="WorkflowRequest"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.WorkflowRemark</c>). Legacy duplicate-CC-receiver
/// check is enforced in <c>WorkflowRemarksAppService</c>, not here.
///
/// DELIBERATE SIMPLIFICATION: legacy notification-sending on save (<c>CreateNotification</c>, via the
/// unported <c>NotificationConfig</c>/<c>NotificationService</c>/<c>EmailService</c>) is dropped — same
/// "blocked cross-module dependency" pattern used throughout this port.
/// </summary>
public class WorkflowRemark : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WorkflowRequestId { get; set; }

    public virtual WorkflowRequest WorkflowRequest { get; set; }

    public virtual int? CCReceiverId { get; set; }

    public virtual Employee CCReceiver { get; set; }
}
