using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAEmployeeRatingsAppService : IApplicationService
{
    Task<PagedResultDto<PAEmployeeRatingDto>> GetAll(GetAllPAEmployeeRatingsInput input);

    Task<GetPAEmployeeRatingForEditOutput> GetPAEmployeeRatingForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAEmployeeRatingDto input);

    Task Delete(EntityDto input);
}
