using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IKpiCategoriesAppService : IApplicationService
{
    Task<PagedResultDto<KpiCategoryDto>> GetAll(GetAllKpiCategoriesInput input);

    Task<GetKpiCategoryForEditOutput> GetKpiCategoryForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditKpiCategoryDto input);

    Task Delete(EntityDto input);
}
