using System;
using System.Globalization;
using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.Storage;

/// <summary>The kinds of image a record carries, each in its own folder under the tenant's folder.</summary>
public static class EntityImageKinds
{
    public const string EmployeePhoto = "employee-photos";

    public const string EmployeeSignature = "employee-signatures";

    public const string ApplicantPhoto = "applicant-photos";
}

/// <summary>
/// Images that belong to one record -- an employee's photo and signature, an applicant's photo -- kept on
/// disk, with only the relative path stored on the record.
///
/// <para>
/// Layout: <c>tenant-{id}/{kind}/{record id}-{random}.img</c>. The tenant folder is the record's own tenant,
/// so one tenant's pictures can be backed up, measured or removed as a folder and never sit beside another's.
/// The random part changes on every save: a replaced photo gets a new path, so a browser cache keyed on the
/// old one cannot keep showing the old picture, and the old file is deleted.
/// </para>
///
/// <para>
/// Reads are checked against the caller's tenant as well as against the store root. The path comes from a
/// tenant-filtered database row, so a mismatch should never happen; if a row were ever tampered with, the
/// check stops it serving another tenant's picture.
/// </para>
/// </summary>
public interface IEntityImageStore
{
    /// <summary>
    /// Stores <paramref name="content"/> as the record's image and returns its new path, deleting
    /// <paramref name="currentPath"/> if there was one. Null or empty content removes the image and returns null.
    /// </summary>
    Task<string> ReplaceAsync(string kind, int tenantId, int recordId, string currentPath, byte[] content);

    /// <summary>The image at <paramref name="path"/>, or null if there is none or it is not this tenant's.</summary>
    Task<byte[]> ReadOrNullAsync(int? tenantId, string path);

    Task DeleteAsync(string path);

    /// <summary>
    /// <see cref="ReplaceAsync"/>, but only when <paramref name="content"/> differs from what is stored.
    /// Edit screens send the photo back on every save; rewriting it each time would churn files and change
    /// the path (and so the browser cache key) for nothing.
    /// </summary>
    Task<string> SaveIfChangedAsync(string kind, int tenantId, int recordId, string currentPath, byte[] content);
}

/// <inheritdoc cref="IEntityImageStore"/>
public class EntityImageStore : IEntityImageStore, ITransientDependency
{
    private readonly IFileStore _fileStore;

    public EntityImageStore(IFileStore fileStore)
    {
        _fileStore = fileStore;
    }

    public async Task<string> ReplaceAsync(string kind, int tenantId, int recordId, string currentPath, byte[] content)
    {
        string newPath = null;

        if (content != null && content.Length > 0)
        {
            newPath = string.Join("/",
                _fileStore.TenantSegment(tenantId),
                kind,
                recordId.ToString(CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N") + ".img");

            await _fileStore.WriteAsync(newPath, content);
        }

        // The new file is in place before the old one goes, so a failure in between leaves the record
        // pointing at a picture that still exists.
        if (!string.IsNullOrEmpty(currentPath) && currentPath != newPath)
        {
            await _fileStore.DeleteAsync(currentPath);
        }

        return newPath;
    }

    public Task<byte[]> ReadOrNullAsync(int? tenantId, string path)
    {
        if (string.IsNullOrEmpty(path)
            || !path.StartsWith(_fileStore.TenantSegment(tenantId) + "/", StringComparison.Ordinal))
        {
            return Task.FromResult<byte[]>(null);
        }

        return _fileStore.ReadOrNullAsync(path);
    }

    public Task DeleteAsync(string path) =>
        string.IsNullOrEmpty(path) ? Task.CompletedTask : _fileStore.DeleteAsync(path);

    public async Task<string> SaveIfChangedAsync(string kind, int tenantId, int recordId, string currentPath, byte[] content)
    {
        var hasContent = content is { Length: > 0 };

        if (!string.IsNullOrEmpty(currentPath) && hasContent)
        {
            var stored = await ReadOrNullAsync(tenantId, currentPath);
            if (stored != null && stored.AsSpan().SequenceEqual(content))
            {
                return currentPath;
            }
        }

        if (string.IsNullOrEmpty(currentPath) && !hasContent)
        {
            return null;
        }

        return await ReplaceAsync(kind, tenantId, recordId, currentPath, content);
    }
}
