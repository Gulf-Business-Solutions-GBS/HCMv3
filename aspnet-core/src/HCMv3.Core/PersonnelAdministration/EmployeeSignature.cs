using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee's scanned signature image (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeSignature</c>).
/// </summary>
public class EmployeeSignature : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual byte[] Signature { get; set; }
}
