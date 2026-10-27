using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Storage;
using HCMv3.SystemSettings.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSettings;

/// <summary>
/// Application service for <see cref="ManageDocumentSettings"/>. Legacy <c>OnDeleting</c> unconditionally
/// throws ("You can't delete record. Please contact administrator."), so <see cref="DeleteDocumentSettings"/>
/// always throws here too. Legacy <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_DocumentSettings)]
public class ManageDocumentSettingsAppService : HCMv3AppServiceBase, IManageDocumentSettingsAppService
{
    private readonly IRepository<ManageDocumentSettings> _repository;
    private readonly IFileStore _fileStore;

    public ManageDocumentSettingsAppService(IRepository<ManageDocumentSettings> repository, IFileStore fileStore)
    {
        _repository = repository;
        _fileStore = fileStore;
    }

    public async Task<PagedResultDto<DocumentSettingsListDto>> GetDocumentSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<DocumentSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<DocumentSettingsEditDto> GetDocumentSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var dto = ToEditDto(entity);
        dto.AvailableRoots = AvailableRoots();

        return dto;
    }

    public Task<List<FileStorageRootDto>> GetFileStorageRoots() => Task.FromResult(AvailableRoots());

    /// <summary>
    /// The roots <c>appsettings.json</c> approves, as the screen's picker. The folder each one resolves to
    /// is told to a host user only: a tenant administrator picking a disk does not need the server's layout,
    /// and handing it over is reconnaissance for free.
    /// </summary>
    private List<FileStorageRootDto> AvailableRoots()
    {
        var isHost = AbpSession.TenantId == null;

        return _fileStore.Roots
            .Select(r => new FileStorageRootDto { Name = r.Name, Path = isHost ? r.Path : null, IsDefault = r.IsDefault })
            .ToList();
    }

    /// <summary>
    /// A chosen root must be one of the configured NAMES. This is the check that keeps the screen from
    /// widening where the server writes: without it the field would be a free-text path by another name.
    /// </summary>
    private string ValidatedRootName(string chosen)
    {
        var name = chosen?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (!_fileStore.Roots.Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UserFriendlyException(L("FileStorageRootNotConfigured", name));
        }

        return name;
    }

    public async Task CreateOrEditDocumentSettings(DocumentSettingsEditDto input)
    {
        if (input.Id != 0)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_DocumentSettings_Delete)]
    public Task DeleteDocumentSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_DocumentSettings_Create)]
    protected virtual async Task CreateAsync(DocumentSettingsEditDto input)
    {
        var entity = new ManageDocumentSettings
        {
            EnableDocSaveInFileSystem = input.EnableDocSaveInFileSystem,
            FileSystemAttachmentPath = input.FileSystemAttachmentPath,
            FileStorageRootName = ValidatedRootName(input.FileStorageRootName),
            MaxAttachmentsPerTransaction = input.MaxAttachmentsPerTransaction,
            AllowedFileExtensions = input.AllowedFileExtensions,
            MaxFileSizePerAttachmentMB = input.MaxFileSizePerAttachmentMB,
            MaxStorageSizeMB = input.MaxStorageSizeMB,
            StorageWarningThresholdMB = input.StorageWarningThresholdMB,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_DocumentSettings_Edit)]
    protected virtual async Task UpdateAsync(DocumentSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        entity.EnableDocSaveInFileSystem = input.EnableDocSaveInFileSystem;
        entity.FileSystemAttachmentPath = input.FileSystemAttachmentPath;
        entity.FileStorageRootName = ValidatedRootName(input.FileStorageRootName);
        entity.MaxAttachmentsPerTransaction = input.MaxAttachmentsPerTransaction;
        entity.AllowedFileExtensions = input.AllowedFileExtensions;
        entity.MaxFileSizePerAttachmentMB = input.MaxFileSizePerAttachmentMB;
        entity.MaxStorageSizeMB = input.MaxStorageSizeMB;
        entity.StorageWarningThresholdMB = input.StorageWarningThresholdMB;

        await _repository.UpdateAsync(entity);
    }

    private static DocumentSettingsListDto ToListDto(ManageDocumentSettings entity)
    {
        return new DocumentSettingsListDto
        {
            Id = entity.Id,
            EnableDocSaveInFileSystem = entity.EnableDocSaveInFileSystem,
            FileSystemAttachmentPath = entity.FileSystemAttachmentPath,
            FileStorageRootName = entity.FileStorageRootName,
            MaxAttachmentsPerTransaction = entity.MaxAttachmentsPerTransaction,
            AllowedFileExtensions = entity.AllowedFileExtensions,
            MaxFileSizePerAttachmentMB = entity.MaxFileSizePerAttachmentMB,
            MaxStorageSizeMB = entity.MaxStorageSizeMB,
            StorageWarningThresholdMB = entity.StorageWarningThresholdMB
        };
    }

    private static DocumentSettingsEditDto ToEditDto(ManageDocumentSettings entity)
    {
        return new DocumentSettingsEditDto
        {
            Id = entity.Id,
            EnableDocSaveInFileSystem = entity.EnableDocSaveInFileSystem,
            FileSystemAttachmentPath = entity.FileSystemAttachmentPath,
            FileStorageRootName = entity.FileStorageRootName,
            MaxAttachmentsPerTransaction = entity.MaxAttachmentsPerTransaction,
            AllowedFileExtensions = entity.AllowedFileExtensions,
            MaxFileSizePerAttachmentMB = entity.MaxFileSizePerAttachmentMB,
            MaxStorageSizeMB = entity.MaxStorageSizeMB,
            StorageWarningThresholdMB = entity.StorageWarningThresholdMB
        };
    }
}
