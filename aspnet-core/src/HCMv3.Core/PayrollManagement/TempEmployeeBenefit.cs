using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Simple Employee -> TempBenefitType join (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.TempEmployeeBenefit</c>). Exactly one row is allowed
/// per <see cref="Employee"/>, regardless of <see cref="TempBenefitType"/> (enforced in
/// <c>TempEmployeeBenefitsAppService</c>, legacy "Employee is already assigned to this Grade" parity --
/// the legacy check is by EmployeeId only, not by the (Employee, TempBenefitType) combination).
/// </summary>
public class TempEmployeeBenefit : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int TempBenefitTypeId { get; set; }

    public virtual TempBenefitType TempBenefitType { get; set; }
}
