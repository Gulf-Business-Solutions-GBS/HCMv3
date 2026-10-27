using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee loan recovery (installment payment) transaction record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeLoanRecovery</c>, Wave 2 deferred
/// bucket). Ports the essential active validations only (recovery amount not exceeding balance due, recovery
/// date not earlier than recovery start date). The legacy cascade that recomputes the parent
/// <see cref="EmployeeLoan.BalanceDueAmount"/> on save/delete is NOT ported (same cross-entity-cascade
/// omission approach as Wave 1's <c>EmployeeAbsAdjustment</c>) -- see
/// <c>EmployeeLoanRecoveriesAppService</c>.
/// </summary>
public class EmployeeLoanRecovery : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeLoanId { get; set; }

    public virtual EmployeeLoan EmployeeLoan { get; set; }

    public virtual DateTime RecoveryDate { get; set; }

    public virtual double RecoveryAmount { get; set; }

    public virtual LoanRecoveryMethod LoanRecoveryMethod { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
