using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee loan record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeLoan</c>, Wave 2 deferred bucket).
///
/// DELIBERATE SIMPLIFICATIONS / DROPPED FIELDS:
/// <list type="bullet">
/// <item><description><c>DocAttachment</c> FK dropped -- the file-BLOB attachment infrastructure was never
/// ported anywhere in this codebase (same simplification as Wave 1's <c>EmployeeAbsence</c>).</description></item>
/// <item><description>The legacy <c>prevXxx</c> change-tracking fields are <c>[NonPersistent]</c> and not
/// ported as entity columns.</description></item>
/// <item><description>The legacy computed getters for <see cref="EndDate"/> (auto-recalculated recovery end
/// date from FixedAmount/DueAmount/exemption count) and <see cref="BalanceDueAmount"/> (DueAmount minus sum
/// of recoveries) are ported as persisted columns but are NOT auto-recomputed by the AppService -- the
/// cross-entity cascade from <c>EmployeeLoanRecovery</c>/<c>EmployeeLoanExemption</c> saves is deliberately
/// not ported (same cross-entity-cascade omission approach as Wave 1's
/// <c>EmployeeAbsAdjustment</c>).</description></item>
/// </list>
/// </summary>
public class EmployeeLoan : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int LoanTypeId { get; set; }

    public virtual LoanType LoanType { get; set; }

    /// <summary>Legacy display name "Loan Payment Date".</summary>
    public virtual DateTime StartDate { get; set; }

    public virtual DateTime RecoveryStartDate { get; set; }

    /// <summary>Legacy display name "Recovery End Date".</summary>
    public virtual DateTime EndDate { get; set; }

    public virtual double ActualAmount { get; set; }

    public virtual double DueAmount { get; set; }

    public virtual double BalanceDueAmount { get; set; }

    public virtual double FixedAmount { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    /// <summary>Legacy display name "Recover Fractional Loan In Last Transaction".</summary>
    public virtual bool RecoverFracInLastTrans { get; set; }

    /// <summary>Legacy display name "Include Loan Payment in Payroll".</summary>
    public virtual bool IncludeInLoanPay { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
