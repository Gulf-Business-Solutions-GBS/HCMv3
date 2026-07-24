using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee loan recovery-exemption (skip a month's recovery) record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeLoanExemption</c>, Wave 2 deferred
/// bucket). The legacy cascade that recomputes the parent <see cref="EmployeeLoan.EndDate"/> on save/delete
/// is NOT ported (same cross-entity-cascade omission approach as Wave 1's <c>EmployeeAbsAdjustment</c>) --
/// see <c>EmployeeLoanExemptionsAppService</c>.
/// </summary>
public class EmployeeLoanExemption : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeLoanId { get; set; }

    public virtual EmployeeLoan EmployeeLoan { get; set; }

    public virtual DateTime ExemptionDate { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
