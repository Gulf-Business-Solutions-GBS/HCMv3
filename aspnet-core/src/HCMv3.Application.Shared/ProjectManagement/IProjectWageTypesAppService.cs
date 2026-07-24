using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ProjectManagement.Dto;

namespace HCMv3.ProjectManagement;

public interface IProjectWageTypesAppService : IApplicationService
{
    Task<PagedResultDto<ProjectWageTypeDto>> GetAll(GetAllProjectWageTypesInput input);

    Task<GetProjectWageTypeForEditOutput> GetProjectWageTypeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditProjectWageTypeDto input);

    Task Delete(EntityDto input);
}
