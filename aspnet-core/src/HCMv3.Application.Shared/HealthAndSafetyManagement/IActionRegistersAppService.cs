using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HealthAndSafetyManagement.Dto;

namespace HCMv3.HealthAndSafetyManagement;

public interface IActionRegistersAppService : IApplicationService
{
    Task<PagedResultDto<ActionRegisterDto>> GetAll(GetAllActionRegistersInput input);

    Task<GetActionRegisterForEditOutput> GetActionRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditActionRegisterDto input);

    Task Delete(EntityDto input);
}
