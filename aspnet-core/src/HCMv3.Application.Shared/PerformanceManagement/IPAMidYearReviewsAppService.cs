using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAMidYearReviewsAppService : IApplicationService
{
    Task<PagedResultDto<PAMidYearReviewDto>> GetAll(GetAllPAMidYearReviewsInput input);

    Task<GetPAMidYearReviewForEditOutput> GetPAMidYearReviewForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAMidYearReviewDto input);

    Task Delete(EntityDto input);
}
