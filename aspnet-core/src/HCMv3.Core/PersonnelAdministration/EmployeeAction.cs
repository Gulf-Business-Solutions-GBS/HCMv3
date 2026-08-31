using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee personal-action record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAction</c>, Wave B).
///
/// The legacy <c>EndDate</c> property is not an independently-stored value -- its getter always just
/// returns <c>StartDate</c> -- so no EndDate column is ported here.
///
/// DEFERRED (not ported): the <c>PayrollArea</c>/<c>PayrollControlRecord.CheckIfPayrollAreaLocked</c>
/// check (Payroll module not ported); <c>CheckPersonalActionData</c>/<c>CheckPersonalActionDataDuplicate</c>
/// (defined in the legacy source but never actually called from <c>OnSaving</c> -- dead/unreachable code).
/// The legacy cascade-delete of dependent <c>EmployeeBasicPay</c>/<c>EmployeeOnHold</c>/
/// <c>EmployeeSeparation</c>/<c>EmployeeFnFPayment</c> records is skipped (those entities aren't ported
/// this wave); only the <see cref="EmployeeOrgAssignment"/>/<see cref="EmployeeContract"/> cascade
/// branches are ported, in <c>EmployeeActionsAppService.Delete</c>.
/// </summary>
public class EmployeeAction : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int PersonalActionId { get; set; }

    public virtual PersonalAction PersonalAction { get; set; }

    public virtual int ActionReasonId { get; set; }

    public virtual PersonalActionReason ActionReason { get; set; }

    public virtual DateTime StartDate { get; set; }

    [StringLength(256)]
    public virtual string RecommendedBy { get; set; }

    [StringLength(1000)]
    public virtual string Comments { get; set; }

    /// <summary>The approval request that governs this action, when the type is workflow-enabled.</summary>
    public virtual int? WorkflowRequestId { get; set; }

    /// <summary>
    /// Null when the action was entered directly and the type is not routed. Read by the ApprovedOnly filter,
    /// which is what keeps an unapproved promotion or transfer out of org assignments, contracts and payroll
    /// until somebody has agreed to it.
    /// </summary>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
