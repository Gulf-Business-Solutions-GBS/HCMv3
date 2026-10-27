using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee fund (e.g. savings/provident fund) header record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeFund</c>, Wave 2 deferred bucket).
/// Employee-scoped only -- no other header FK in the legacy source.
///
/// The legacy cascade that recomputes <see cref="Amount"/>/<see cref="PaidAmount"/>/
/// <see cref="BalanceAmount"/> from the child <see cref="EmployeeFundDetail"/> rows on save/delete of a
/// detail row is NOT ported (same cross-entity-cascade omission approach as Wave 1's
/// <c>EmployeeAbsAdjustment</c>) -- see <c>EmployeeFundDetailsAppService</c>.
/// </summary>
public class EmployeeFund : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual double Amount { get; set; }

    public virtual double PaidAmount { get; set; }

    public virtual double BalanceAmount { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
