using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Authorization.Permissions.Dto;

namespace HCMv3.Authorization.Permissions;

public interface IPermissionAppService : IApplicationService
{
    ListResultDto<FlatPermissionWithLevelDto> GetAllPermissions();
}

