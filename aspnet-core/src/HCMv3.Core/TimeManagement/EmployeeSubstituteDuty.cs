using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;

namespace HCMv3.TimeManagement;

/// <summary>
/// Per-date Duty override for an Employee (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.EmployeeSubstituteDuty</c>, Wave 3).
///
/// DELIBERATE, SIGNIFICANT SIMPLIFICATION: the legacy <c>OnSaving</c>/<c>OnDeleting</c> contain a large
/// <c>RecalculateTotalLeaveDays()</c>/<c>CheckQuotaBalance()</c> cascade into <c>EmployeeAbsence</c>/
/// <c>EmployeeAbsenceQuota</c>/<c>EmployeeSubGroupAbsence</c>/<c>TimeTransfer</c> sick-leave-opening-balance
/// logic. None of those leave-quota entities exist in this port yet (<c>EmployeeAbsence</c>/
/// <c>EmployeeAbsenceQuota</c> were never ported -- deferred as TimeManagement-adjacent when
/// PersonnelAdministration was scoped). Only the entity shape and a basic duplicate
/// (Employee, SubstituteDate) validation are ported here (in
/// <c>EmployeeSubstituteDutiesAppService</c>) -- none of the quota-recalculation cascade is ported. This
/// is the same category of deferral as the <c>HolidayPeriod</c> cascade from Wave 1.
///
/// The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
///
/// <para>
/// <b>Workflow.</b> This row IS the shift-override transaction -- one employee, one date, one Duty -- and it
/// is what legacy routed as a <c>RosterPlannerRequest</c> header/detail pair. There is no such pair here:
/// the override row carries <see cref="IWorkflowEnabledEntity"/> and routes in place, so a planned shift
/// change cannot drift away from the request that asked for it. Until an approver agrees, the row's
/// <see cref="WorkflowStatus"/> keeps it behind the <c>ApprovedOnly</c> filter and therefore out of
/// <c>RosterService</c>, the Time Engine and every attendance report -- a pending override cannot change
/// anybody's calculated roster or TimeRegister day. See <c>SubstituteDutyWorkflowTransactionHandler</c>.
/// </para>
///
/// <para>
/// Legacy's batch grid (one request header, many employee/date lines) is deliberately NOT ported in this
/// pass: <c>WorkflowRequest.TargetEntityId</c> addresses one row, so one override is one request.
/// </para>
///
/// <para>
/// <b>Removing an override is an AMENDMENT, not a delete.</b> A manager who wants to take a planned shift
/// away amends the override to an off-duty / rest <see cref="Duty"/>, and that change goes through approval
/// like any other change. Two reasons, and neither is squeamishness about DELETE:
/// <list type="number">
/// <item><description>The <c>ApprovedOnly</c> filter expresses "not yet effective", not "pending removal".
/// Marking a live row for deletion would need a state in the shared engine -- something like PendingRemoval,
/// understood by the filter, by the inbox and by every handler -- that only this one transaction type would
/// ever use.</description></item>
/// <item><description>An amendment PRESERVES the record that somebody decided to take the shift away, and
/// who approved it. A delete erases exactly that.</description></item>
/// </list>
/// </para>
/// </summary>
public class EmployeeSubstituteDuty : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int DutyId { get; set; }

    public virtual Duty Duty { get; set; }

    public virtual DateTime SubstituteDate { get; set; }

    /// <inheritdoc/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
