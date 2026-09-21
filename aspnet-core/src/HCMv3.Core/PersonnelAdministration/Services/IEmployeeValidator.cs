using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Save-time checks every new or changed <see cref="Employee"/> must pass, whichever path writes it. See
/// <see cref="EmployeeValidator"/>.
/// </summary>
public interface IEmployeeValidator : IDomainService
{
    /// <summary>Throws <c>UserFriendlyException</c> on the first rule the employee breaks.</summary>
    void Validate(Employee employee);
}
