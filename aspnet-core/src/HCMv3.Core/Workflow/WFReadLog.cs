using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Workflow;

/// <summary>
/// Records who viewed a <see cref="WorkflowRequest"/> and when (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.General.WFReadLog</c> -- note the legacy file lives under the
/// <c>General</c> folder despite belonging conceptually to the core workflow engine). Only recorded per
/// step when that step's <c>RequestDetail.EnableReadLog</c> is set (legacy behavior enforced by the
/// consuming feature, not by this entity).
/// </summary>
public class WFReadLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WorkflowRequestId { get; set; }

    public virtual WorkflowRequest WorkflowRequest { get; set; }

    public virtual DateTime ReadDateTime { get; set; }

    [StringLength(256)]
    public virtual string User { get; set; }
}
