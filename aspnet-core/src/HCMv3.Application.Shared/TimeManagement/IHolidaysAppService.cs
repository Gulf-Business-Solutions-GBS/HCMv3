using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IHolidaysAppService : IApplicationService
{
    Task<PagedResultDto<HolidayDto>> GetAll(GetAllHolidaysInput input);

    Task<GetHolidayForEditOutput> GetHolidayForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditHolidayDto input);

    Task Delete(EntityDto input);
}
