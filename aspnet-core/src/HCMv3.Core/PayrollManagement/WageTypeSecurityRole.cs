using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.Authorization.Roles;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Role-based access-restriction list consumed when <see cref="WageType.IsSecured"/> is true (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.PayrollManagement.WageTypeSecurityRole</c>), listed separately
/// and linked back via <see cref="WageTypeId"/>. FKs ASP.NET Zero's own built-in <see cref="Role"/> entity
/// rather than a duplicate permission-policy concept. No filtering logic is implemented yet in this wave —
/// this is only the join table.
/// </summary>
public class WageTypeSecurityRole : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual int RoleId { get; set; }

    public virtual Role Role { get; set; }
}
