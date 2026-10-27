using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ManpowerPlanningManagement.Dto;

namespace HCMv3.ManpowerPlanningManagement;

public interface IManpowerPlanningPeriodsAppService : IApplicationService
{
    Task<PagedResultDto<ManpowerPlanningPeriodDto>> GetAll(GetAllManpowerPlanningPeriodsInput input);

    Task<GetManpowerPlanningPeriodForEditOutput> GetManpowerPlanningPeriodForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditManpowerPlanningPeriodDto input);

    Task Delete(EntityDto input);
}
