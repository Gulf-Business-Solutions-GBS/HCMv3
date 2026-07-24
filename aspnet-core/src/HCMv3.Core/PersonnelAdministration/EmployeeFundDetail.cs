using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee fund transaction (deduction/payment/opening) row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeFundDetail</c>, Wave 2 deferred bucket).
/// The legacy cascade that recomputes the parent <see cref="EmployeeFund"/> totals on save/delete is NOT
/// ported (same cross-entity-cascade omission approach as Wave 1's <c>EmployeeAbsAdjustment</c>) -- see
/// <c>EmployeeFundDetailsAppService</c>.
/// </summary>
public class EmployeeFundDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeFundId { get; set; }

    public virtual EmployeeFund EmployeeFund { get; set; }

    public virtual DateTime TransactionDate { get; set; }

    public virtual double Amount { get; set; }

    public virtual FundPayType FundPayType { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
