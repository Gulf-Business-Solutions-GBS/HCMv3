using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Monthly air-ticket accrual snapshot per employee (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.TicketAccrualSummary</c>). No business validation (same
/// swallowed-empty-catch anti-pattern as <see cref="GratuityAccrualSummary"/> — not replicated, see its
/// class remarks).
///
/// BUG FIX: the legacy <c>TicketAccrued</c>/<c>TicketUsed</c> are typed <c>int</c> — same precision-loss
/// pattern flagged above. Ported as <see cref="double"/> in this port.
/// </summary>
public class TicketAccrualSummary : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int Year { get; set; }

    public virtual int Month { get; set; }

    public virtual double TicketAccrued { get; set; }

    public virtual double TicketUsed { get; set; }
}
