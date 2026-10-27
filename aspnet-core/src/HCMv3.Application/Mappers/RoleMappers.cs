using Abp.Authorization.Users;
using Abp.Mapperly;
using HCMv3.Authorization.Roles;
using HCMv3.Authorization.Roles.Dto;
using HCMv3.Authorization.Users.Dto;
using HCMv3.Organizations.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class RoleToRoleEditDtoMapper : MapperBase<Role, RoleEditDto>
{
    public override partial RoleEditDto Map(Role source);
    public override partial void Map(Role source, RoleEditDto destination);
}

[Mapper]
public partial class RoleEditDtoToRoleMapper : MapperBase<RoleEditDto, Role>
{
    public override partial Role Map(RoleEditDto source);
    public override partial void Map(RoleEditDto source, Role destination);
}

[Mapper]
public partial class RoleToRoleListDtoMapper : MapperBase<Role, RoleListDto>
{
    public override partial RoleListDto Map(Role source);
    public override partial void Map(Role source, RoleListDto destination);
}

// UserListRoleDto needs manual mapping as properties differ from UserRole
[Mapper]
public partial class UserRoleToUserListRoleDtoMapper : MapperBase<UserRole, UserListRoleDto>
{
    public override UserListRoleDto Map(UserRole source)
    {
        return new UserListRoleDto
        {
            RoleId = source.RoleId
            // RoleName needs to be set separately as UserRole doesn't have it
        };
    }

    public override void Map(UserRole source, UserListRoleDto destination)
    {
        destination.RoleId = source.RoleId;
        // RoleName needs to be set separately as UserRole doesn't have it
    }
}

[Mapper]
public partial class RoleToOrganizationUnitRoleListDtoMapper : MapperBase<Role, OrganizationUnitRoleListDto>
{
    public override partial OrganizationUnitRoleListDto Map(Role source);
    public override partial void Map(Role source, OrganizationUnitRoleListDto destination);
}
