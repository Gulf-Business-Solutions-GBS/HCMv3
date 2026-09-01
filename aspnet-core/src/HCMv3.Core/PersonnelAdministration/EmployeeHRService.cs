using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// An HR service an employee has asked for -- a salary certificate, a letter, an ID replacement (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.Workflow.General.HRServiceRequest</c>).
///
/// <para>
/// Legacy modelled this as a subclass of <c>WorkflowRequest</c>, so the request and the record it produced
/// were the same object by inheritance. HCMv3 keeps the one-row idea but inverts the relationship: this is
/// an ordinary transaction row that a <see cref="WorkflowRequest"/> points at, which is what lets the same
/// row be raised from self-service, raised by HR, or recorded with workflow switched off entirely.
/// </para>
///
/// <para>
/// This one IS self-service: the employee asks for their own certificate, so it carries an ESS route and a
/// <c>MyHRServiceAppService</c>. Its sibling <see cref="EmployeeITService"/> deliberately does not.
/// </para>
///
/// <para>
/// NOT ported: legacy's <c>RequestComments</c>/<c>WorkflowComments</c> (both non-persistent screen fields --
/// approver comments live on the workflow request itself), its hand-rolled <c>RequestNumber</c> assignment
/// (the engine numbers requests now), and its <c>SysCreator</c>/<c>SysCreatedOn</c> stamping, which
/// <c>FullAuditedEntity</c> already does. The legacy "RequestStatus == Approved" block was empty, so there
/// is no approval side effect to port and the handler does not override <c>OnApprovedAsync</c>.
/// </para>
/// </summary>
public class EmployeeHRService : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int HRServiceTypeId { get; set; }

    public virtual HRServiceType HRServiceType { get; set; }

    public virtual DateTime ServiceDate { get; set; }

    /// <summary>Optional. Some services carry a fee or a reimbursement; most carry nothing, so zero is a
    /// legitimate value rather than a missing one.</summary>
    public virtual double Amount { get; set; }

    [StringLength(1000)]
    public virtual string Comments { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
