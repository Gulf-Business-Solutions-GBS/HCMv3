using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.CompetencyDevelopment.Dto;

namespace HCMv3.CompetencyDevelopment;

public interface ICDMidYearReviewsAppService : IApplicationService
{
    Task<PagedResultDto<CDMidYearReviewDto>> GetAll(GetAllCDMidYearReviewsInput input);

    Task<GetCDMidYearReviewForEditOutput> GetCDMidYearReviewForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCDMidYearReviewDto input);

    Task Delete(EntityDto input);
}
