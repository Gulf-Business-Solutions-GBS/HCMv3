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

    /// <summary>
    /// Legacy free text, kept with its old value and NOT read. A path typed on a settings screen is a path
    /// the service account then writes to, so where files go is named in configuration instead; see
    /// <see cref="FileStorageRootName"/> and <c>IFileStore</c>.
    /// </summary>
    public virtual string FileSystemAttachmentPath { get; set; }

    /// <summary>
    /// Which of the roots configuration approves this tenant's new files go to, by name -- never a path.
    /// Blank means the default root. A name no longer configured falls back to the default, with a warning
    /// in the log (<c>IFileStorageRootResolver</c>).
    /// </summary>
    [StringLength(64)]
    public virtual string FileStorageRootName { get; set; }

    public virtual int MaxAttachmentsPerTransaction { get; set; }

    [StringLength(200)]
    public virtual string AllowedFileExtensions { get; set; }

    public virtual int MaxFileSizePerAttachmentMB { get; set; }

    public virtual int MaxStorageSizeMB { get; set; }

    public virtual int StorageWarningThresholdMB { get; set; }
}
