using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Timing;
using Abp.UI;
using HCMv3.SystemSettings.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Storage;

/// <summary>
/// Stores, lists and removes files attached to any record. Knows nothing about who may do so -- that is
/// decided per owning entity type in the application layer, before this is called.
/// </summary>
public interface IAttachmentManager : IDomainService
{
    /// <summary>
    /// Validates and stores a file against the tenant's document settings (legacy
    /// <c>Attachment.PerformValidation</c>): allowed extensions, size per file, files per record, and the
    /// tenant's total storage.
    /// </summary>
    Task<AppAttachment> AddAsync(string entityType, string entityId, string fileName, byte[] content);

    Task<List<AppAttachment>> GetListAsync(string entityType, string entityId);

    Task<AppAttachment> GetOrNullAsync(int id);

    Task<byte[]> ReadContentAsync(AppAttachment attachment);

    Task DeleteAsync(AppAttachment attachment);

    /// <summary>Removes every file of one record; call it when the record itself is deleted.</summary>
    Task DeleteAllAsync(string entityType, string entityId);
}

/// <inheritdoc cref="IAttachmentManager"/>
public class AttachmentManager : HCMv3DomainServiceBase, IAttachmentManager
{
    private const long BytesPerMB = 1024L * 1024L;

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".tif"] = "image/tiff",
        [".tiff"] = "image/tiff",
        [".webp"] = "image/webp",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain",
        [".csv"] = "text/csv",
        [".zip"] = "application/zip"
    };

    private readonly IRepository<AppAttachment> _attachmentRepository;
    private readonly IFileStore _fileStore;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public AttachmentManager(
        IRepository<AppAttachment> attachmentRepository,
        IFileStore fileStore,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _attachmentRepository = attachmentRepository;
        _fileStore = fileStore;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    public async Task<AppAttachment> AddAsync(string entityType, string entityId, string fileName, byte[] content)
    {
        if (content == null || content.Length == 0)
        {
            throw new UserFriendlyException(L("AttachmentIsEmpty"));
        }

        // Only the last segment: a browser may send a full client path, and the name is later offered back
        // as a download file name.
        var safeName = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (safeName.Length == 0)
        {
            throw new UserFriendlyException(L("AttachmentIsEmpty"));
        }

        if (safeName.Length > AppAttachmentConsts.MaxFileNameLength)
        {
            safeName = safeName.Substring(safeName.Length - AppAttachmentConsts.MaxFileNameLength);
        }

        var extension = Path.GetExtension(safeName).ToLowerInvariant();
        var settings = await _sysSettingsAccessor.GetDocumentSettingsAsync();

        var allowed = ParseExtensions(settings?.AllowedFileExtensions);
        if (!allowed.Contains(extension))
        {
            throw new UserFriendlyException(L("AttachmentFileTypeNotAllowed", extension, string.Join(", ", allowed)));
        }

        var maxFileSizeMB = settings?.MaxFileSizePerAttachmentMB > 0
            ? settings.MaxFileSizePerAttachmentMB
            : AppAttachmentConsts.DefaultMaxFileSizeMB;
        if (content.LongLength > maxFileSizeMB * BytesPerMB)
        {
            throw new UserFriendlyException(L("AttachmentTooLarge", maxFileSizeMB));
        }

        var maxPerRecord = settings?.MaxAttachmentsPerTransaction ?? 0;
        if (maxPerRecord > 0)
        {
            var existing = await _attachmentRepository.CountAsync(a => a.EntityType == entityType && a.EntityId == entityId);
            if (existing >= maxPerRecord)
            {
                throw new UserFriendlyException(L("AttachmentLimitReached", maxPerRecord));
            }
        }

        var maxStorageMB = settings?.MaxStorageSizeMB ?? 0;
        if (maxStorageMB > 0)
        {
            var used = await _attachmentRepository.GetAll().SumAsync(a => (long?)a.Size) ?? 0;
            if (used + content.LongLength > maxStorageMB * BytesPerMB)
            {
                throw new UserFriendlyException(L("AttachmentStorageFull", maxStorageMB));
            }
        }

        var tenantId = CurrentUnitOfWork?.GetTenantId();

        // The name on disk is generated, never the uploaded one: a user-chosen name is not a safe path
        // segment, and two uploads of "scan.pdf" must not overwrite each other.
        var storagePath = string.Join("/",
            _fileStore.TenantSegment(tenantId),
            "attachments",
            SafeSegment(entityType),
            Clock.Now.ToString("yyyy-MM"),
            Guid.NewGuid().ToString("N") + extension);

        await _fileStore.WriteAsync(storagePath, content);

        var lastDocNo = await _attachmentRepository.GetAll().MaxAsync(a => (long?)a.DocNo) ?? 0;

        var attachment = new AppAttachment
        {
            TenantId = tenantId ?? 0,
            EntityType = entityType,
            EntityId = entityId,
            FileName = safeName,
            ContentType = ContentTypes.TryGetValue(extension, out var type) ? type : "application/octet-stream",
            Size = content.LongLength,
            DocNo = lastDocNo + 1,
            StoragePath = storagePath
        };

        try
        {
            attachment.Id = await _attachmentRepository.InsertAndGetIdAsync(attachment);
        }
        catch
        {
            // The row did not make it, so the file on disk would be an orphan nothing ever points at.
            await _fileStore.DeleteAsync(storagePath);
            throw;
        }

        return attachment;
    }

    public Task<List<AppAttachment>> GetListAsync(string entityType, string entityId) =>
        _attachmentRepository.GetAll()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderBy(a => a.Id)
            .ToListAsync();

    public Task<AppAttachment> GetOrNullAsync(int id) => _attachmentRepository.FirstOrDefaultAsync(id);

    public Task<byte[]> ReadContentAsync(AppAttachment attachment) => _fileStore.ReadOrNullAsync(attachment.StoragePath);

    public async Task DeleteAsync(AppAttachment attachment)
    {
        // Soft-deleted like every audited row, but the bytes go: keeping a scan of somebody's passport on
        // disk after it was removed is a liability, not an audit trail.
        await _attachmentRepository.DeleteAsync(attachment);
        await _fileStore.DeleteAsync(attachment.StoragePath);
    }

    public async Task DeleteAllAsync(string entityType, string entityId)
    {
        foreach (var attachment in await GetListAsync(entityType, entityId))
        {
            await DeleteAsync(attachment);
        }
    }

    private static List<string> ParseExtensions(string configured)
    {
        var source = string.IsNullOrWhiteSpace(configured) ? AppAttachmentConsts.DefaultAllowedExtensions : configured;

        return source.Split(',', ';', ' ')
            .Select(e => e.Trim().ToLowerInvariant())
            .Where(e => e.Length > 0)
            .Select(e => e.StartsWith('.') ? e : "." + e)
            .Distinct()
            .ToList();
    }

    private static string SafeSegment(string value)
    {
        var cleaned = new string((value ?? "unknown").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());

        return cleaned.Length == 0 ? "unknown" : cleaned;
    }
}
