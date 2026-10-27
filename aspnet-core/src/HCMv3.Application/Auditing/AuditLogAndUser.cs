using Abp.Auditing;
using HCMv3.Authorization.Users;

namespace HCMv3.Auditing;

/// <summary>
/// A helper class to store an <see cref="AuditLog"/> and a <see cref="User"/> object.
/// </summary>
public class AuditLogAndUser
{
    public AuditLog AuditLog { get; set; }

    public User User { get; set; }
}
