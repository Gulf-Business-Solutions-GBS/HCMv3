using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IOnbAppTaskCommentsAppService : IApplicationService
{
    Task<PagedResultDto<OnbAppTaskCommentsDto>> GetAll(GetAllOnbAppTaskCommentsInput input);

    Task<GetOnbAppTaskCommentsForEditOutput> GetOnbAppTaskCommentsForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOnbAppTaskCommentsDto input);

    Task Delete(EntityDto input);
}
