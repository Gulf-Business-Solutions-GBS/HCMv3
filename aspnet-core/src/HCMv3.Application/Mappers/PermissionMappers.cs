using Abp.Authorization;
using Abp.Localization;
using Abp.Mapperly;
using HCMv3.Authorization.Permissions.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

// Permission has ILocalizableString properties that need manual mapping
[Mapper]
public partial class PermissionToFlatPermissionDtoMapper : MapperBase<Permission, FlatPermissionDto>
{
    private readonly ILocalizationContext _localizationContext;

    public PermissionToFlatPermissionDtoMapper(ILocalizationContext localizationContext)
    {
        _localizationContext = localizationContext;
    }

    public override FlatPermissionDto Map(Permission source)
    {
        return new FlatPermissionDto
        {
            ParentName = source.Parent?.Name,
            Name = source.Name,
            DisplayName = source.DisplayName?.Localize(_localizationContext),
            Description = source.Description?.Localize(_localizationContext),
            IsGrantedByDefault = false
        };
    }

    public override void Map(Permission source, FlatPermissionDto destination)
    {
        destination.ParentName = source.Parent?.Name;
        destination.Name = source.Name;
        destination.DisplayName = source.DisplayName?.Localize(_localizationContext);
        destination.Description = source.Description?.Localize(_localizationContext);
        destination.IsGrantedByDefault = false;
    }
}

[Mapper]
public partial class PermissionToFlatPermissionWithLevelDtoMapper : MapperBase<Permission, FlatPermissionWithLevelDto>
{
    private readonly ILocalizationContext _localizationContext;

    public PermissionToFlatPermissionWithLevelDtoMapper(ILocalizationContext localizationContext)
    {
        _localizationContext = localizationContext;
    }

    public override FlatPermissionWithLevelDto Map(Permission source)
    {
        return new FlatPermissionWithLevelDto
        {
            ParentName = source.Parent?.Name,
            Name = source.Name,
            DisplayName = source.DisplayName?.Localize(_localizationContext),
            Description = source.Description?.Localize(_localizationContext),
            IsGrantedByDefault = false
        };
    }

    public override void Map(Permission source, FlatPermissionWithLevelDto destination)
    {
        destination.ParentName = source.Parent?.Name;
        destination.Name = source.Name;
        destination.DisplayName = source.DisplayName?.Localize(_localizationContext);
        destination.Description = source.Description?.Localize(_localizationContext);
        destination.IsGrantedByDefault = false;
    }
}

// FlatPermissionWithLevelDto inherits from FlatPermissionDto
[Mapper]
public partial class FlatPermissionWithLevelDtoToFlatPermissionDtoMapper : MapperBase<FlatPermissionWithLevelDto, FlatPermissionDto>
{
    public override partial FlatPermissionDto Map(FlatPermissionWithLevelDto source);
    public override partial void Map(FlatPermissionWithLevelDto source, FlatPermissionDto destination);
}
