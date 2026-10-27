using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Overtime put forward for a whole department and date range as ONE document for approval (ported from legacy
/// XAF <c>HCM10.Module.BusinessObjects.Workflow.Payroll.OvertimeRequest</c> and its
/// <c>OvertimeRequestDetail</c>, raised from <c>HCM10.ESS</c> <c>OverTimeController.OvertimeRequestSubmit</c>).
///
/// <para>
/// <b>Why this is a header entity and not a second overtime table.</b> The one-entity-per-transaction rule
/// forbids a <c>*Request</c> mirror of a transaction. This is not one: a batch is a HEADER OVER MANY EMPLOYEES
/// AND DAYS, and one approval produces N <see cref="EmployeeOvertime"/> rows, one per line. There is no single
/// pre-existing overtime row for <c>WorkflowRequest.TargetEntityId</c> to point at, so this row exists to be
/// that one integer. Same argument, and the same shape, as <see cref="EmployeePaymentBatch"/> over
/// <see cref="EmployeePayment"/>.
/// </para>
///
/// <para>
/// <b>What approval buys.</b> Nothing exists in <see cref="EmployeeOvertime"/> until the chain completes.
/// Legacy created the overtime rows in <c>OvertimeRequest.OnSaving</c> inside
/// <c>if (RequestStatus == Approved)</c>, i.e. on every save of an approved request; here they are created once,
/// by <c>Services.IOvertimeBatchService.PostOvertimesAsync</c>, which records on each line the row it made.
/// </para>
///
/// <para>
/// No <c>IHasEmployeeId</c>, deliberately: the batch is about a department. It is routed against the person
/// who raised it, exactly as legacy routed <c>OvertimeRequest.Employee</c> (the logged-in employee).
/// </para>
/// </summary>
public class OvertimeBatch : FullAuditedEntity, IMustHaveTenant, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    /// <summary>Legacy <c>OrganizationUnit</c> (display name "Department"). The unit whose people were loaded.</summary>
    public virtual long OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    /// <summary>
    /// Legacy <c>RequestComments</c>, Size(4000). Legacy declared it <c>[NonPersistent]</c> and so lost it once
    /// the form closed; it is a real column here, as on <see cref="EmployeePaymentBatch.Comments"/>.
    /// </summary>
    [StringLength(4000)]
    public virtual string Comments { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }

    /// <summary>One line per employee per day. Never empty, never the same employee and day twice.</summary>
    public virtual ICollection<OvertimeBatchDetail> Details { get; set; }
}
