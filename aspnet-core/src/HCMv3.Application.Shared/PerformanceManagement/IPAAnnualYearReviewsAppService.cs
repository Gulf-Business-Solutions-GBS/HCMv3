using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAAnnualYearReviewsAppService : IApplicationService
{
    Task<PagedResultDto<PAAnnualYearReviewDto>> GetAll(GetAllPAAnnualYearReviewsInput input);

    Task<GetPAAnnualYearReviewForEditOutput> GetPAAnnualYearReviewForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAAnnualYearReviewDto input);

    Task Delete(EntityDto input);
}
