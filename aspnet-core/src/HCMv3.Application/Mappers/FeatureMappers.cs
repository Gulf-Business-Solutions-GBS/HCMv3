using Abp.Application.Features;
using Abp.Localization;
using Abp.Mapperly;
using Abp.UI.Inputs;
using HCMv3.Editions.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

// Feature has ILocalizableString properties that need manual mapping
[Mapper]
public partial class FeatureToFlatFeatureSelectDtoMapper : MapperBase<Feature, FlatFeatureSelectDto>
{
    private readonly ILocalizationContext _localizationContext;

    public FeatureToFlatFeatureSelectDtoMapper(ILocalizationContext localizationContext)
    {
        _localizationContext = localizationContext;
    }

    public override FlatFeatureSelectDto Map(Feature source)
    {
        return new FlatFeatureSelectDto
        {
            ParentName = source.Parent?.Name,
            Name = source.Name,
            DisplayName = source.DisplayName?.Localize(_localizationContext),
            Description = source.Description?.Localize(_localizationContext),
            DefaultValue = source.DefaultValue,
            InputType = source.InputType
        };
    }

    public override void Map(Feature source, FlatFeatureSelectDto destination)
    {
        destination.ParentName = source.Parent?.Name;
        destination.Name = source.Name;
        destination.DisplayName = source.DisplayName?.Localize(_localizationContext);
        destination.Description = source.Description?.Localize(_localizationContext);
        destination.DefaultValue = source.DefaultValue;
        destination.InputType = source.InputType;
    }
}

[Mapper]
public partial class FeatureToFlatFeatureDtoMapper : MapperBase<Feature, FlatFeatureDto>
{
    private readonly ILocalizationContext _localizationContext;

    public FeatureToFlatFeatureDtoMapper(ILocalizationContext localizationContext)
    {
        _localizationContext = localizationContext;
    }

    public override FlatFeatureDto Map(Feature source)
    {
        return new FlatFeatureDto
        {
            ParentName = source.Parent?.Name,
            Name = source.Name,
            DisplayName = source.DisplayName?.Localize(_localizationContext),
            Description = source.Description?.Localize(_localizationContext),
            DefaultValue = source.DefaultValue,
            InputType = MapToFeatureInputTypeDto(source.InputType)
        };
    }

    public override void Map(Feature source, FlatFeatureDto destination)
    {
        destination.ParentName = source.Parent?.Name;
        destination.Name = source.Name;
        destination.DisplayName = source.DisplayName?.Localize(_localizationContext);
        destination.Description = source.Description?.Localize(_localizationContext);
        destination.DefaultValue = source.DefaultValue;
        destination.InputType = MapToFeatureInputTypeDto(source.InputType);
    }

    private FeatureInputTypeDto MapToFeatureInputTypeDto(IInputType source)
    {
        return source switch
        {
            CheckboxInputType checkbox => MapCheckboxInputType(checkbox),
            SingleLineStringInputType singleLine => MapSingleLineStringInputType(singleLine),
            ComboboxInputType combobox => MapComboboxInputType(combobox),
            _ => new FeatureInputTypeDto
            {
                Name = source.Name,
                Attributes = source.Attributes,
                Validator = source.Validator
            }
        };
    }

    private partial FeatureInputTypeDto MapCheckboxInputType(CheckboxInputType source);
    private partial FeatureInputTypeDto MapSingleLineStringInputType(SingleLineStringInputType source);
    private partial FeatureInputTypeDto MapComboboxInputType(ComboboxInputType source);
}
