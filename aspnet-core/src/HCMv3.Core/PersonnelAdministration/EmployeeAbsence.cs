using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee leave/absence transaction record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeAbsence</c>, Wave 1 deferred bucket).
///
/// DELIBERATE SIMPLIFICATIONS / DROPPED FIELDS:
/// <list type="bullet">
/// <item><description><c>WfRequest</c> FK dropped -- the Workflow module was never ported.</description></item>
/// <item><description><c>DocAttachment</c> FK dropped -- the file-BLOB attachment infrastructure was never
/// ported anywhere in this codebase.</description></item>
/// <item><description><c>EmpAbsenceType</c> (a derived/computed <c>XPCollection</c> used only to filter the
/// AbsenceType picklist in the legacy XAF UI by gender/religion/salary-profile) is not a real persisted
/// column and is not ported; that filtering, if needed, is an Angular-side concern.</description></item>
/// <item><description>All the "Fractional Leave" (Frac1/Frac2), old "Paid Leave"/"Unpaid Leave" helper
/// fields, and the <c>prevXxx</c> change-tracking fields are <c>[NonPersistent]</c> in the legacy source
/// (used only to drive in-memory XAF UI calculations / quota cascade side effects) and are not ported as
/// entity columns.</description></item>
/// <item><description>The "extension leave" (leave-type-conversion) sub-record fields
/// (<see cref="ExtAbsenceTypeId"/>, <see cref="ExtStartDate"/>, <see cref="ExtEndDate"/>,
/// <see cref="ExtTotalLeaveDays"/>) ARE ported as real nullable columns per Wave 1 scope, even though they
/// are <c>[NonPersistent]</c> in the legacy source.</description></item>
/// </list>
/// </summary>
public class EmployeeAbsence : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int AbsenceTypeId { get; set; }

    public virtual AbsenceType AbsenceType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual HalfdayType HalfdayType { get; set; }

    public virtual DateTime StartTime { get; set; }

    public virtual DateTime EndTime { get; set; }

    public virtual double AbsenceHours { get; set; }

    public virtual bool IsTravelling { get; set; }

    public virtual double CalendarDays { get; set; }

    /// <summary>Total number of leave days deducted from quota for this record.</summary>
    public virtual double TotalLeaveDays { get; set; }

    public virtual LeavePaymentType LeavePaymentType { get; set; }

    public virtual DateTime PaymentDate { get; set; }

    public virtual bool AdvanceLeavePaid { get; set; }

    [StringLength(4000)]
    public virtual string Comments { get; set; }

    public virtual DateTime ReturnDate { get; set; }

    public virtual bool IsReturned { get; set; }

    /// <summary>
    /// Original EndDate before a leave-extension edit, used to compute the "extra days" gap between the
    /// initial EndDate and the actual ReturnDate.
    /// </summary>
    public virtual DateTime InitialEndDate { get; set; }

    /// <summary>
    /// Optional FK to the AbsenceType this record's extension sub-period is converted into (leave-type
    /// conversion, e.g. converting excess Annual leave to Unpaid leave).
    /// </summary>
    public virtual int? ExtAbsenceTypeId { get; set; }

    public virtual AbsenceType ExtAbsenceType { get; set; }

    public virtual DateTime? ExtStartDate { get; set; }

    public virtual DateTime? ExtEndDate { get; set; }

    public virtual double? ExtTotalLeaveDays { get; set; }

    /// <summary>
    /// Legacy <c>AbsenceRequest.DelegatedTo</c> ("Primary Delegation"): the colleague covering the employee's
    /// WORK while they are away. Informational -- they are told on submit, and nothing is rerouted to them.
    /// A plain id with no navigation, like the two below: three more paths into <c>Employee</c> from one table
    /// would each need a cascade exception and nothing here ever loads the row through them.
    /// </summary>
    public virtual int? PrimaryDelegateId { get; set; }

    /// <summary>Legacy <c>AbsenceRequest.SecondaryDelegation</c>: the fallback for the primary delegate.</summary>
    public virtual int? SecondaryDelegateId { get; set; }

    /// <summary>
    /// Legacy <c>AbsenceRequest.ApproverDelegation</c>: who decides the employee's APPROVALS while they are
    /// away. Unlike legacy, which only stored the name, approving the leave creates an
    /// <see cref="EmployeeDelegation"/> for the leave dates, so the delegate really does receive them.
    /// </summary>
    public virtual int? ApproverDelegateId { get; set; }

    /// <summary>
    /// When the employee accepted the leave policy for this request, for a type that requires it
    /// (<c>AbsenceType.RequirePolicyAcceptance</c>). Legacy checked the box in the browser and stored nothing.
    /// </summary>
    public virtual DateTime? PolicyAcceptedOn { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
