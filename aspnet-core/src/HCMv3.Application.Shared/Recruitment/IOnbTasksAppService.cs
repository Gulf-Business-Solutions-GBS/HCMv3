using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IOnbTasksAppService : IApplicationService
{
    Task<PagedResultDto<OnbTaskDto>> GetAll(GetAllOnbTasksInput input);

    Task<GetOnbTaskForEditOutput> GetOnbTaskForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOnbTaskDto input);

    Task Delete(EntityDto input);
}
