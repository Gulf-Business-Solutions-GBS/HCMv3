using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Tenant-wide document/attachment storage configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageDocumentSettings</c>). Legacy
/// <c>MaxFileSizePerAttachment</c>/<c>MaxStorageSize</c>/<c>WarningThresholdSize</c> stored the raw byte
/// count AES-encrypted at rest with a hardcoded key purely as DB-tamper obfuscation (pure legacy XAF/XPO
/// storage plumbing, not a real business rule) -- ported here simply as the plain MB values the legacy UI
/// actually exposed (<c>MaxFileSizePerAttachmentMB</c>/<c>MaxStorageSizeMB</c>/<c>StorageWarningThresholdMB</c>).
/// Legacy <c>OnDeleting</c> unconditionally throws -- enforced in
/// <c>ManageDocumentSettingsAppService.DeleteDocumentSettings</c>, not here.
/// </summary>
public class ManageDocumentSettings : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual bool EnableDocSaveInFileSystem { get; set; }

    public virtual string FileSystemAttachmentPath { get; set; }

    public virtual int MaxAttachmentsPerTransaction { get; set; }

    [StringLength(200)]
    public virtual string AllowedFileExtensions { get; set; }

    public virtual int MaxFileSizePerAttachmentMB { get; set; }

    public virtual int MaxStorageSizeMB { get; set; }

    public virtual int StorageWarningThresholdMB { get; set; }
}
