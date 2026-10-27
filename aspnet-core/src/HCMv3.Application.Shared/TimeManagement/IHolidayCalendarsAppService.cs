using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IHolidayCalendarsAppService : IApplicationService
{
    Task<PagedResultDto<HolidayCalendarDto>> GetAll(GetAllHolidayCalendarsInput input);

    Task<GetHolidayCalendarForEditOutput> GetHolidayCalendarForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditHolidayCalendarDto input);

    Task Delete(EntityDto input);
}
