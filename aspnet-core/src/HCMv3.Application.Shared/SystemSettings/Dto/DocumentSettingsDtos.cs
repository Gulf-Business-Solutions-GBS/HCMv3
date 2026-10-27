using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class DocumentSettingsListDto : EntityDto
{
    public bool EnableDocSaveInFileSystem { get; set; }

    /// <summary>Legacy free text, kept and not read. See <see cref="FileStorageRootName"/>.</summary>
    public string FileSystemAttachmentPath { get; set; }

    /// <summary>Which approved root new files go to. Blank means the default one.</summary>
    public string FileStorageRootName { get; set; }

    public int MaxAttachmentsPerTransaction { get; set; }

    public string AllowedFileExtensions { get; set; }

    public int MaxFileSizePerAttachmentMB { get; set; }

    public int MaxStorageSizeMB { get; set; }

    public int StorageWarningThresholdMB { get; set; }
}

public class DocumentSettingsEditDto : EntityDto
{
    public bool EnableDocSaveInFileSystem { get; set; }

    /// <summary>Legacy free text, kept and not read. See <see cref="FileStorageRootName"/>.</summary>
    public string FileSystemAttachmentPath { get; set; }

    /// <summary>
    /// The NAME of one of the roots configuration approves (<see cref="FileStorageRootDto"/>), never a
    /// path. Anything else is refused on save, so this screen cannot widen where the server writes.
    /// </summary>
    [StringLength(64)]
    public string FileStorageRootName { get; set; }

    /// <summary>Read-only: what the screen offers in the picker. Ignored on save.</summary>
    public List<FileStorageRootDto> AvailableRoots { get; set; } = new();

    public int MaxAttachmentsPerTransaction { get; set; }

    [StringLength(200)]
    public string AllowedFileExtensions { get; set; }

    public int MaxFileSizePerAttachmentMB { get; set; }

    public int MaxStorageSizeMB { get; set; }

    public int StorageWarningThresholdMB { get; set; }
}

/// <summary>
/// One folder the SERVER administrator has approved in <c>appsettings.json</c>, offered to the settings
/// screen as a choice. Nobody types a path: see <c>IFileStore</c> for why a free-text path on a screen is a
/// way to make this process write anywhere it can reach.
/// </summary>
public class FileStorageRootDto
{
    public string Name { get; set; }

    /// <summary>
    /// Where it is on the server. Filled in for a host user only -- a tenant administrator choosing a disk
    /// has no need of the server's folder layout, and telling them is free reconnaissance.
    /// </summary>
    public string Path { get; set; }

    public bool IsDefault { get; set; }
}
