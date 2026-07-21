using Abp.Mapperly;
using HCMv3.ApiClient;
using HCMv3.Maui.Models.Common;
using HCMv3.Maui.Models.Tenants;
using HCMv3.MultiTenancy.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Maui.Mappers;

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

[Mapper]
public partial class TenantEditDtoToEditTenantModelMapper : MapperBase<TenantEditDto, EditTenantModel>
{
    [MapperIgnoreTarget(nameof(EditTenantModel.IsSubscriptionFieldVisible))]
    [MapperIgnoreTarget(nameof(EditTenantModel.IsUnlimitedTimeSubscription))]
    [MapperIgnoreTarget(nameof(EditTenantModel.Editions))]
    [MapperIgnoreTarget(nameof(EditTenantModel.SelectedEdition))]
    public override partial EditTenantModel Map(TenantEditDto source);

    [MapperIgnoreTarget(nameof(EditTenantModel.IsSubscriptionFieldVisible))]
    [MapperIgnoreTarget(nameof(EditTenantModel.IsUnlimitedTimeSubscription))]
    [MapperIgnoreTarget(nameof(EditTenantModel.Editions))]
    [MapperIgnoreTarget(nameof(EditTenantModel.SelectedEdition))]
    public override partial void Map(TenantEditDto source, EditTenantModel destination);
}
