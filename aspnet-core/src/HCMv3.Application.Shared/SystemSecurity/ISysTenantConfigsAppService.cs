using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface ISysTenantConfigsAppService : IApplicationService
{
    Task<PagedResultDto<SysTenantConfigDto>> GetAll(GetAllSysTenantConfigsInput input);

    Task<GetSysTenantConfigForEditOutput> GetSysTenantConfigForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSysTenantConfigDto input);

    Task Delete(EntityDto input);
}
