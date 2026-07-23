using System;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupAbsencesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupAbsenceDto>> GetAll(GetAllEmployeeSubGroupAbsencesInput input);

    Task<GetEmployeeSubGroupAbsenceForEditOutput> GetEmployeeSubGroupAbsenceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupAbsenceDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// Legacy <c>EmployeeSubGroupAbsence.GetEmployeeSubGroupLeave</c> parity: resolves the applicable leave
    /// quota row for an employee on a given date by years-of-service tiering (ChangeYear) computed from
    /// <see cref="HCMv3.PersonnelAdministration.Employee.JoiningDate"/>.
    /// </summary>
    Task<EmployeeSubGroupAbsenceDto> GetEmployeeSubGroupLeave(int employeeSubGroupId, int employeeId, int absenceTypeId, DateTime dayDate);
}
