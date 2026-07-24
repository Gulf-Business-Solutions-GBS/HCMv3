using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAQuarter3ReviewsAppService : IApplicationService
{
    Task<PagedResultDto<PAQuarter3ReviewDto>> GetAll(GetAllPAQuarter3ReviewsInput input);

    Task<GetPAQuarter3ReviewForEditOutput> GetPAQuarter3ReviewForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAQuarter3ReviewDto input);

    Task Delete(EntityDto input);
}
