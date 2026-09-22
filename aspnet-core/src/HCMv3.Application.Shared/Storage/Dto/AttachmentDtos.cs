using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace HCMv3.Storage.Dto;

public class AttachmentDto : EntityDto
{
    public string FileName { get; set; }

    public string ContentType { get; set; }

    public long Size { get; set; }

    public long DocNo { get; set; }

    public DateTime CreationTime { get; set; }

    /// <summary>Whether the caller may delete this file; drives the delete button.</summary>
    public bool CanDelete { get; set; }
}

/// <summary>Names the record whose files are wanted.</summary>
public class AttachmentOwnerInput
{
    [Required]
    [StringLength(AppAttachmentConsts.MaxEntityTypeLength)]
    public string EntityType { get; set; }

    [Required]
    [StringLength(AppAttachmentConsts.MaxEntityIdLength)]
    public string EntityId { get; set; }
}

public class GetAttachmentsOutput
{
    public List<AttachmentDto> Items { get; set; } = new();

    /// <summary>Whether the caller may add or remove files on this record right now.</summary>
    public bool CanEdit { get; set; }
}

/// <summary>Attaches a file already uploaded to the temp cache (<c>Attachment/UploadFile</c>).</summary>
public class AddAttachmentInput : AttachmentOwnerInput
{
    [Required]
    [StringLength(64)]
    public string FileToken { get; set; }
}

/// <summary>The tenant's limits, for the upload control to check before sending anything.</summary>
public class AttachmentSettingsDto
{
    public List<string> AllowedExtensions { get; set; } = new();

    public int MaxFileSizeMB { get; set; }

    /// <summary>Zero means no limit.</summary>
    public int MaxAttachmentsPerRecord { get; set; }
}
