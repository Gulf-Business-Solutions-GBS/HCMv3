using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Records that a given <see cref="PayrollManagement.AbsenceType"/> is included for a
/// <see cref="PayrollManagement.WageType"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.WageTypeLeave</c>), listed separately and linked back
/// via <see cref="WageTypeId"/>.
/// </summary>
public class WageTypeLeave : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual int AbsenceTypeId { get; set; }

    public virtual AbsenceType AbsenceType { get; set; }
}
