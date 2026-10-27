using Abp.Localization;
using Abp.Mapperly;
using HCMv3.Localization.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class ApplicationLanguageToApplicationLanguageEditDtoMapper : MapperBase<ApplicationLanguage, ApplicationLanguageEditDto>
{
    public override ApplicationLanguageEditDto Map(ApplicationLanguage source)
    {
        var dto = MapInternal(source);
        dto.IsEnabled = !source.IsDisabled;
        return dto;
    }

    public override void Map(ApplicationLanguage source, ApplicationLanguageEditDto destination)
    {
        MapInternal(source, destination);
        destination.IsEnabled = !source.IsDisabled;
    }

    [MapperIgnoreTarget(nameof(ApplicationLanguageEditDto.IsEnabled))]
    private partial ApplicationLanguageEditDto MapInternal(ApplicationLanguage source);

    [MapperIgnoreTarget(nameof(ApplicationLanguageEditDto.IsEnabled))]
    private partial void MapInternal(ApplicationLanguage source, ApplicationLanguageEditDto destination);
}

[Mapper]
public partial class ApplicationLanguageToApplicationLanguageListDtoMapper : MapperBase<ApplicationLanguage, ApplicationLanguageListDto>
{
    public override partial ApplicationLanguageListDto Map(ApplicationLanguage source);
    public override partial void Map(ApplicationLanguage source, ApplicationLanguageListDto destination);
}
