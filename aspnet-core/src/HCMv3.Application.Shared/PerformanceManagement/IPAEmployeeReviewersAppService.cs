using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAEmployeeReviewersAppService : IApplicationService
{
    Task<PagedResultDto<PAEmployeeReviewerDto>> GetAll(GetAllPAEmployeeReviewersInput input);

    Task<GetPAEmployeeReviewerForEditOutput> GetPAEmployeeReviewerForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAEmployeeReviewerDto input);

    Task Delete(EntityDto input);
}
