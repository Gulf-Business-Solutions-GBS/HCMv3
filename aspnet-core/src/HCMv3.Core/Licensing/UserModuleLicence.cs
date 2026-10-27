using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Licensing;

/// <summary>
/// One licensed module assigned to one user. Ported from legacy <c>ApplicationUserLicense</c>.
///
/// <para>
/// Legacy kept a row per user and module with an <c>IsAssigned</c> flag; here the row's existence is the assignment
/// and unassigning soft-deletes it, so the audit columns say who gave and who took away each seat. A user holds a
/// module at most once (filtered unique index on TenantId, UserId, Module where not deleted).
/// </para>
///
/// <para>
/// <see cref="UserId"/> is not a database foreign key, the same choice as <c>Employee.UserId</c>: users are
/// soft-deleted, and a deleted or deactivated user's seat simply stops being counted.
/// </para>
/// </summary>
public class UserModuleLicence : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public long UserId { get; set; }

    public LicenceModule Module { get; set; }
}
