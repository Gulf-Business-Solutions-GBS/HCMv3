using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ProjectManagement.Dto;

namespace HCMv3.ProjectManagement;

public interface IProjectsAppService : IApplicationService
{
    Task<PagedResultDto<ProjectDto>> GetAll(GetAllProjectsInput input);

    Task<GetProjectForEditOutput> GetProjectForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditProjectDto input);

    Task Delete(EntityDto input);
}
