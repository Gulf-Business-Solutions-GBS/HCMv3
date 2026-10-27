using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeTasksAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeTaskDto>> GetAll(GetAllEmployeeTasksInput input);

    Task<GetEmployeeTaskForEditOutput> GetEmployeeTaskForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeTaskDto input);

    Task Delete(EntityDto input);
}
