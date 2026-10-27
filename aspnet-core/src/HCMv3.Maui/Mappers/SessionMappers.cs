using Abp.Mapperly;
using HCMv3.Maui.Models.Common;
using HCMv3.Sessions.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Maui.Mappers;

[Mapper]
public partial class GetCurrentLoginInformationsOutputToCurrentLoginInformationPersistanceModelMapper : MapperBase<GetCurrentLoginInformationsOutput, CurrentLoginInformationPersistanceModel>
{
    public override partial CurrentLoginInformationPersistanceModel Map(GetCurrentLoginInformationsOutput source);
    public override partial void Map(GetCurrentLoginInformationsOutput source, CurrentLoginInformationPersistanceModel destination);
}

[Mapper]
public partial class CurrentLoginInformationPersistanceModelToGetCurrentLoginInformationsOutputMapper : MapperBase<CurrentLoginInformationPersistanceModel, GetCurrentLoginInformationsOutput>
{
    public override partial GetCurrentLoginInformationsOutput Map(CurrentLoginInformationPersistanceModel source);
    public override partial void Map(CurrentLoginInformationPersistanceModel source, GetCurrentLoginInformationsOutput destination);
}

[Mapper]
public partial class UserLoginInfoDtoToUserLoginInfoPersistanceModelMapper : MapperBase<UserLoginInfoDto, UserLoginInfoPersistanceModel>
{
    public override partial UserLoginInfoPersistanceModel Map(UserLoginInfoDto source);
    public override partial void Map(UserLoginInfoDto source, UserLoginInfoPersistanceModel destination);
}

[Mapper]
public partial class UserLoginInfoPersistanceModelToUserLoginInfoDtoMapper : MapperBase<UserLoginInfoPersistanceModel, UserLoginInfoDto>
{
    public override partial UserLoginInfoDto Map(UserLoginInfoPersistanceModel source);
    public override partial void Map(UserLoginInfoPersistanceModel source, UserLoginInfoDto destination);
}

[Mapper]
public partial class TenantLoginInfoDtoToTenantLoginInfoPersistanceModelMapper : MapperBase<TenantLoginInfoDto, TenantLoginInfoPersistanceModel>
{
    public override partial TenantLoginInfoPersistanceModel Map(TenantLoginInfoDto source);
    public override partial void Map(TenantLoginInfoDto source, TenantLoginInfoPersistanceModel destination);
}

[Mapper]
public partial class TenantLoginInfoPersistanceModelToTenantLoginInfoDtoMapper : MapperBase<TenantLoginInfoPersistanceModel, TenantLoginInfoDto>
{
    public override partial TenantLoginInfoDto Map(TenantLoginInfoPersistanceModel source);
    public override partial void Map(TenantLoginInfoPersistanceModel source, TenantLoginInfoDto destination);
}

[Mapper]
public partial class ApplicationInfoDtoToApplicationInfoPersistanceModelMapper : MapperBase<ApplicationInfoDto, ApplicationInfoPersistanceModel>
{
    public override partial ApplicationInfoPersistanceModel Map(ApplicationInfoDto source);
    public override partial void Map(ApplicationInfoDto source, ApplicationInfoPersistanceModel destination);
}

[Mapper]
public partial class ApplicationInfoPersistanceModelToApplicationInfoDtoMapper : MapperBase<ApplicationInfoPersistanceModel, ApplicationInfoDto>
{
    public override partial ApplicationInfoDto Map(ApplicationInfoPersistanceModel source);
    public override partial void Map(ApplicationInfoPersistanceModel source, ApplicationInfoDto destination);
}
