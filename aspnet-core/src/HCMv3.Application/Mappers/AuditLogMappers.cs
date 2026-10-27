using Abp.Auditing;
using Abp.Mapperly;
using HCMv3.Auditing.Dto;
using Riok.Mapperly.Abstractions;

namespace HCMv3.Mappers;

[Mapper]
public partial class AuditLogToAuditLogListDtoMapper : MapperBase<AuditLog, AuditLogListDto>
{
    public override partial AuditLogListDto Map(AuditLog source);
    public override partial void Map(AuditLog source, AuditLogListDto destination);
}
