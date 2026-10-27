using Abp.Mapperly;
using HCMv3.Authorization.Roles;
using HCMv3.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.GraphQL.Mappers;

[Mapper]
public partial class RoleToRoleDtoMapper : MapperBase<Role, RoleDto>
{
    public override partial RoleDto Map(Role source);
    public override partial void Map(Role source, RoleDto destination);
}

[Mapper]
public partial class RoleToUserDtoRoleDtoMapper : MapperBase<Role, UserDto.RoleDto>
{
    public override partial UserDto.RoleDto Map(Role source);
    public override partial void Map(Role source, UserDto.RoleDto destination);
}
