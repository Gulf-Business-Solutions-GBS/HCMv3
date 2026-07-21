using Abp.Mapperly;
using Abp.Organizations;
using HCMv3.Organizations.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class OrganizationUnitToOrganizationUnitDtoMapper : MapperBase<OrganizationUnit, OrganizationUnitDto>
{
    public override partial OrganizationUnitDto Map(OrganizationUnit source);
    public override partial void Map(OrganizationUnit source, OrganizationUnitDto destination);
}
