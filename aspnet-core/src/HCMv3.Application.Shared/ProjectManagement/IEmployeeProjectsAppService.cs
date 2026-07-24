using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ProjectManagement.Dto;

namespace HCMv3.ProjectManagement;

public interface IEmployeeProjectsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeProjectDto>> GetAll(GetAllEmployeeProjectsInput input);

    Task<GetEmployeeProjectForEditOutput> GetEmployeeProjectForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeProjectDto input);

    Task Delete(EntityDto input);
}
