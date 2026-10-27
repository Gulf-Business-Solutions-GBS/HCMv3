using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ProjectManagement.Dto;

namespace HCMv3.ProjectManagement;

public interface IProjectRegisterDetailsAppService : IApplicationService
{
    Task<PagedResultDto<ProjectRegisterDetailDto>> GetAll(GetAllProjectRegisterDetailsInput input);

    Task<GetProjectRegisterDetailForEditOutput> GetProjectRegisterDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditProjectRegisterDetailDto input);

    Task Delete(EntityDto input);
}
