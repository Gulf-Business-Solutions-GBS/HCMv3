using System;
using System.Linq;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Legacy <c>Employee.PerformValidation</c> parity: BirthDate/JoiningDate/ProbationDays sanity checks and
/// duplicate Code/CivilID/AccessID/LoginID checks (CivilID/AccessID scoped to active employees, matching
/// legacy). Legacy ran these in <c>OnSaving</c>, so every way of creating an employee went through them; here
/// they are called by <c>EmployeesAppService</c> and by the recruitment Joined hand-off
/// (<c>ApplicationStatusesAppService</c>). Synchronous LINQ because HCMv3.Core does not use the EF Core async
/// extensions (see <c>EmployeeAbsenceCalculationService</c>).
/// </summary>
public class EmployeeValidator : HCMv3DomainServiceBase, IEmployeeValidator
{
    private readonly IRepository<Employee> _employeeRepository;

    public EmployeeValidator(IRepository<Employee> employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public void Validate(Employee employee)
    {
        var duplicateCode = _employeeRepository.GetAll()
            .Any(e => e.Id != employee.Id && e.Code == employee.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        if (employee.BirthDate.Date >= DateTime.Now.Date)
        {
            throw new UserFriendlyException(L("BirthDateCannotBeFutureOrCurrent"));
        }

        if (employee.JoiningDate.Date <= employee.BirthDate.Date)
        {
            throw new UserFriendlyException(L("JoiningDateCannotBeEarlierThanBirthDate"));
        }

        if (employee.ProbationDays < 0)
        {
            throw new UserFriendlyException(L("ProbationDaysCannotBeNegative"));
        }

        // The duplicate lookups below project to an object, not straight to Code: selecting Code alone read a
        // match with a blank code as "no match", so a clash with such an employee (e.g. one created by the old
        // recruitment Joined path) slipped through.
        //
        // Legacy also skips the employee's own previous code (Employee.cs 1640/1651), so a rejoin that creates a
        // new employee can be saved while the old record still carries the same civil ID and access ID. Applied
        // only when a previous code is set: legacy's "Code != ''" form would also skip every blank-coded employee,
        // which is the clash the projection above was introduced to catch.
        var previousCode = employee.PreviousEmpCode.IsNullOrWhiteSpace() ? null : employee.PreviousEmpCode;

        var duplicateCivilId = _employeeRepository.GetAll()
            .Where(e => e.Id != employee.Id
                        && e.CivilID == employee.CivilID
                        && e.Status == EmployeeStatus.Active)
            .WhereIf(previousCode != null, e => e.Code != previousCode)
            .Select(e => new { e.Code })
            .FirstOrDefault();
        if (duplicateCivilId != null)
        {
            throw new UserFriendlyException(L("DuplicateCivilIdFound", duplicateCivilId.Code));
        }

        if (!employee.AccessID.IsNullOrWhiteSpace())
        {
            var duplicateAccessId = _employeeRepository.GetAll()
                .Where(e => e.Id != employee.Id
                            && e.AccessID == employee.AccessID
                            && e.Status == EmployeeStatus.Active)
                .WhereIf(previousCode != null, e => e.Code != previousCode)
                .Select(e => new { e.Code })
                .FirstOrDefault();
            if (duplicateAccessId != null)
            {
                throw new UserFriendlyException(L("DuplicateAccessIdFound", duplicateAccessId.Code));
            }
        }

        // LoginID identifies WHO IS SIGNED IN, so it must name exactly one employee.
        //
        // Deliberately NOT scoped to Status == Active, unlike the CivilID/AccessID checks above. Those
        // answer "is this identifier in use right now"; this one answers "can this login ever resolve to
        // two people". A resigned employee's row keeps its LoginID, and the fallback lookup in
        // EmployeeSessionService does not filter by status either -- so allowing a leaver's LoginID to be
        // reused would reintroduce exactly the ambiguity Employee.UserId exists to remove. To free a login
        // for reuse, clear it on the employee who left; the error message says so.
        if (!employee.LoginID.IsNullOrWhiteSpace())
        {
            var duplicateLoginId = _employeeRepository.GetAll()
                .Where(e => e.Id != employee.Id && e.LoginID == employee.LoginID)
                .Select(e => new { e.Code })
                .FirstOrDefault();
            if (duplicateLoginId != null)
            {
                throw new UserFriendlyException(L("DuplicateLoginIdFound", employee.LoginID, duplicateLoginId.Code));
            }
        }
    }
}
