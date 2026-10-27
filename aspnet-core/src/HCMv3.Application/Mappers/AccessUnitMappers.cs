using Abp.Mapperly;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class AccessUnitToAccessUnitDtoMapper : MapperBase<AccessUnit, AccessUnitDto>
{
    public override partial AccessUnitDto Map(AccessUnit source);
    public override partial void Map(AccessUnit source, AccessUnitDto destination);
}

[Mapper]
public partial class AccessUnitToCreateOrEditAccessUnitDtoMapper : MapperBase<AccessUnit, CreateOrEditAccessUnitDto>
{
    public override partial CreateOrEditAccessUnitDto Map(AccessUnit source);
    public override partial void Map(AccessUnit source, CreateOrEditAccessUnitDto destination);
}

/// <summary>
/// Maps the create/edit input onto an <see cref="AccessUnit"/> entity.
/// Identity, tenant and audit columns are owned by the framework / app service, so they are ignored here.
/// </summary>
[Mapper]
public partial class CreateOrEditAccessUnitDtoToAccessUnitMapper : MapperBase<CreateOrEditAccessUnitDto, AccessUnit>
{
    [MapperIgnoreTarget(nameof(AccessUnit.Id))]
    [MapperIgnoreTarget(nameof(AccessUnit.TenantId))]
    [MapperIgnoreTarget(nameof(AccessUnit.CreatorUserId))]
    [MapperIgnoreTarget(nameof(AccessUnit.CreationTime))]
    [MapperIgnoreTarget(nameof(AccessUnit.LastModifierUserId))]
    [MapperIgnoreTarget(nameof(AccessUnit.LastModificationTime))]
    [MapperIgnoreTarget(nameof(AccessUnit.IsDeleted))]
    [MapperIgnoreTarget(nameof(AccessUnit.DeleterUserId))]
    [MapperIgnoreTarget(nameof(AccessUnit.DeletionTime))]
    public override partial AccessUnit Map(CreateOrEditAccessUnitDto source);

    [MapperIgnoreTarget(nameof(AccessUnit.Id))]
    [MapperIgnoreTarget(nameof(AccessUnit.TenantId))]
    [MapperIgnoreTarget(nameof(AccessUnit.CreatorUserId))]
    [MapperIgnoreTarget(nameof(AccessUnit.CreationTime))]
    [MapperIgnoreTarget(nameof(AccessUnit.LastModifierUserId))]
    [MapperIgnoreTarget(nameof(AccessUnit.LastModificationTime))]
    [MapperIgnoreTarget(nameof(AccessUnit.IsDeleted))]
    [MapperIgnoreTarget(nameof(AccessUnit.DeleterUserId))]
    [MapperIgnoreTarget(nameof(AccessUnit.DeletionTime))]
    public override partial void Map(CreateOrEditAccessUnitDto source, AccessUnit destination);
}
