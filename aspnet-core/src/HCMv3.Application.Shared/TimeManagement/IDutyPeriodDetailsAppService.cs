using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IDutyPeriodDetailsAppService : IApplicationService
{
    Task<PagedResultDto<DutyPeriodDetailDto>> GetAll(GetAllDutyPeriodDetailsInput input);

    Task<GetDutyPeriodDetailForEditOutput> GetDutyPeriodDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditDutyPeriodDetailDto input);

    Task Delete(EntityDto input);
}
