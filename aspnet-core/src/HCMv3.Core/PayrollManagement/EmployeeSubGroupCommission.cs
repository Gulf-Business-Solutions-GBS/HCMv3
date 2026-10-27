using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Commission wage type entry for an <see cref="EmployeeSubGroup"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeSubGroupCommission</c>), listed separately and
/// linked back via <see cref="EmployeeSubGroupId"/>. <see cref="WageTypeId"/> is a nullable FK to the
/// not-yet-ported <c>WageType</c> lookup (a later wave).
/// </summary>
public class EmployeeSubGroupCommission : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    public virtual int? WageTypeId { get; set; }
}
