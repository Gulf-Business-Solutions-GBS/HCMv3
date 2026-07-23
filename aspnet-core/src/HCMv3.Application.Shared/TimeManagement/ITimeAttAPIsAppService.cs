using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface ITimeAttAPIsAppService : IApplicationService
{
    Task<PagedResultDto<TimeAttAPIDto>> GetAll(GetAllTimeAttAPIsInput input);

    Task<GetTimeAttAPIForEditOutput> GetTimeAttAPIForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTimeAttAPIDto input);

    Task Delete(EntityDto input);
}
