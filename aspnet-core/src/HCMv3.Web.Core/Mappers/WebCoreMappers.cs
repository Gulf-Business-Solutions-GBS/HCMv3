using Abp.AspNetZeroCore.Web.Authentication.External;
using Abp.Mapperly;
using HCMv3.Web.Models.TokenAuth;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Web.Mappers;

[Mapper]
public partial class ExternalLoginProviderInfoToModelMapper : MapperBase<ExternalLoginProviderInfo, ExternalLoginProviderInfoModel>
{
    public override partial ExternalLoginProviderInfoModel Map(ExternalLoginProviderInfo source);
    public override partial void Map(ExternalLoginProviderInfo source, ExternalLoginProviderInfoModel destination);
}
