using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAQuarter1ReviewsAppService : IApplicationService
{
    Task<PagedResultDto<PAQuarter1ReviewDto>> GetAll(GetAllPAQuarter1ReviewsInput input);

    Task<GetPAQuarter1ReviewForEditOutput> GetPAQuarter1ReviewForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAQuarter1ReviewDto input);

    Task Delete(EntityDto input);
}
