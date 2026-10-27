using HCMv3.Auditing.Dto;
using HCMv3.Dto;
using HCMv3.EntityChanges.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.Auditing.Exporting;

public interface IAuditLogListExcelExporter
{
    Task<FileDto> ExportToFile(List<AuditLogListDto> auditLogListDtos);

    Task<FileDto> ExportToFile(List<EntityChangeListDto> entityChangeListDtos);
}
