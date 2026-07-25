using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IOnbVideoLinksAppService : IApplicationService
{
    Task<PagedResultDto<OnbVideoLinkDto>> GetAll(GetAllOnbVideoLinksInput input);

    Task<GetOnbVideoLinkForEditOutput> GetOnbVideoLinkForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOnbVideoLinkDto input);

    Task Delete(EntityDto input);
}
