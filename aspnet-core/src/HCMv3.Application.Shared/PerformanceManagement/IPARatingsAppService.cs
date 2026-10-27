using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPARatingsAppService : IApplicationService
{
    Task<PagedResultDto<PARatingDto>> GetAll(GetAllPARatingsInput input);

    Task<GetPARatingForEditOutput> GetPARatingForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPARatingDto input);

    Task Delete(EntityDto input);
}
