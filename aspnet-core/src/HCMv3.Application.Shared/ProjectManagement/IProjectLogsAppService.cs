using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ProjectManagement.Dto;

namespace HCMv3.ProjectManagement;

public interface IProjectLogsAppService : IApplicationService
{
    Task<PagedResultDto<ProjectLogDto>> GetAll(GetAllProjectLogsInput input);

    Task<GetProjectLogForEditOutput> GetProjectLogForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditProjectLogDto input);

    Task Delete(EntityDto input);
}
