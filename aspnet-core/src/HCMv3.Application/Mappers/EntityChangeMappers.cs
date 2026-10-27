using Abp.EntityHistory;
using Abp.Mapperly;
using HCMv3.EntityChanges;
using HCMv3.EntityChanges.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class EntityChangeToEntityChangeListDtoMapper : MapperBase<EntityChange, EntityChangeListDto>
{
    public override partial EntityChangeListDto Map(EntityChange source);
    public override partial void Map(EntityChange source, EntityChangeListDto destination);
}

[Mapper]
public partial class EntityChangeToEntityAndPropertyChangeListDtoMapper : MapperBase<EntityChange, EntityAndPropertyChangeListDto>
{
    public override partial EntityAndPropertyChangeListDto Map(EntityChange source);
    public override partial void Map(EntityChange source, EntityAndPropertyChangeListDto destination);
}

[Mapper]
public partial class EntityPropertyChangeToEntityPropertyChangeDtoMapper : MapperBase<EntityPropertyChange, EntityPropertyChangeDto>
{
    public override partial EntityPropertyChangeDto Map(EntityPropertyChange source);
    public override partial void Map(EntityPropertyChange source, EntityPropertyChangeDto destination);
}

[Mapper]
public partial class EntityChangePropertyAndUserToEntityChangeListDtoMapper : MapperBase<EntityChangePropertyAndUser, EntityChangeListDto>
{
    public override partial EntityChangeListDto Map(EntityChangePropertyAndUser source);
    public override partial void Map(EntityChangePropertyAndUser source, EntityChangeListDto destination);
}
