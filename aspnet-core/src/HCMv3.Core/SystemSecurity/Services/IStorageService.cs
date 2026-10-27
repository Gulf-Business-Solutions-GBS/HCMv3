using System.Threading.Tasks;

namespace HCMv3.SystemSecurity.Services;

/// <summary>
/// Companion domain service to <see cref="FileTypeDetector"/> for the DB-dependent parts of legacy
/// <c>StorageService</c> (<c>GetUsedStorage</c>/<c>GetRemainingStorage</c>). See <see cref="StorageService"/>
/// remarks for why these are currently stubbed.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// STUBBED -- see <see cref="StorageService"/> remarks: no <c>Attachment</c>/<c>FileSysAttachment</c>
    /// equivalent entity has been ported into this codebase, so there is nothing to sum. Always returns 0.
    /// </summary>
    Task<long> GetUsedStorageAsync();

    /// <summary>
    /// STUBBED -- see <see cref="StorageService"/> remarks. Returns <c>ManageDocumentSettings.MaxStorageSizeMB</c>
    /// (converted to bytes) since used storage is always reported as 0.
    /// </summary>
    Task<long> GetRemainingStorageAsync();
}
