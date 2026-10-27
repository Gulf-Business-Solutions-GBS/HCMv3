using Abp.Mapperly;
using HCMv3.ApiClient;
using HCMv3.Maui.Models.Common;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Maui.Mappers;

// Saving and restoring the chosen tenant on the phone. These lived in TenantMappers.cs next to the admin
// tenant screens and went with them; without them choosing a tenant, or starting the app, throws.
[Mapper]
public partial class TenantInformationToTenantInformationPersistanceModelMapper : MapperBase<TenantInformation, TenantInformationPersistanceModel>
{
    public override partial TenantInformationPersistanceModel Map(TenantInformation source);
    public override partial void Map(TenantInformation source, TenantInformationPersistanceModel destination);
}

[Mapper]
public partial class TenantInformationPersistanceModelToTenantInformationMapper : MapperBase<TenantInformationPersistanceModel, TenantInformation>
{
    public override TenantInformation Map(TenantInformationPersistanceModel source)
    {
        return new TenantInformation(source.TenancyName, source.TenantId);
    }

    public override void Map(TenantInformationPersistanceModel source, TenantInformation destination)
    {
        // TenantInformation properties are read-only via constructor, can't update in place
    }
}
