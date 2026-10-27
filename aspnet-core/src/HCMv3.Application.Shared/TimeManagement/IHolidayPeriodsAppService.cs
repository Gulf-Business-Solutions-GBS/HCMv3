using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IHolidayPeriodsAppService : IApplicationService
{
    Task<PagedResultDto<HolidayPeriodDto>> GetAll(GetAllHolidayPeriodsInput input);

    Task<GetHolidayPeriodForEditOutput> GetHolidayPeriodForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditHolidayPeriodDto input);

    Task Delete(EntityDto input);
}
