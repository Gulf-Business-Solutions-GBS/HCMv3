using Abp.Mapperly;
using HCMv3.MultiTenancy.Payments;
using HCMv3.MultiTenancy.Payments.Dto;
using HCMv3.Sessions.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class SubscriptionPaymentToSubscriptionPaymentDtoMapper : MapperBase<SubscriptionPayment, SubscriptionPaymentDto>
{
    public override SubscriptionPaymentDto Map(SubscriptionPayment source)
    {
        var dto = MapInternal(source);
        dto.TotalAmount = source.GetTotalAmount();
        return dto;
    }

    public override void Map(SubscriptionPayment source, SubscriptionPaymentDto destination)
    {
        MapInternal(source, destination);
        destination.TotalAmount = source.GetTotalAmount();
    }

    [MapperIgnoreTarget(nameof(SubscriptionPaymentDto.TotalAmount))]
    private partial SubscriptionPaymentDto MapInternal(SubscriptionPayment source);

    [MapperIgnoreTarget(nameof(SubscriptionPaymentDto.TotalAmount))]
    private partial void MapInternal(SubscriptionPayment source, SubscriptionPaymentDto destination);
}

[Mapper]
public partial class SubscriptionPaymentDtoToSubscriptionPaymentMapper : MapperBase<SubscriptionPaymentDto, SubscriptionPayment>
{
    public override partial SubscriptionPayment Map(SubscriptionPaymentDto source);
    public override partial void Map(SubscriptionPaymentDto source, SubscriptionPayment destination);
}

[Mapper]
public partial class SubscriptionPaymentProductToSubscriptionPaymentProductDtoMapper : MapperBase<SubscriptionPaymentProduct, SubscriptionPaymentProductDto>
{
    public override partial SubscriptionPaymentProductDto Map(SubscriptionPaymentProduct source);
    public override partial void Map(SubscriptionPaymentProduct source, SubscriptionPaymentProductDto destination);
}

[Mapper]
public partial class SubscriptionPaymentProductDtoToSubscriptionPaymentProductMapper : MapperBase<SubscriptionPaymentProductDto, SubscriptionPaymentProduct>
{
    public override partial SubscriptionPaymentProduct Map(SubscriptionPaymentProductDto source);
    public override partial void Map(SubscriptionPaymentProductDto source, SubscriptionPaymentProduct destination);
}

[Mapper]
public partial class SubscriptionPaymentToSubscriptionPaymentListDtoMapper : MapperBase<SubscriptionPayment, SubscriptionPaymentListDto>
{
    [MapperIgnoreSource(nameof(SubscriptionPayment.ErrorUrl))]
    public override partial SubscriptionPaymentListDto Map(SubscriptionPayment source);

    [MapperIgnoreSource(nameof(SubscriptionPayment.ErrorUrl))]
    public override partial void Map(SubscriptionPayment source, SubscriptionPaymentListDto destination);
}

[Mapper]
public partial class SubscriptionPaymentListDtoToSubscriptionPaymentMapper : MapperBase<SubscriptionPaymentListDto, SubscriptionPayment>
{
    [MapperIgnoreTarget(nameof(SubscriptionPayment.ErrorUrl))]
    public override partial SubscriptionPayment Map(SubscriptionPaymentListDto source);

    [MapperIgnoreTarget(nameof(SubscriptionPayment.ErrorUrl))]
    public override partial void Map(SubscriptionPaymentListDto source, SubscriptionPayment destination);
}

[Mapper]
public partial class SubscriptionPaymentToSubscriptionPaymentInfoDtoMapper : MapperBase<SubscriptionPayment, SubscriptionPaymentInfoDto>
{
    [MapperIgnoreSource(nameof(SubscriptionPayment.ErrorUrl))]
    public override partial SubscriptionPaymentInfoDto Map(SubscriptionPayment source);

    [MapperIgnoreSource(nameof(SubscriptionPayment.ErrorUrl))]
    public override partial void Map(SubscriptionPayment source, SubscriptionPaymentInfoDto destination);
}
