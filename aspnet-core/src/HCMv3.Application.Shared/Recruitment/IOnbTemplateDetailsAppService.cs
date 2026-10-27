using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Recruitment.Dto;

namespace HCMv3.Recruitment;

public interface IOnbTemplateDetailsAppService : IApplicationService
{
    Task<PagedResultDto<OnbTemplateDetailsDto>> GetAll(GetAllOnbTemplateDetailsInput input);

    Task<GetOnbTemplateDetailsForEditOutput> GetOnbTemplateDetailsForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOnbTemplateDetailsDto input);

    Task Delete(EntityDto input);
}
