using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Auditing;

namespace HCMv3.Auditing;

public interface IExpiredAndDeletedAuditLogBackupService
{
    bool CanBackup();

    Task Backup(List<AuditLog> auditLogs);
}