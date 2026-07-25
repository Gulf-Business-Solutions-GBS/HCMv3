using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IOnbGroupDetailsAppService : IApplicationService
{
    Task<PagedResultDto<OnbGroupDetailDto>> GetAll(GetAllOnbGroupDetailsInput input);

    Task<GetOnbGroupDetailForEditOutput> GetOnbGroupDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOnbGroupDetailDto input);

    Task Delete(EntityDto input);
}
