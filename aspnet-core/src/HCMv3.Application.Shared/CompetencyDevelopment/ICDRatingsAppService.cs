using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.CompetencyDevelopment.Dto;

namespace HCMv3.CompetencyDevelopment;

public interface ICDRatingsAppService : IApplicationService
{
    Task<PagedResultDto<CDRatingDto>> GetAll(GetAllCDRatingsInput input);

    Task<GetCDRatingForEditOutput> GetCDRatingForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCDRatingDto input);

    Task Delete(EntityDto input);
}
