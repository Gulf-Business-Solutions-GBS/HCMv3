using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;

namespace HCMv3.TimeManagement;

/// <summary>
/// A batch of calculated timesheet days put forward for approval (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.Time.TimeLogRequest</c>).
///
/// <para>
/// <b>Why this type gets its own header/detail pair when nothing else in the wave does.</b> The single-row
/// rule -- the transaction row IS the request -- needs the request to address exactly one row, because
/// <c>WorkflowRequest.TargetEntityId</c> is one integer. What is being approved here is a rectangle:
/// N employees by M days of <see cref="TimeRegister"/>. <see cref="TimeRegister"/> therefore cannot itself be
/// <see cref="IWorkflowEnabledEntity"/>, and this header exists to be the one row the request points at.
/// </para>
///
/// <para>
/// <b>THE LEGACY DEFECT THIS SHAPE EXISTS TO FIX.</b> In legacy, the corrected working-day figures were
/// written straight onto <see cref="TimeRegister"/> at SUBMIT time
/// (<c>HCM10.ESS/Repository/TimeLogRepository.cs</c> lines 301-340) and approval merely flipped
/// <c>TimeRegister.Status</c> to Approved. An employee's corrected timesheet therefore went live -- visible to
/// payroll -- before anybody had approved anything, and rejecting the request did not take it back. Here the
/// proposed figures live on <see cref="TimeLogApprovalDetail"/> and NOTHING touches
/// <see cref="TimeRegister"/> until <c>TimeLogApprovalWorkflowTransactionHandler.OnApprovedAsync</c> runs.
/// </para>
///
/// <para>
/// <b>One header per employee, even when the grid spans a department.</b> <see cref="IHasEmployeeId"/> needs a
/// single non-null employee and so does the approver resolver, which routes by the SUBJECT employee's
/// workflow group -- a batch spanning two departments has no one correct approver. The admin service
/// therefore groups the selected days by employee and raises one header (one request) per employee, with
/// <see cref="OrganizationUnitId"/> recording the unit the grid was filtered by so the batch can still be
/// reported on as a whole.
/// </para>
/// </summary>
public class TimeLogApproval : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    /// <summary>The employee whose days this batch approves. Never null -- see the class remarks.</summary>
    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>
    /// The organisation unit the approval grid was filtered by, when it was. Informational: the days that are
    /// actually approved are the <see cref="Details"/>, never everybody currently in this unit.
    /// </summary>
    public virtual long? OrganizationUnitId { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    [StringLength(4000)]
    public virtual string Comments { get; set; }

    public int? WorkflowRequestId { get; set; }

    public RequestStatus? WorkflowStatus { get; set; }

    /// <summary>
    /// The days in this batch, each carrying its PROPOSED figures. Never empty -- the handler refuses an
    /// empty batch at submit, where legacy only emitted a message string and carried on.
    /// </summary>
    public virtual ICollection<TimeLogApprovalDetail> Details { get; set; }
}
