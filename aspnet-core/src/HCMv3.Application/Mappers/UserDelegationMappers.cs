using Abp.Mapperly;
using HCMv3.Authorization.Delegation;
using HCMv3.Authorization.Users.Delegation.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class CreateUserDelegationDtoToUserDelegationMapper : MapperBase<CreateUserDelegationDto, UserDelegation>
{
    public override partial UserDelegation Map(CreateUserDelegationDto source);
    public override partial void Map(CreateUserDelegationDto source, UserDelegation destination);
}
