using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ProjectManagement.Dto;

namespace HCMv3.ProjectManagement;

public interface IProjectRegistersAppService : IApplicationService
{
    Task<PagedResultDto<ProjectRegisterDto>> GetAll(GetAllProjectRegistersInput input);

    Task<GetProjectRegisterForEditOutput> GetProjectRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditProjectRegisterDto input);

    Task Delete(EntityDto input);
}
