using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ManpowerPlanningManagement.Dto;

namespace HCMv3.ManpowerPlanningManagement;

public interface IManpowerPlanningDetailsAppService : IApplicationService
{
    Task<PagedResultDto<ManpowerPlanningDetailDto>> GetAll(GetAllManpowerPlanningDetailsInput input);

    Task<GetManpowerPlanningDetailForEditOutput> GetManpowerPlanningDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditManpowerPlanningDetailDto input);

    Task Delete(EntityDto input);
}
