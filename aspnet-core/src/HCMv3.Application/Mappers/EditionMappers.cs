using Abp.Application.Editions;
using Abp.Mapperly;
using HCMv3.Editions;
using HCMv3.Editions.Dto;
using HCMv3.Sessions.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class SubscribableEditionToEditionEditDtoMapper : MapperBase<SubscribableEdition, EditionEditDto>
{
    public override partial EditionEditDto Map(SubscribableEdition source);
    public override partial void Map(SubscribableEdition source, EditionEditDto destination);
}

[Mapper]
public partial class EditionEditDtoToSubscribableEditionMapper : MapperBase<EditionEditDto, SubscribableEdition>
{
    public override partial SubscribableEdition Map(EditionEditDto source);
    public override partial void Map(EditionEditDto source, SubscribableEdition destination);
}

[Mapper]
public partial class EditionCreateDtoToSubscribableEditionMapper : MapperBase<EditionCreateDto, SubscribableEdition>
{
    public override partial SubscribableEdition Map(EditionCreateDto source);
    public override partial void Map(EditionCreateDto source, SubscribableEdition destination);
}

[Mapper]
public partial class SubscribableEditionToEditionSelectDtoMapper : MapperBase<SubscribableEdition, EditionSelectDto>
{
    public override partial EditionSelectDto Map(SubscribableEdition source);
    public override partial void Map(SubscribableEdition source, EditionSelectDto destination);
}

[Mapper]
public partial class EditionSelectDtoToSubscribableEditionMapper : MapperBase<EditionSelectDto, SubscribableEdition>
{
    public override partial SubscribableEdition Map(EditionSelectDto source);
    public override partial void Map(EditionSelectDto source, SubscribableEdition destination);
}

// EditionInfoDto inherits from EntityDto which has protected Id setter - use manual mapping
// Note: IsHighestEdition and IsFree are calculated properties not on SubscribableEdition
[Mapper]
public partial class SubscribableEditionToEditionInfoDtoMapper : MapperBase<SubscribableEdition, EditionInfoDto>
{
    public override EditionInfoDto Map(SubscribableEdition source)
    {
        return new EditionInfoDto
        {
            Id = source.Id,
            DisplayName = source.DisplayName,
            TrialDayCount = source.TrialDayCount,
            MonthlyPrice = source.MonthlyPrice,
            AnnualPrice = source.AnnualPrice,
            IsHighestEdition = false,
            IsFree = !source.MonthlyPrice.HasValue && !source.AnnualPrice.HasValue
        };
    }

    public override void Map(SubscribableEdition source, EditionInfoDto destination)
    {
        destination.Id = source.Id;
        destination.DisplayName = source.DisplayName;
        destination.TrialDayCount = source.TrialDayCount;
        destination.MonthlyPrice = source.MonthlyPrice;
        destination.AnnualPrice = source.AnnualPrice;
        destination.IsHighestEdition = false;
        destination.IsFree = !source.MonthlyPrice.HasValue && !source.AnnualPrice.HasValue;
    }
}

// EditionInfoDto inherits from EntityDto which has protected Id setter - use manual mapping
[Mapper]
public partial class EditionToEditionInfoDtoMapper : MapperBase<Edition, EditionInfoDto>
{
    public override EditionInfoDto Map(Edition source)
    {
        return new EditionInfoDto
        {
            Id = source.Id,
            DisplayName = source.DisplayName
        };
    }

    public override void Map(Edition source, EditionInfoDto destination)
    {
        destination.Id = source.Id;
        destination.DisplayName = source.DisplayName;
    }
}

[Mapper]
public partial class SubscribableEditionToEditionListDtoMapper : MapperBase<SubscribableEdition, EditionListDto>
{
    public override partial EditionListDto Map(SubscribableEdition source);
    public override partial void Map(SubscribableEdition source, EditionListDto destination);
}

[Mapper]
public partial class EditionToEditionEditDtoMapper : MapperBase<Edition, EditionEditDto>
{
    public override partial EditionEditDto Map(Edition source);
    public override partial void Map(Edition source, EditionEditDto destination);
}

[Mapper]
public partial class EditionToSubscribableEditionMapper : MapperBase<Edition, SubscribableEdition>
{
    public override partial SubscribableEdition Map(Edition source);
    public override partial void Map(Edition source, SubscribableEdition destination);
}

[Mapper]
public partial class EditionToEditionSelectDtoMapper : MapperBase<Edition, EditionSelectDto>
{
    public override partial EditionSelectDto Map(Edition source);
    public override partial void Map(Edition source, EditionSelectDto destination);
}
