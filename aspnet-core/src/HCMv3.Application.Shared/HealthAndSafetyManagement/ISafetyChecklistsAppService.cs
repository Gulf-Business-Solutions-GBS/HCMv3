using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HealthAndSafetyManagement.Dto;

namespace HCMv3.HealthAndSafetyManagement;

public interface ISafetyChecklistsAppService : IApplicationService
{
    Task<PagedResultDto<SafetyChecklistDto>> GetAll(GetAllSafetyChecklistsInput input);

    Task<GetSafetyChecklistForEditOutput> GetSafetyChecklistForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSafetyChecklistDto input);

    Task Delete(EntityDto input);
}
