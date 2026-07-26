using System.Threading.Tasks;
using Abp.Domain.Uow;
using HCMv3.SystemSettings.Services;

namespace HCMv3.SystemSecurity.Services;

/// <summary>
/// DB-dependent companion to <see cref="FileTypeDetector"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.StorageService</c>).
///
/// SIMPLIFICATION: legacy <c>GetUsedStorage</c> summed the <c>Size</c> column of the legacy
/// <c>Attachment</c>/<c>FileSysAttachment</c> entities. Neither entity (nor any generic
/// attachment/document-storage entity) has been ported into this codebase yet (grepped
/// <c>HCMv3.Core</c> for "Attachment" -- no matches), so there is nothing to sum against. Both methods
/// are therefore stubbed: <see cref="GetUsedStorageAsync"/> always returns 0 and
/// <see cref="GetRemainingStorageAsync"/> simply returns the configured max storage size. Revisit once/if
/// a document/attachment entity is ported in a future wave.
/// </summary>
[UnitOfWork]
public class StorageService : HCMv3DomainServiceBase, IStorageService
{
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public StorageService(ISysSettingsAccessor sysSettingsAccessor)
    {
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    public Task<long> GetUsedStorageAsync()
    {
        return Task.FromResult(0L);
    }

    public async Task<long> GetRemainingStorageAsync()
    {
        var settings = await _sysSettingsAccessor.GetDocumentSettingsAsync();
        var maxStorageBytes = (settings?.MaxStorageSizeMB ?? 0) * 1024L * 1024L;
        var used = await GetUsedStorageAsync();

        return maxStorageBytes - used;
    }
}
