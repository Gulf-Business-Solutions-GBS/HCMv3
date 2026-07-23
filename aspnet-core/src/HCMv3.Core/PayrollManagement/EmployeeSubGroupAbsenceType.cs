using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Records that a given <see cref="PayrollManagement.AbsenceType"/> applies to an <see cref="EmployeeSubGroup"/>
/// (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeSubGroupAbsenceType</c>), listed separately and
/// linked back via <see cref="EmployeeSubGroupId"/>.
/// </summary>
public class EmployeeSubGroupAbsenceType : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    public virtual int AbsenceTypeId { get; set; }

    public virtual AbsenceType AbsenceType { get; set; }
}
