using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IKpiByOrgUnitDetailsAppService : IApplicationService
{
    Task<PagedResultDto<KpiByOrgUnitDetailDto>> GetAll(GetAllKpiByOrgUnitDetailsInput input);

    Task<GetKpiByOrgUnitDetailForEditOutput> GetKpiByOrgUnitDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditKpiByOrgUnitDetailDto input);

    Task Delete(EntityDto input);
}
