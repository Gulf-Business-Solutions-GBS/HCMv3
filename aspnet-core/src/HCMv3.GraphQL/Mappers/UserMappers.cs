using Abp.Mapperly;
using HCMv3.Authorization.Users;
using HCMv3.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.GraphQL.Mappers;

[Mapper]
public partial class UserToUserDtoMapper : MapperBase<User, UserDto>
{
    [MapperIgnoreTarget(nameof(UserDto.Roles))]
    [MapperIgnoreTarget(nameof(UserDto.OrganizationUnits))]
    public override partial UserDto Map(User source);

    [MapperIgnoreTarget(nameof(UserDto.Roles))]
    [MapperIgnoreTarget(nameof(UserDto.OrganizationUnits))]
    public override partial void Map(User source, UserDto destination);
}
