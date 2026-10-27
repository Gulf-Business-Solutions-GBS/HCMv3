using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IDailyDutiesAppService : IApplicationService
{
    Task<PagedResultDto<DailyDutyDto>> GetAll(GetAllDailyDutiesInput input);

    Task<GetDailyDutyForEditOutput> GetDailyDutyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditDailyDutyDto input);

    Task Delete(EntityDto input);
}
