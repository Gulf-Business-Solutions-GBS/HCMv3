using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeDisciplinariesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDisciplinaryDto>> GetAll(GetAllEmployeeDisciplinariesInput input);

    Task<GetEmployeeDisciplinaryForEditOutput> GetEmployeeDisciplinaryForEdit(EntityDto input);

    /// <summary>
    /// What this offence would cost, for a record that does not exist yet. Read-only, and the point of it
    /// is timing: the consequence has to be visible BEFORE the record is raised.
    /// </summary>
    Task<DisciplinaryEscalationDto> GetDisciplinaryEscalation(GetDisciplinaryEscalationInput input);

    Task CreateOrEdit(CreateOrEditEmployeeDisciplinaryDto input);

    Task Delete(EntityDto input);
}
