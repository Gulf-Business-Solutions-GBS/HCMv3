using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Monthly leave accrual snapshot per employee/leave-type (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.LeaveAccrualSummary</c>). No business validation (same
/// swallowed-empty-catch anti-pattern as <see cref="GratuityAccrualSummary"/> — not replicated, see its
/// class remarks).
///
/// BUG FIX: the legacy <c>UsedDays</c>/<c>CumulativeAccruedDays</c> are typed <c>int</c> while the sibling
/// <c>AccrualDays</c> is already a <c>double</c> — an inconsistent precision-loss bug. Ported as
/// <see cref="double"/> for all three fields in this port, matching <see cref="AccrualDays"/>.
/// </summary>
public class LeaveAccrualSummary : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int AbsenceTypeId { get; set; }

    public virtual AbsenceType AbsenceType { get; set; }

    public virtual int Year { get; set; }

    public virtual int Month { get; set; }

    public virtual double AccrualDays { get; set; }

    public virtual double UsedDays { get; set; }

    public virtual double CumulativeAccruedDays { get; set; }
}
