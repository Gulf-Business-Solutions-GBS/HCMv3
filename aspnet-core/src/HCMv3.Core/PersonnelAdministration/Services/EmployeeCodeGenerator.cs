using System.Linq;
using Abp.Domain.Repositories;
using Abp.Extensions;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Legacy <c>GetMaxCode</c> parity (same pattern as <c>CitiesAppService.AssignCodeIfEmptyAsync</c>):
/// when Code is blank, assign (max existing numeric code) + 1. Simplified from the legacy
/// <c>NumberSeries.GetCode(EmploymentType, ...)</c> per-EmploymentType series. Legacy ran this in
/// <c>Employee.OnSaving</c>, so it covered every way an employee was created; here it is called explicitly by
/// <c>EmployeesAppService</c> and by the recruitment Joined hand-off (<c>ApplicationStatusesAppService</c>).
/// Synchronous LINQ because HCMv3.Core does not use the EF Core async extensions (see
/// <c>EmployeeAbsenceCalculationService</c>).
/// </summary>
public class EmployeeCodeGenerator : HCMv3DomainServiceBase, IEmployeeCodeGenerator
{
    private readonly IRepository<Employee> _employeeRepository;

    public EmployeeCodeGenerator(IRepository<Employee> employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public void AssignCodeIfEmpty(Employee employee)
    {
        if (!employee.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = _employeeRepository.GetAll()
            .Where(e => e.Code != null && e.Code != "")
            .Select(e => e.Code)
            .ToList();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        employee.Code = (maxCode + 1).ToString();
    }
}
