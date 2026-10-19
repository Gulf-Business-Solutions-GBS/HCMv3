using System.Collections.Generic;

namespace HCMv3.HcmNotifications;

/// <summary>
/// The codes a Notification Master rule stores in <c>NotificationType</c>: the whitelist of record kinds a
/// rule may be written about. Legacy offered "any persistent class"; this is the fixed list that replaced it.
///
/// <para>
/// Declared here, beside the notification names, rather than read off the source classes, because the Zero
/// notification provider must name every definition at startup and runs before any source is resolved. A test
/// checks that every registered source's code is in this list and every code has a source, so the two cannot
/// drift apart.
/// </para>
///
/// <para><b>Never rename a value</b>: stored rules and users' subscriptions both refer to it.</para>
/// </summary>
public static class NotificationRuleSourceCodes
{
    public const string EmployeeDocument = "EmployeeDocument";
    public const string CompanyDocument = "CompanyDocument";
    public const string Leave = "Leave";
    public const string Disciplinary = "Disciplinary";
    public const string Employee = "Employee";
    public const string Loan = "Loan";

    public static readonly IReadOnlyList<string> All = new[]
    {
        EmployeeDocument,
        CompanyDocument,
        Leave,
        Disciplinary,
        Employee,
        Loan
    };
}
