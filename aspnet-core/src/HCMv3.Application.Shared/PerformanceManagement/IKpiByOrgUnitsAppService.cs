using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IKpiByOrgUnitsAppService : IApplicationService
{
    Task<PagedResultDto<KpiByOrgUnitDto>> GetAll(GetAllKpiByOrgUnitsInput input);

    Task<GetKpiByOrgUnitForEditOutput> GetKpiByOrgUnitForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditKpiByOrgUnitDto input);

    Task Delete(EntityDto input);
}
