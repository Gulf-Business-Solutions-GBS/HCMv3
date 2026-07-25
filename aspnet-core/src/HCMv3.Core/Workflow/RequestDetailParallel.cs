using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.Workflow;

/// <summary>
/// Additional parallel approver definition (by job title + workflow role) for a <see cref="RequestDetail"/>
/// step (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestDetailParallel</c>). Legacy
/// <c>PerformValidation</c> parity (WorkflowRole required) is enforced in
/// <c>RequestDetailParallelsAppService</c>, not here.
/// </summary>
public class RequestDetailParallel : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int RequestDetailId { get; set; }

    public virtual RequestDetail RequestDetail { get; set; }

    public virtual int? JobTitleId { get; set; }

    public virtual JobTitle JobTitle { get; set; }

    public virtual int? WorkflowRoleId { get; set; }

    public virtual WorkflowRole WorkflowRole { get; set; }
}
