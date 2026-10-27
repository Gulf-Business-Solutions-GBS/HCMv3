using System.Collections.Generic;
using HCMv3.Authorization.Permissions.Dto;

namespace HCMv3.Authorization.Users.Dto;

public class GetUserPermissionsForEditOutput
{
    public List<FlatPermissionDto> Permissions { get; set; }

    public List<string> GrantedPermissionNames { get; set; }
}

