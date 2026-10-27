using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Abp.Auditing;
using HCMv3.Configuration;
using HCMv3.DataExporting.Excel.MiniExcel;
using HCMv3.Dto;
using HCMv3.Storage;
using MiniExcelLibs;

namespace HCMv3.Auditing;

public class ExpiredAndDeletedAuditLogBackupService : MiniExcelExcelExporterBase,
    IExpiredAndDeletedAuditLogBackupService
{
    private readonly bool _isBackupEnabled;

    private readonly IAppConfigurationAccessor _configurationAccessor;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public ExpiredAndDeletedAuditLogBackupService(
        ITempFileCacheManager tempFileCacheManager,
        IAppConfigurationAccessor configurationAccessor
    )
        : base(tempFileCacheManager)
    {
        _tempFileCacheManager = tempFileCacheManager;
        _configurationAccessor = configurationAccessor;
        _isBackupEnabled =
            _configurationAccessor.Configuration["App:AuditLog:AutoDeleteExpiredLogs:ExcelBackup:IsEnabled"] ==
            true.ToString();
    }

    public bool CanBackup() => _isBackupEnabled;

    public async Task Backup(List<AuditLog> auditLogs)
    {
        if (auditLogs.Count == 0)
        {
            return;
        }

        var items = new List<Dictionary<string, object>>();

        foreach (var auditLog in auditLogs)
        {
            items.Add(new Dictionary<string, object>()
            {
                {L("TenantId"), auditLog.TenantId},
                {L("UserId"), auditLog.UserId},
                {L("ServiceName"), auditLog.ServiceName},
                {L("MethodName"), auditLog.MethodName},
                {L("Parameters"), auditLog.Parameters},
                {L("ReturnValue"), auditLog.ReturnValue},
                {L("ExecutionTime"), auditLog.ExecutionTime},
                {L("ExecutionDuration"), auditLog.ExecutionDuration},
                {L("ClientIpAddress"), auditLog.ClientIpAddress},
                {L("ClientName"), auditLog.ClientName},
                {L("BrowserInfo"), auditLog.BrowserInfo},
                {L("Exception"), auditLog.Exception},
                {L("ExceptionMessage"), auditLog.ExceptionMessage},
                {L("ImpersonatorUserId"), auditLog.ImpersonatorUserId},
                {L("ImpersonatorTenantId"), auditLog.ImpersonatorTenantId},
                {L("CustomData"), auditLog.CustomData},
            });
        }

        await CreateExcelPackageAsync(
            "AuditLogBackup_" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH.mm.ss.FFFZ") + ".xlsx",
            items
        );
    }

    protected override async Task SaveAsync(List<Dictionary<string, object>> items, FileDto file)
    {
        var backupFilePath =
            _configurationAccessor.Configuration["App:AuditLog:AutoDeleteExpiredLogs:ExcelBackup:FilePath"];
        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            return;
        }

        if (!Directory.Exists(backupFilePath))
        {
            Directory.CreateDirectory(backupFilePath);
        }

        using (var stream = new MemoryStream())
        {
            await stream.SaveAsAsync(items);
            _tempFileCacheManager.SetFile(file.FileToken, stream.ToArray());

            using (var excelFile = new FileStream(
                       Path.Combine(backupFilePath, file.FileName),
                       FileMode.Create,
                       FileAccess.Write)
                  )
            {
                var fileContent = _tempFileCacheManager.GetFile(file.FileToken);
                excelFile.Write(fileContent);
            }
        }
    }
}