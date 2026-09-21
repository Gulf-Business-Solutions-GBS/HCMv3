using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Assigns the next employee code to an <see cref="Employee"/> whose code is blank. Shared by every path that
/// creates an employee, so none of them can leave one without a code. See <see cref="EmployeeCodeGenerator"/>.
/// </summary>
public interface IEmployeeCodeGenerator : IDomainService
{
    void AssignCodeIfEmpty(Employee employee);
}
