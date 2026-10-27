using Abp.Authorization.Users;
using Abp.Mapperly;
using HCMv3.Authorization.Users;
using HCMv3.Authorization.Users.Dto;
using HCMv3.Authorization.Users.Importing.Dto;
using HCMv3.Authorization.Users.Profile.Dto;
using HCMv3.Chat.Dto;
using HCMv3.Common.Dto;
using HCMv3.Organizations.Dto;
using HCMv3.Sessions.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class UserToUserLoginInfoDtoMapper : MapperBase<User, UserLoginInfoDto>
{
    public override partial UserLoginInfoDto Map(User source);
    public override partial void Map(User source, UserLoginInfoDto destination);
}

[Mapper]
public partial class UserToUserEditDtoMapper : MapperBase<User, UserEditDto>
{
    [MapperIgnoreTarget(nameof(UserEditDto.Password))]
    public override partial UserEditDto Map(User source);

    [MapperIgnoreTarget(nameof(UserEditDto.Password))]
    public override partial void Map(User source, UserEditDto destination);
}

[Mapper]
public partial class UserEditDtoToUserMapper : MapperBase<UserEditDto, User>
{
    [MapperIgnoreTarget(nameof(User.Password))]
    public override partial User Map(UserEditDto source);

    [MapperIgnoreTarget(nameof(User.Password))]
    public override partial void Map(UserEditDto source, User destination);
}

[Mapper]
public partial class UserToUserListDtoMapper : MapperBase<User, UserListDto>
{
    public override partial UserListDto Map(User source);
    public override partial void Map(User source, UserListDto destination);
}

[Mapper]
public partial class UserToChatUserDtoMapper : MapperBase<User, ChatUserDto>
{
    public override partial ChatUserDto Map(User source);
    public override partial void Map(User source, ChatUserDto destination);
}

[Mapper]
public partial class UserToOrganizationUnitUserListDtoMapper : MapperBase<User, OrganizationUnitUserListDto>
{
    public override partial OrganizationUnitUserListDto Map(User source);
    public override partial void Map(User source, OrganizationUnitUserListDto destination);
}

[Mapper]
public partial class UserToCurrentUserProfileEditDtoMapper : MapperBase<User, CurrentUserProfileEditDto>
{
    public override partial CurrentUserProfileEditDto Map(User source);
    public override partial void Map(User source, CurrentUserProfileEditDto destination);
}

[Mapper]
public partial class CurrentUserProfileEditDtoToUserMapper : MapperBase<CurrentUserProfileEditDto, User>
{
    public override partial User Map(CurrentUserProfileEditDto source);
    public override partial void Map(CurrentUserProfileEditDto source, User destination);
}

[Mapper]
public partial class ImportUserDtoToUserMapper : MapperBase<ImportUserDto, User>
{
    [MapperIgnoreTarget(nameof(User.Roles))]
    public override partial User Map(ImportUserDto source);

    [MapperIgnoreTarget(nameof(User.Roles))]
    public override partial void Map(ImportUserDto source, User destination);
}

[Mapper]
public partial class UserToFindUsersOutputDtoMapper : MapperBase<User, FindUsersOutputDto>
{
    public override partial FindUsersOutputDto Map(User source);
    public override partial void Map(User source, FindUsersOutputDto destination);
}

[Mapper]
public partial class UserToFindOrganizationUnitUsersOutputDtoMapper : MapperBase<User, FindOrganizationUnitUsersOutputDto>
{
    public override partial FindOrganizationUnitUsersOutputDto Map(User source);
    public override partial void Map(User source, FindOrganizationUnitUsersOutputDto destination);
}

[Mapper]
public partial class UserLoginAttemptToUserLoginAttemptDtoMapper : MapperBase<UserLoginAttempt, UserLoginAttemptDto>
{
    public override partial UserLoginAttemptDto Map(UserLoginAttempt source);
    public override partial void Map(UserLoginAttempt source, UserLoginAttemptDto destination);
}
