using Abp.DynamicEntityProperties;
using Abp.Mapperly;
using HCMv3.DynamicEntityProperties.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class DynamicPropertyToDynamicPropertyDtoMapper : MapperBase<DynamicProperty, DynamicPropertyDto>
{
    public override partial DynamicPropertyDto Map(DynamicProperty source);
    public override partial void Map(DynamicProperty source, DynamicPropertyDto destination);
}

[Mapper]
public partial class DynamicPropertyDtoToDynamicPropertyMapper : MapperBase<DynamicPropertyDto, DynamicProperty>
{
    public override partial DynamicProperty Map(DynamicPropertyDto source);
    public override partial void Map(DynamicPropertyDto source, DynamicProperty destination);
}

[Mapper]
public partial class DynamicPropertyValueToDynamicPropertyValueDtoMapper : MapperBase<DynamicPropertyValue, DynamicPropertyValueDto>
{
    public override partial DynamicPropertyValueDto Map(DynamicPropertyValue source);
    public override partial void Map(DynamicPropertyValue source, DynamicPropertyValueDto destination);
}

[Mapper]
public partial class DynamicPropertyValueDtoToDynamicPropertyValueMapper : MapperBase<DynamicPropertyValueDto, DynamicPropertyValue>
{
    public override partial DynamicPropertyValue Map(DynamicPropertyValueDto source);
    public override partial void Map(DynamicPropertyValueDto source, DynamicPropertyValue destination);
}

[Mapper]
public partial class DynamicEntityPropertyToDynamicEntityPropertyDtoMapper : MapperBase<DynamicEntityProperty, DynamicEntityPropertyDto>
{
    public override DynamicEntityPropertyDto Map(DynamicEntityProperty source)
    {
        var dto = MapInternal(source);
        dto.DynamicPropertyName = string.IsNullOrEmpty(source.DynamicProperty?.DisplayName)
            ? source.DynamicProperty?.PropertyName
            : source.DynamicProperty?.DisplayName;
        return dto;
    }

    public override void Map(DynamicEntityProperty source, DynamicEntityPropertyDto destination)
    {
        MapInternal(source, destination);
        destination.DynamicPropertyName = string.IsNullOrEmpty(source.DynamicProperty?.DisplayName)
            ? source.DynamicProperty?.PropertyName
            : source.DynamicProperty?.DisplayName;
    }

    [MapperIgnoreTarget(nameof(DynamicEntityPropertyDto.DynamicPropertyName))]
    private partial DynamicEntityPropertyDto MapInternal(DynamicEntityProperty source);

    [MapperIgnoreTarget(nameof(DynamicEntityPropertyDto.DynamicPropertyName))]
    private partial void MapInternal(DynamicEntityProperty source, DynamicEntityPropertyDto destination);
}

[Mapper]
public partial class DynamicEntityPropertyDtoToDynamicEntityPropertyMapper : MapperBase<DynamicEntityPropertyDto, DynamicEntityProperty>
{
    public override partial DynamicEntityProperty Map(DynamicEntityPropertyDto source);
    public override partial void Map(DynamicEntityPropertyDto source, DynamicEntityProperty destination);
}

[Mapper]
public partial class DynamicEntityPropertyValueToDynamicEntityPropertyValueDtoMapper : MapperBase<DynamicEntityPropertyValue, DynamicEntityPropertyValueDto>
{
    public override partial DynamicEntityPropertyValueDto Map(DynamicEntityPropertyValue source);
    public override partial void Map(DynamicEntityPropertyValue source, DynamicEntityPropertyValueDto destination);
}

[Mapper]
public partial class DynamicEntityPropertyValueDtoToDynamicEntityPropertyValueMapper : MapperBase<DynamicEntityPropertyValueDto, DynamicEntityPropertyValue>
{
    public override partial DynamicEntityPropertyValue Map(DynamicEntityPropertyValueDto source);
    public override partial void Map(DynamicEntityPropertyValueDto source, DynamicEntityPropertyValue destination);
}
