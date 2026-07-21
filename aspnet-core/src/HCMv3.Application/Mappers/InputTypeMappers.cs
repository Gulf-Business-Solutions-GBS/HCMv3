using Abp.Localization;
using Abp.Mapperly;
using Abp.UI.Inputs;
using HCMv3.Editions.Dto;
using Riok.Mapperly.Abstractions;
using System.Collections.ObjectModel;

namespace HCMv3.Mappers;

[Mapper]
public partial class CheckboxInputTypeToFeatureInputTypeDtoMapper : MapperBase<CheckboxInputType, FeatureInputTypeDto>
{
    public override partial FeatureInputTypeDto Map(CheckboxInputType source);
    public override partial void Map(CheckboxInputType source, FeatureInputTypeDto destination);
}

[Mapper]
public partial class SingleLineStringInputTypeToFeatureInputTypeDtoMapper : MapperBase<SingleLineStringInputType, FeatureInputTypeDto>
{
    public override partial FeatureInputTypeDto Map(SingleLineStringInputType source);
    public override partial void Map(SingleLineStringInputType source, FeatureInputTypeDto destination);
}

[Mapper]
public partial class ComboboxInputTypeToFeatureInputTypeDtoMapper : MapperBase<ComboboxInputType, FeatureInputTypeDto>
{
    public override partial FeatureInputTypeDto Map(ComboboxInputType source);
    public override partial void Map(ComboboxInputType source, FeatureInputTypeDto destination);
}

[Mapper]
public partial class StaticLocalizableComboboxItemSourceToLocalizableComboboxItemSourceDtoMapper : MapperBase<StaticLocalizableComboboxItemSource, LocalizableComboboxItemSourceDto>
{
    private readonly ILocalizationContext _localizationContext;

    public StaticLocalizableComboboxItemSourceToLocalizableComboboxItemSourceDtoMapper(ILocalizationContext localizationContext)
    {
        _localizationContext = localizationContext;
    }

    public override LocalizableComboboxItemSourceDto Map(StaticLocalizableComboboxItemSource source)
    {
        if (source == null)
            return null;

        var dto = new LocalizableComboboxItemSourceDto
        {
            Items = new Collection<LocalizableComboboxItemDto>()
        };

        if (source.Items != null)
        {
            foreach (var item in source.Items)
            {
                if (item is LocalizableComboboxItem localizableItem)
                {
                    dto.Items.Add(new LocalizableComboboxItemDto
                    {
                        Value = localizableItem.Value,
                        DisplayText = localizableItem.DisplayText?.Localize(_localizationContext)
                    });
                }
            }
        }

        return dto;
    }

    public override void Map(StaticLocalizableComboboxItemSource source, LocalizableComboboxItemSourceDto destination)
    {
        if (source == null || destination == null)
            return;

        destination.Items = new Collection<LocalizableComboboxItemDto>();

        if (source.Items != null)
        {
            foreach (var item in source.Items)
            {
                if (item is LocalizableComboboxItem localizableItem)
                {
                    destination.Items.Add(new LocalizableComboboxItemDto
                    {
                        Value = localizableItem.Value,
                        DisplayText = localizableItem.DisplayText?.Localize(_localizationContext)
                    });
                }
            }
        }
    }
}

[Mapper]
public partial class LocalizableComboboxItemToLocalizableComboboxItemDtoMapper : MapperBase<LocalizableComboboxItem, LocalizableComboboxItemDto>
{
    private readonly ILocalizationContext _localizationContext;

    public LocalizableComboboxItemToLocalizableComboboxItemDtoMapper(ILocalizationContext localizationContext)
    {
        _localizationContext = localizationContext;
    }

    public override LocalizableComboboxItemDto Map(LocalizableComboboxItem source)
    {
        if (source == null)
            return null;

        return new LocalizableComboboxItemDto
        {
            Value = source.Value,
            DisplayText = source.DisplayText?.Localize(_localizationContext)
        };
    }

    public override void Map(LocalizableComboboxItem source, LocalizableComboboxItemDto destination)
    {
        if (source == null || destination == null)
            return;

        destination.Value = source.Value;
        destination.DisplayText = source.DisplayText?.Localize(_localizationContext);
    }
}
