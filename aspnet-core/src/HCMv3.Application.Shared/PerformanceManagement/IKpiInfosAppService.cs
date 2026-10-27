using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IKpiInfosAppService : IApplicationService
{
    Task<PagedResultDto<KpiInfoDto>> GetAll(GetAllKpiInfosInput input);

    Task<GetKpiInfoForEditOutput> GetKpiInfoForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditKpiInfoDto input);

    Task Delete(EntityDto input);
}
