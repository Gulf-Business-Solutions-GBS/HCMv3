using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAPeerReviewsAppService : IApplicationService
{
    Task<PagedResultDto<PAPeerReviewDto>> GetAll(GetAllPAPeerReviewsInput input);

    Task<GetPAPeerReviewForEditOutput> GetPAPeerReviewForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAPeerReviewDto input);

    Task Delete(EntityDto input);
}
