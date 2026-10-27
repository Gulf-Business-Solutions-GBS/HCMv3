using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IBreakShiftsAppService : IApplicationService
{
    Task<PagedResultDto<BreakShiftDto>> GetAll(GetAllBreakShiftsInput input);

    Task<GetBreakShiftForEditOutput> GetBreakShiftForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditBreakShiftDto input);

    Task Delete(EntityDto input);
}
