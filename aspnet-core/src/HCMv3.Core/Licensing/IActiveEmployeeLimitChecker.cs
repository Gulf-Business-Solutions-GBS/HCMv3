using System.Threading.Tasks;

namespace HCMv3.Licensing;

/// <summary>
/// Legacy licence line "Active Employees", on the <see cref="HcmFeatures.MaxActiveEmployees"/> edition feature. A hard
/// block, as legacy and as decided for this port: an employee who would take the tenant past the limit is not made
/// active. Employees who are already active are never affected, even when the limit is later lowered below them.
/// </summary>
public interface IActiveEmployeeLimitChecker
{
    /// <summary>
    /// Throws a user-friendly error when making one more employee active would exceed the limit.
    /// <paramref name="employeeId"/> is the employee being activated (0 for a new one) and is not counted twice.
    /// </summary>
    Task EnsureCanActivateAsync(int tenantId, int employeeId);

    /// <inheritdoc cref="EnsureCanActivateAsync"/>
    void EnsureCanActivate(int tenantId, int employeeId);
}
