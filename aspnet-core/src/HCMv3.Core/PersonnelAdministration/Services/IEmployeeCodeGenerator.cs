using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Assigns employee codes from the tenant's <see cref="HCMv3.GeneralMasterInfo.NumberSeries"/>. Shared by every
/// path that creates an employee, so none of them can leave one without a code. See
/// <see cref="EmployeeCodeGenerator"/>.
/// </summary>
public interface IEmployeeCodeGenerator : IDomainService
{
    /// <summary>Gives a blank-coded employee the next code of the series for their employment type.</summary>
    void AssignCodeIfEmpty(Employee employee);

    /// <summary>
    /// Legacy <c>Employee.OnSaving</c> "Update Code For EmploymentType": an existing employee whose employment type
    /// has changed is given the next code of the series for the NEW type. Nothing changes when the type is the same
    /// or no series applies.
    /// </summary>
    void ReassignCodeIfEmploymentTypeChanged(Employee employee, EmploymentType previousEmploymentType);
}
