using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Auditing.Dto;
using HCMv3.Dto;
using HCMv3.EntityChanges.Dto;

namespace HCMv3.Auditing;

public interface IAuditLogAppService : IApplicationService
{
    Task<PagedResultDto<AuditLogListDto>> GetAuditLogs(GetAuditLogsInput input);

    Task<FileDto> GetAuditLogsToExcel(GetAuditLogsInput input);

    Task<PagedResultDto<EntityChangeListDto>> GetEntityChanges(GetEntityChangeInput input);

    Task<PagedResultDto<EntityChangeListDto>> GetEntityTypeChanges(GetEntityTypeChangeInput input);

    Task<FileDto> GetEntityChangesToExcel(GetEntityChangeInput input);

    Task<List<EntityPropertyChangeDto>> GetEntityPropertyChanges(long entityChangeId);

    List<NameValueDto> GetEntityHistoryObjectTypes();
}

