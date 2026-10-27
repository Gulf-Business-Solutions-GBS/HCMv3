using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.CompetencyDevelopment.Dto;

namespace HCMv3.CompetencyDevelopment;

public interface ICDAnnualYearReviewsAppService : IApplicationService
{
    Task<PagedResultDto<CDAnnualYearReviewDto>> GetAll(GetAllCDAnnualYearReviewsInput input);

    Task<GetCDAnnualYearReviewForEditOutput> GetCDAnnualYearReviewForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditCDAnnualYearReviewDto input);

    Task Delete(EntityDto input);
}
