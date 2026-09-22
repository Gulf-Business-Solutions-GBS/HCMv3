using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Dependency;
using Abp.UI;
using HCMv3.Dto;
using HCMv3.Storage.Dto;
using HCMv3.SystemSettings.Services;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.Storage;

/// <inheritdoc cref="IAttachmentAppService"/>
[AbpAuthorize]
public class AttachmentAppService : HCMv3AppServiceBase, IAttachmentAppService
{
    private readonly IAttachmentManager _attachmentManager;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IIocResolver _iocResolver;

    public AttachmentAppService(
        IAttachmentManager attachmentManager,
        ITempFileCacheManager tempFileCacheManager,
        ISysSettingsAccessor sysSettingsAccessor,
        IIocResolver iocResolver)
    {
        _attachmentManager = attachmentManager;
        _tempFileCacheManager = tempFileCacheManager;
        _sysSettingsAccessor = sysSettingsAccessor;
        _iocResolver = iocResolver;
    }

    public async Task<AttachmentSettingsDto> GetAttachmentSettings()
    {
        var settings = await _sysSettingsAccessor.GetDocumentSettingsAsync();
        var configured = string.IsNullOrWhiteSpace(settings?.AllowedFileExtensions)
            ? AppAttachmentConsts.DefaultAllowedExtensions
            : settings.AllowedFileExtensions;

        return new AttachmentSettingsDto
        {
            AllowedExtensions = configured.Split(',', ';', ' ')
                .Select(e => e.Trim().ToLowerInvariant())
                .Where(e => e.Length > 0)
                .Select(e => e.StartsWith('.') ? e : "." + e)
                .Distinct()
                .ToList(),
            MaxFileSizeMB = settings?.MaxFileSizePerAttachmentMB > 0
                ? settings.MaxFileSizePerAttachmentMB
                : AppAttachmentConsts.DefaultMaxFileSizeMB,
            MaxAttachmentsPerRecord = settings?.MaxAttachmentsPerTransaction ?? 0
        };
    }

    [HttpPost]
    public async Task<GetAttachmentsOutput> GetAttachments(AttachmentOwnerInput input)
    {
        var policy = PolicyFor(input.EntityType);
        if (!await policy.CanViewAsync(input.EntityId))
        {
            throw new AbpAuthorizationException(L("AttachmentAccessDenied"));
        }

        var canEdit = await policy.CanEditAsync(input.EntityId);
        var items = await _attachmentManager.GetListAsync(input.EntityType, input.EntityId);

        return new GetAttachmentsOutput
        {
            CanEdit = canEdit,
            Items = items.Select(a => ToDto(a, canEdit)).ToList()
        };
    }

    public async Task<AttachmentDto> AddAttachment(AddAttachmentInput input)
    {
        if (!await PolicyFor(input.EntityType).CanEditAsync(input.EntityId))
        {
            throw new AbpAuthorizationException(L("AttachmentAccessDenied"));
        }

        var file = _tempFileCacheManager.GetFileInfo(input.FileToken);
        if (file?.File == null)
        {
            // Temp files expire; an upload left sitting in a form for too long lands here.
            throw new UserFriendlyException(L("AttachmentUploadExpired"));
        }

        var attachment = await _attachmentManager.AddAsync(input.EntityType, input.EntityId, file.FileName, file.File);

        return ToDto(attachment, canDelete: true);
    }

    public async Task DeleteAttachment(EntityDto input)
    {
        var attachment = await LoadAsync(input.Id);
        if (!await PolicyFor(attachment.EntityType).CanEditAsync(attachment.EntityId))
        {
            throw new AbpAuthorizationException(L("AttachmentAccessDenied"));
        }

        await _attachmentManager.DeleteAsync(attachment);
    }

    public async Task<FileDto> GetAttachmentDownload(EntityDto input)
    {
        var attachment = await LoadAsync(input.Id);
        if (!await PolicyFor(attachment.EntityType).CanViewAsync(attachment.EntityId))
        {
            throw new AbpAuthorizationException(L("AttachmentAccessDenied"));
        }

        var content = await _attachmentManager.ReadContentAsync(attachment);
        if (content == null)
        {
            throw new UserFriendlyException(L("AttachmentFileMissing"));
        }

        var file = new FileDto(attachment.FileName, attachment.ContentType);
        _tempFileCacheManager.SetFile(file.FileToken, content);

        return file;
    }

    private async Task<AppAttachment> LoadAsync(int id)
    {
        var attachment = await _attachmentManager.GetOrNullAsync(id);

        return attachment ?? throw new UserFriendlyException(L("AttachmentFileMissing"));
    }

    private IAttachmentOwnerPolicy PolicyFor(string entityType)
    {
        var policies = _iocResolver.ResolveAll<IAttachmentOwnerPolicy>();

        return policies.FirstOrDefault(p => p.EntityType == entityType)
               ?? throw new AbpAuthorizationException(L("AttachmentAccessDenied"));
    }

    private static AttachmentDto ToDto(AppAttachment attachment, bool canDelete) => new()
    {
        Id = attachment.Id,
        FileName = attachment.FileName,
        ContentType = attachment.ContentType,
        Size = attachment.Size,
        DocNo = attachment.DocNo,
        CreationTime = attachment.CreationTime,
        CanDelete = canDelete
    };
}
