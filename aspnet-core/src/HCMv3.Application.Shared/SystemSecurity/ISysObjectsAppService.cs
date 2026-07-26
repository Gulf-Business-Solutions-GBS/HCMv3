using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface ISysObjectsAppService : IApplicationService
{
    Task<PagedResultDto<SysObjectDto>> GetAll(GetAllSysObjectsInput input);

    Task<GetSysObjectForEditOutput> GetSysObjectForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSysObjectDto input);

    Task Delete(EntityDto input);
}
