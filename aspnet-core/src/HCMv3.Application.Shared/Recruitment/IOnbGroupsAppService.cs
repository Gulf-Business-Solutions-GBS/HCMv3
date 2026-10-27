using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IOnbGroupsAppService : IApplicationService
{
    Task<PagedResultDto<OnbGroupDto>> GetAll(GetAllOnbGroupsInput input);

    Task<GetOnbGroupForEditOutput> GetOnbGroupForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOnbGroupDto input);

    Task Delete(EntityDto input);
}
