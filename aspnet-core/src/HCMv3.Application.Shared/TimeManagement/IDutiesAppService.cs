using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

public interface IDutiesAppService : IApplicationService
{
    Task<PagedResultDto<DutyDto>> GetAll(GetAllDutiesInput input);

    Task<GetDutyForEditOutput> GetDutyForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditDutyDto input);

    Task Delete(EntityDto input);
}
