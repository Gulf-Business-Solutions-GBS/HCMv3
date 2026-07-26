using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSettings.Dto;

public class DocumentSettingsListDto : EntityDto
{
    public bool EnableDocSaveInFileSystem { get; set; }

    public string FileSystemAttachmentPath { get; set; }

    public int MaxAttachmentsPerTransaction { get; set; }

    public string AllowedFileExtensions { get; set; }

    public int MaxFileSizePerAttachmentMB { get; set; }

    public int MaxStorageSizeMB { get; set; }

    public int StorageWarningThresholdMB { get; set; }
}

public class DocumentSettingsEditDto : EntityDto
{
    public bool EnableDocSaveInFileSystem { get; set; }

    public string FileSystemAttachmentPath { get; set; }

    public int MaxAttachmentsPerTransaction { get; set; }

    [StringLength(200)]
    public string AllowedFileExtensions { get; set; }

    public int MaxFileSizePerAttachmentMB { get; set; }

    public int MaxStorageSizeMB { get; set; }

    public int StorageWarningThresholdMB { get; set; }
}
