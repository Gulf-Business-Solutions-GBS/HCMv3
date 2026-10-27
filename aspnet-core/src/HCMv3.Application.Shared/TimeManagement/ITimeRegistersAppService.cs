using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface ITimeRegistersAppService : IApplicationService
{
    Task<PagedResultDto<TimeRegisterDto>> GetAll(GetAllTimeRegistersInput input);

    Task<GetTimeRegisterForEditOutput> GetTimeRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditTimeRegisterDto input);

    Task Delete(EntityDto input);
}
