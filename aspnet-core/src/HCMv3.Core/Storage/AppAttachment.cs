using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Storage;

/// <summary>
/// A file attached to any record -- the port of legacy <c>DocAttachment</c>/<c>Attachment</c>/
/// <c>FileSysAttachment</c>.
///
/// <para>
/// Legacy put a <c>DocAttachment</c> foreign key on each of the 22 entities that could carry files, so
/// making a new entity attachable meant a new column and a migration. Here the owner is named by
/// <see cref="EntityType"/> + <see cref="EntityId"/> instead: any record can carry files without its own
/// table changing. The trade-off is that the database cannot enforce the link, so deleting an owner must
/// remove its attachments explicitly (<c>IAttachmentManager.DeleteAllAsync</c>).
/// </para>
///
/// <para>
/// Only metadata is stored here. The bytes are on disk at <see cref="StoragePath"/>, relative to the file
/// store root (<see cref="IFileStore"/>).
/// </para>
/// </summary>
[Table("AppAttachments")]
public class AppAttachment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>The owning entity's type name, e.g. <c>EmployeeDocument</c>.</summary>
    [Required]
    [StringLength(AppAttachmentConsts.MaxEntityTypeLength)]
    public virtual string EntityType { get; set; }

    /// <summary>The owning record's id, as text so int, long and Guid keys all fit.</summary>
    [Required]
    [StringLength(AppAttachmentConsts.MaxEntityIdLength)]
    public virtual string EntityId { get; set; }

    /// <summary>The name the file was uploaded with, used when it is downloaded again.</summary>
    [Required]
    [StringLength(AppAttachmentConsts.MaxFileNameLength)]
    public virtual string FileName { get; set; }

    [StringLength(AppAttachmentConsts.MaxContentTypeLength)]
    public virtual string ContentType { get; set; }

    /// <summary>Size in bytes; summed for the tenant's storage limit.</summary>
    public virtual long Size { get; set; }

    /// <summary>Running document number across the tenant (legacy <c>Attachment.DocNo</c>).</summary>
    public virtual long DocNo { get; set; }

    /// <summary>Where the bytes are, relative to the file store root. Never a user-supplied name.</summary>
    [Required]
    [StringLength(AppAttachmentConsts.MaxStoragePathLength)]
    public virtual string StoragePath { get; set; }
}
