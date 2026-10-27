using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow;

/// <summary>
/// Immutable audit-trail row for a <see cref="WorkflowRequest"/> action (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestLog</c>). Legacy UI made rows
/// non-editable/non-deletable after creation (<c>Appearance("EditDisabled", ...)</c>); enforced in
/// <c>RequestLogsAppService</c> by exposing create only (no update), not here.
/// </summary>
public class RequestLog : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WorkflowRequestId { get; set; }

    public virtual WorkflowRequest WorkflowRequest { get; set; }

    public virtual int? ActionById { get; set; }

    public virtual Employee ActionBy { get; set; }

    [StringLength(4000)]
    public virtual string RequestActionComment { get; set; }

    public virtual DateTime ActionDateTime { get; set; }

    public virtual RequestActionType ActionType { get; set; }

    public virtual int WorkflowStep { get; set; }

    public virtual int ActionDays { get; set; }

    public virtual WorkflowPlatform Platform { get; set; }
}
