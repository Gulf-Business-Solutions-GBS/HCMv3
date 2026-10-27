using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IKpiByJobTitleDetailsAppService : IApplicationService
{
    Task<PagedResultDto<KpiByJobTitleDetailDto>> GetAll(GetAllKpiByJobTitleDetailsInput input);

    Task<GetKpiByJobTitleDetailForEditOutput> GetKpiByJobTitleDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditKpiByJobTitleDetailDto input);

    Task Delete(EntityDto input);
}
