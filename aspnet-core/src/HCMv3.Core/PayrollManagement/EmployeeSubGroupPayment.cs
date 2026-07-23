using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Fixed pay-component wage type entry for an <see cref="EmployeeSubGroup"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeSubGroupPayment</c>), listed separately and
/// linked back via <see cref="EmployeeSubGroupId"/>. <see cref="WageTypeId"/> is a nullable FK to the
/// not-yet-ported <c>WageType</c> lookup (a later wave). DELIBERATE SIMPLIFICATION: the legacy cross-field
/// rule validating <c>WageType.IncludeInCalculatedPay</c>/<c>IncludeInBasicPay</c> against
/// <see cref="PayInterval"/> is not ported -- WageType does not exist yet this wave.
/// </summary>
public class EmployeeSubGroupPayment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    public virtual int? WageTypeId { get; set; }

    public virtual double Amount { get; set; }

    public virtual PayInterval PayInterval { get; set; }

    /// <summary>
    /// Reuses <see cref="CustomStartMonth"/> (None + 12 months) -- the legacy <c>PayMonth</c> enum has the
    /// identical value set, so no separate enum is declared.
    /// </summary>
    public virtual CustomStartMonth PayMonth { get; set; }
}
