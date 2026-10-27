using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Monthly gratuity accrual snapshot per employee (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.GratuityAccrualSummary</c>). No business validation is
/// ported: the legacy <c>OnSaving</c> wraps everything in a try/catch with an EMPTY catch block (silently
/// swallowing ALL exceptions, including the creator/modifier stamping). That anti-pattern is NOT
/// replicated here — there is nothing else to validate in the legacy source beyond the swallowed stamping,
/// so this entity is ported as plain CRUD with normal exception propagation like every other entity in this
/// port.
///
/// BUG FIX: the legacy money/day-count fields (<c>GratuityDaysAccrued</c>/<c>GratuityDaysBalance</c>/
/// <c>GratuityAmount</c>/<c>GratuityAmountPaid</c>) are typed <c>int</c> in the legacy source — a
/// precision-loss bug for fractional-day/money values. Ported as <see cref="double"/> in this port.
/// </summary>
public class GratuityAccrualSummary : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int Year { get; set; }

    public virtual int Month { get; set; }

    public virtual double GratuityDaysAccrued { get; set; }

    public virtual double GratuityDaysBalance { get; set; }

    public virtual double GratuityAmount { get; set; }

    public virtual double GratuityAmountPaid { get; set; }
}
