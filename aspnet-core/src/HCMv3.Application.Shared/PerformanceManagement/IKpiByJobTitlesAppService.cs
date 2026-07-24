using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IKpiByJobTitlesAppService : IApplicationService
{
    Task<PagedResultDto<KpiByJobTitleDto>> GetAll(GetAllKpiByJobTitlesInput input);

    Task<GetKpiByJobTitleForEditOutput> GetKpiByJobTitleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditKpiByJobTitleDto input);

    Task Delete(EntityDto input);
}
