using Abp.Mapperly;
using HCMv3.Authorization.Users.Dto;
using HCMv3.Maui.Models.User;
using HCMv3.Organizations.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Maui.Mappers;

[Mapper]
public partial class UserListDtoToUserListModelMapper : MapperBase<UserListDto, UserListModel>
{
    [MapperIgnoreTarget(nameof(UserListModel.Photo))]
    public override partial UserListModel Map(UserListDto source);

    [MapperIgnoreTarget(nameof(UserListModel.Photo))]
    public override partial void Map(UserListDto source, UserListModel destination);
}

[Mapper]
public partial class UserEditDtoToCreateOrEditUserModelMapper : MapperBase<UserEditDto, CreateOrEditUserModel>
{
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.Photo))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.CreationTime))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.IsEmailConfirmed))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.IsNewUser))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.SendActivationEmail))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.SetRandomPassword))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.PasswordRepeat))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.Roles))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.SelectedOrganizationUnits))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.MemberedOrganizationUnits))]
    public override partial CreateOrEditUserModel Map(UserEditDto source);

    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.Photo))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.CreationTime))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.IsEmailConfirmed))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.IsNewUser))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.SendActivationEmail))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.SetRandomPassword))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.PasswordRepeat))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.Roles))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.SelectedOrganizationUnits))]
    [MapperIgnoreTarget(nameof(CreateOrEditUserModel.MemberedOrganizationUnits))]
    public override partial void Map(UserEditDto source, CreateOrEditUserModel destination);
}

[Mapper]
public partial class OrganizationUnitDtoToOrganizationUnitModelMapper : MapperBase<OrganizationUnitDto, OrganizationUnitModel>
{
    public override partial OrganizationUnitModel Map(OrganizationUnitDto source);
    public override partial void Map(OrganizationUnitDto source, OrganizationUnitModel destination);
}
