using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow;

/// <summary>
/// Generic workflow request instance (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowRequest</c>).
///
/// ARCHITECTURE NOTE (deliberate scope decision): in the legacy XAF/XPO codebase, <c>WorkflowRequest</c> is
/// an abstract-ish base class that ~50 concrete feature-specific request types inherit from directly
/// (e.g. <c>LeaveRequest</c>, <c>EmployeeActionRequest</c>, <c>LoanRequest</c>, <c>TravelDeskRequest</c>,
/// ...), each adding its own feature fields, gaining the shared Employee/WorkflowStep/RequestStatus/
/// RequestNumber/Approver/RequestLog/WorkFlowApprover/WorkflowRemark/WFReadLog shape "for free" via table-
/// per-hierarchy inheritance. Porting all of those concrete subclasses (spread across Leave, Payroll, Time,
/// GovtRelations, and several other feature modules, most not yet requiring workflow integration) is out of
/// scope for this standalone Workflow-module port — see the task's own precedent: <c>PAEmployee</c> in the
/// already-ported PerformanceManagement module deliberately does NOT inherit from any workflow base. This
/// entity is therefore ported as its own standalone, non-abstract table (not as an EF Core TPH base class)
/// capturing the shared workflow-instance shape only. Wiring a concrete feature request (e.g. a future
/// <c>TimeManagement.LeaveRequest</c>) to actually participate in workflow — via TPH/TPT inheritance, a
/// shared FK, or otherwise — is a follow-up architectural decision for whichever team owns that module,
/// mirroring the PAEmployee precedent instead of retrofitting it here.
///
/// DELIBERATE SIMPLIFICATION: legacy <c>DocAttachment</c> (auto-created in <c>AfterConstruction</c>) is
/// dropped — same "blocked cross-module dependency" pattern used throughout this port. Legacy
/// <c>AllowedEmployeeList</c>/<c>DelegatedEmployee</c>/<c>DelegateEmployeeList</c>/<c>Approvers</c>/
/// <c>DelegateApprovers</c>/<c>CurrentWorkFlowRole</c>/<c>IsshownEmployee</c> are UI/view-model helpers
/// (XAF <c>[NonPersistent]</c> or in-memory-only in the legacy source), not entity state, and are not
/// ported. <c>RequestType</c> was a legacy computed property (reflection over <c>ClassInfo.ClassType.Name</c>)
/// used to discriminate the concrete subclass at runtime; ported here as a plain settable string for the
/// same purpose, since there is no concrete subclass hierarchy in this standalone port.
/// </summary>
public class WorkflowRequest : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int WorkflowStep { get; set; }

    public virtual int? ApproverId { get; set; }

    public virtual Employee Approver { get; set; }

    [StringLength(256)]
    public virtual string RequestType { get; set; }

    /// <summary>
    /// Primary key of the transaction row this request governs -- e.g. the <c>EmployeeAbsence.Id</c> for a
    /// leave request. Together with <see cref="RequestType"/> this is a deliberate polymorphic association:
    /// the workflow engine stays generic and knows nothing about leave, assets or loans, while each
    /// transaction table needs only the two <c>IWorkflowEnabledEntity</c> columns pointing back.
    ///
    /// <para>
    /// Not a foreign key, and it cannot be one -- it addresses rows in a different table per
    /// <see cref="RequestType"/>. Referential integrity is the responsibility of the type's
    /// <c>IWorkflowTransactionHandler</c>. Nullable so that a request can exist before its target row does,
    /// and so that already-ported rows migrate cleanly.
    /// </para>
    /// </summary>
    public virtual int? TargetEntityId { get; set; }

    /// <summary>
    /// The transaction's own sub-type, when its approval route depends on one -- <c>AbsenceTypeId</c> for
    /// leave, so that annual leave can require three approvals while sick leave requires one. Matched
    /// against <c>WorkflowGroupDetail.RequestSubTypeId</c> when resolving which <see cref="RequestMaster"/>
    /// applies.
    /// </summary>
    public virtual int? RequestSubTypeId { get; set; }

    /// <summary>
    /// Human-readable one-line description of what is being requested ("Annual Leave, 5 days, 01-05 Jan
    /// 2026"), written once by the type's handler at submit time.
    ///
    /// <para>
    /// Denormalised on purpose. The approver inbox lists requests of every type at once; without this it
    /// would have to fan out to a different table per row just to render a label -- a join across ~50 tables
    /// to draw one list. With it the inbox is a single query over <c>WorkflowRequests</c> joined to
    /// <c>WorkFlowApprovers</c>, and only the detail view touches the transaction table.
    /// </para>
    /// </summary>
    [StringLength(512)]
    public virtual string SummaryEn { get; set; }

    /// <inheritdoc cref="SummaryEn"/>
    [StringLength(512)]
    public virtual string SummaryAr { get; set; }

    [StringLength(64)]
    public virtual string RequestNumber { get; set; }

    public virtual RequestStatus RequestStatus { get; set; }

    public virtual int ApprovalProcessTime { get; set; }

    public virtual ICollection<RequestLog> RequestLogs { get; set; }

    public virtual ICollection<WorkFlowApprover> WorkFlowApprovers { get; set; }

    public virtual ICollection<WorkflowRemark> WorkflowRemarks { get; set; }

    public virtual ICollection<WFReadLog> WFReadLogs { get; set; }
}
