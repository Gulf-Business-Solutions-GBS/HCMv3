using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.PayrollLock;

/// <summary>
/// Stops payroll-sensitive records being created, changed or deleted while the employee's payroll area is locked.
///
/// <para>Ported from legacy <c>MyBaseObject.OnSaving</c>/<c>OnDeleting</c>. The rule, exactly as legacy has it:</para>
/// <list type="number">
/// <item><description>the record's type is flagged payroll-sensitive for the company (System Objects screen);</description></item>
/// <item><description>the record belongs to an employee (it has an <c>EmployeeId</c>);</description></item>
/// <item><description>that employee's payroll area -- from the org assignment in force TODAY, not on the record's
/// own date -- has its latest payroll period locked.</description></item>
/// </list>
/// <para>All three, and the change is refused. Any one missing, and it goes through: an employee with no org
/// assignment today, or no payroll area on it, is not checked.</para>
///
/// <para>It runs on every save rather than in each screen, as legacy did, so imports, Go-Live, workflow approvals
/// and the AI assistant are held to it too. See <see cref="PayrollLockEntityHook"/>.</para>
/// </summary>
public interface IPayrollLockGuard : IDomainService
{
    /// <summary>
    /// Throws when <paramref name="entity"/> may not be changed because its employee's payroll area is locked.
    /// </summary>
    void EnsureChangeAllowed(object entity);

    /// <summary>
    /// Lets everything saved in the current unit of work through, for processes the system runs itself.
    ///
    /// <para>Legacy's engines mark every record they write <c>IsSystemGenerated</c>, which skips this check. The
    /// payroll engine has to be exempt at the very least: a regular run is only allowed once the period IS
    /// locked, and it updates loans and absences as it goes. It marks the unit of work rather than switching
    /// the check off for the length of a method call, because the writes are saved when the unit of work
    /// completes -- after the method has returned.</para>
    /// </summary>
    void ExemptCurrentUnitOfWork();
}
