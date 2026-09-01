using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// An IT service raised about an employee -- a laptop build, an account, a software install (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.Workflow.Services.ITServiceRequest</c>).
///
/// <para>
/// NOT self-service, and that is a finding about the legacy system rather than a decision made here: there
/// is no ESS view, controller or model for an IT service anywhere in <c>HCM10.ESS</c>, only a back-office
/// screen. So it follows the <see cref="EmployeeAction"/> precedent -- catalogued workflow-capable with no
/// ESS route and no ESS permission, submitted from its admin application service through
/// <c>IAdminTransactionSubmitter</c>. It can be routed for approval without also becoming something anybody
/// can raise about themselves.
/// </para>
///
/// <para>
/// NOT ported, deliberately: legacy's <c>OnSaving</c> wrapped its entire body -- validation included -- in a
/// <c>catch (Exception) { }</c> that swallowed every failure and then called <c>base.OnSaving()</c> anyway,
/// so a request with no employee and no service type saved silently. That is a bug, not behaviour. The
/// required-field checks it meant to run live in <c>ITServiceWorkflowTransactionHandler</c> and on the DTO,
/// where they actually stop a save. Its "RequestStatus == Approved" block was empty, so there is no approval
/// side effect to port.
/// </para>
/// </summary>
public class EmployeeITService : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int ITServiceTypeId { get; set; }

    public virtual ITServiceType ITServiceType { get; set; }

    /// <summary>Legacy called this <c>Date</c>; named for what it is, and matched to its HR sibling.</summary>
    public virtual DateTime ServiceDate { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
