using System;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;

namespace HCMv3.Storage;

/// <summary>
/// <see cref="IBinaryObjectManager"/> backed by the file system instead of the <c>AppBinaryObjects</c> table.
///
/// <para>
/// Replaces ASP.NET Zero's <c>DbBinaryObjectManager</c> so that every caller -- profile pictures, tenant
/// logos and CSS, chat files, Excel import uploads, geo punch selfies -- writes to disk without any of them
/// changing. The <see cref="BinaryObject"/> entity survives only as the shape callers already pass around.
/// </para>
///
/// <para>
/// ONE FOLDER PER TENANT. An object is written under its own tenant's segment
/// (<c>tenant-{id}/binary-objects/{shard}/{id}.bin</c>, or <c>host/...</c>), and read from the folder of the
/// tenant the current unit of work is running as -- the same tenant the <c>MayHaveTenant</c> filter used for
/// the table. Asking for another tenant's id therefore looks in the wrong folder and finds nothing: tenants
/// are kept apart by where the bytes are, not only by a check. The <c>.json</c> sidecar still records the
/// owning tenant and is compared as a second guard. Every caller in the code base selects the tenant with
/// <c>SetTenantId</c> before reading (logos, chat), none switches the filter off, so the tenant is always known.
/// </para>
///
/// <para>
/// READS FALL BACK TO THE TABLE. Rows written before this change are still in <c>AppBinaryObjects</c>; an id
/// not found on disk is looked up there, so existing profile pictures and logos keep working. Nothing new is
/// ever written to the table.
/// </para>
/// </summary>
public class FileSystemBinaryObjectManager : IBinaryObjectManager, ITransientDependency
{
    private const string Folder = "binary-objects";

    private readonly IFileStore _fileStore;
    private readonly IFileStorageRootResolver _rootResolver;
    private readonly IRepository<BinaryObject, Guid> _legacyRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IAbpSession _session;

    public FileSystemBinaryObjectManager(
        IFileStore fileStore,
        IFileStorageRootResolver rootResolver,
        IRepository<BinaryObject, Guid> legacyRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IAbpSession session)
    {
        _fileStore = fileStore;
        _rootResolver = rootResolver;
        _legacyRepository = legacyRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _session = session;
    }

    public async Task<BinaryObject> GetOrNullAsync(Guid id)
    {
        var tenantId = CurrentTenantId();

        // Unlike an attachment, a binary object has no stored path to carry its root name: it is addressed
        // by id alone, so the path is recomputed on every read. That is why a read looks under the default
        // root as well -- a tenant moved to another disk must not lose the pictures written before the move.
        var root = await RootFor(tenantId, id);
        var metadataJson = await _fileStore.ReadOrNullAsync(MetadataPath(tenantId, id, root));
        if (metadataJson == null)
        {
            return await _legacyRepository.FirstOrDefaultAsync(id);
        }

        var metadata = JsonSerializer.Deserialize<Metadata>(metadataJson);
        if (metadata.TenantId != tenantId)
        {
            // Unreachable unless a file was copied into the wrong tenant's folder by hand.
            return null;
        }

        var bytes = await _fileStore.ReadOrNullAsync(ContentPath(tenantId, id, root));
        if (bytes == null)
        {
            return null;
        }

        return new BinaryObject(metadata.TenantId, bytes, metadata.Description) { Id = id };
    }

    public async Task SaveAsync(BinaryObject file)
    {
        if (file.Id == Guid.Empty)
        {
            file.Id = Guid.NewGuid();
        }

        // Filed under the object's own tenant. Bytes first, sidecar last: a reader only trusts an id whose
        // sidecar exists, so an interrupted save leaves an orphan file rather than a half-written object.
        var root = await _rootResolver.GetCurrentRootNameAsync();
        await _fileStore.WriteAsync(ContentPath(file.TenantId, file.Id, root), file.Bytes);
        await _fileStore.WriteAsync(MetadataPath(file.TenantId, file.Id, root),
            JsonSerializer.SerializeToUtf8Bytes(new Metadata { TenantId = file.TenantId, Description = file.Description }));
    }

    public async Task DeleteAsync(Guid id)
    {
        var tenantId = CurrentTenantId();
        var root = await RootFor(tenantId, id);

        if (await _fileStore.ReadOrNullAsync(MetadataPath(tenantId, id, root)) != null)
        {
            await _fileStore.DeleteAsync(MetadataPath(tenantId, id, root));
            await _fileStore.DeleteAsync(ContentPath(tenantId, id, root));
            return;
        }

        var legacy = await _legacyRepository.FirstOrDefaultAsync(id);
        if (legacy != null)
        {
            await _legacyRepository.DeleteAsync(legacy);
        }
    }

    /// <summary>The tenant the <c>MayHaveTenant</c> filter would have used: the unit of work's, else the session's.</summary>
    private int? CurrentTenantId()
    {
        var unitOfWork = _unitOfWorkManager.Current;

        return unitOfWork != null ? unitOfWork.GetTenantId() : _session.TenantId;
    }

    private string ContentPath(int? tenantId, Guid id, string root) => $"{Directory(tenantId, id, root)}/{id:N}.bin";

    private string MetadataPath(int? tenantId, Guid id, string root) => $"{Directory(tenantId, id, root)}/{id:N}.json";

    private string Directory(int? tenantId, Guid id, string root) =>
        $"{_fileStore.TenantSegment(tenantId, root)}/{Folder}/{id.ToString("N").Substring(0, 2)}";

    /// <summary>
    /// The root a stored object is actually under: the tenant's current one when its sidecar is there, and
    /// otherwise the default, where everything written before the tenant was moved still sits.
    /// </summary>
    private async Task<string> RootFor(int? tenantId, Guid id)
    {
        var current = await _rootResolver.GetCurrentRootNameAsync();
        if (string.Equals(current, LocalFileStore.DefaultRootName, StringComparison.OrdinalIgnoreCase))
        {
            return current;
        }

        return await _fileStore.ReadOrNullAsync(MetadataPath(tenantId, id, current)) != null
            ? current
            : LocalFileStore.DefaultRootName;
    }

    private sealed class Metadata
    {
        public int? TenantId { get; set; }

        public string Description { get; set; }
    }
}
