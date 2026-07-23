using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IDutyPeriodsAppService : IApplicationService
{
    Task<PagedResultDto<DutyPeriodDto>> GetAll(GetAllDutyPeriodsInput input);

    Task<GetDutyPeriodForEditOutput> GetDutyPeriodForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditDutyPeriodDto input);

    Task Delete(EntityDto input);
}
