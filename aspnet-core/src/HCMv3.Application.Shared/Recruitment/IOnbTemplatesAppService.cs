using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IOnbTemplatesAppService : IApplicationService
{
    Task<PagedResultDto<OnbTemplateDto>> GetAll(GetAllOnbTemplatesInput input);

    Task<GetOnbTemplateForEditOutput> GetOnbTemplateForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOnbTemplateDto input);

    Task Delete(EntityDto input);
}
