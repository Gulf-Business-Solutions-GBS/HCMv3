using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Castle.Core.Logging;
using HCMv3.SystemSettings.Services;

namespace HCMv3.Storage;

/// <summary>
/// Which of the configured roots (<see cref="IFileStore.Roots"/>) the current tenant's new files go to.
///
/// <para>
/// The choice is a NAME held in <c>ManageDocumentSettings.FileStorageRootName</c>, never a path: an
/// administrator picks from what the server approves, so nothing they can type decides where this process
/// writes. The legacy <c>FileSystemAttachmentPath</c> column, which was free text, is left in place with its
/// old value and is not read -- see <see cref="IFileStore"/> for why.
/// </para>
/// </summary>
public interface IFileStorageRootResolver
{
    /// <summary>
    /// The tenant's chosen root name, or <see cref="LocalFileStore.DefaultRootName"/> when none is chosen or
    /// the chosen one is no longer configured.
    /// </summary>
    Task<string> GetCurrentRootNameAsync();
}

/// <inheritdoc cref="IFileStorageRootResolver"/>
public class FileStorageRootResolver : IFileStorageRootResolver, ITransientDependency
{
    private readonly IFileStore _fileStore;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public FileStorageRootResolver(IFileStore fileStore, ISysSettingsAccessor sysSettingsAccessor)
    {
        _fileStore = fileStore;
        _sysSettingsAccessor = sysSettingsAccessor;
        Logger = NullLogger.Instance;
    }

    public ILogger Logger { get; set; }

    public async Task<string> GetCurrentRootNameAsync()
    {
        var settings = await _sysSettingsAccessor.GetDocumentSettingsAsync();
        var chosen = settings?.FileStorageRootName?.Trim();

        if (string.IsNullOrEmpty(chosen))
        {
            return LocalFileStore.DefaultRootName;
        }

        if (_fileStore.Roots.Any(r => string.Equals(r.Name, chosen, StringComparison.OrdinalIgnoreCase)))
        {
            return chosen;
        }

        // Configuration changed under a stored choice -- a root was renamed or removed. New files go to the
        // default root rather than failing; files already written under the old name keep naming it, and
        // reading one of those does fail loudly, which is the half that must not pass silently.
        Logger.Warn(
            $"File storage root '{chosen}' is chosen in document settings but is not configured; " +
            "new files go to the default root. Check FileStorage:Roots in appsettings.json.");

        return LocalFileStore.DefaultRootName;
    }
}
