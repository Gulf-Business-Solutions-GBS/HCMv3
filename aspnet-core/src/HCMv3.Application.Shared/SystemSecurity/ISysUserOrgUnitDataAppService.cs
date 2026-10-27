using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity.Dto;

namespace HCMv3.SystemSecurity;

public interface ISysUserOrgUnitDataAppService : IApplicationService
{
    Task<PagedResultDto<SysUserOrgUnitDataDto>> GetAll(GetAllSysUserOrgUnitDataInput input);

    Task<GetSysUserOrgUnitDataForEditOutput> GetSysUserOrgUnitDataForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSysUserOrgUnitDataDto input);

    Task Delete(EntityDto input);
}
