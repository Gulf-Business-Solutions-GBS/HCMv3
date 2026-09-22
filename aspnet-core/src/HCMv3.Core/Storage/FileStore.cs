using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using HCMv3.Configuration;
using Microsoft.Extensions.Configuration;

namespace HCMv3.Storage;

/// <summary>
/// Where file bytes live: a folder on the server, never the database.
///
/// <para>
/// Every stored file -- attachments, geo punch selfies, profile pictures, tenant logos -- goes through here.
/// Keeping bytes out of SQL Server is a deliberate decision: images and scans are the bulk of the data by
/// size, and in a table they bloat every backup and slow every query plan that touches the pages around
/// them. The database keeps only metadata and the relative path returned by <see cref="IFileStore"/>.
/// </para>
///
/// <para>
/// Paths are always RELATIVE to one root fixed by deployment configuration
/// (<see cref="RootPathConfigurationKey"/>), and every resolved path is checked to be inside that root. A
/// relative path is data that reaches this class from the database; resolving it without the check would
/// let a tampered row read or overwrite any file the service account can reach.
/// </para>
/// </summary>
public interface IFileStore
{
    /// <summary>Writes <paramref name="content"/> at <paramref name="relativePath"/>, creating folders as needed.</summary>
    Task WriteAsync(string relativePath, byte[] content);

    /// <summary>The file's bytes, or <c>null</c> when it does not exist.</summary>
    Task<byte[]> ReadOrNullAsync(string relativePath);

    /// <summary>Removes the file; a missing file is not an error.</summary>
    Task DeleteAsync(string relativePath);

    /// <summary>The folder segment that holds one tenant's files ("host" for the host).</summary>
    string TenantSegment(int? tenantId);
}

/// <inheritdoc cref="IFileStore"/>
public class LocalFileStore : IFileStore, ISingletonDependency
{
    /// <summary>
    /// Optional. Absent, files land under the application's own directory (<c>App_Data/FileStore</c>): fine
    /// for a developer machine, but a real deployment should point this at a disk that is backed up and is
    /// NOT inside the web root.
    /// </summary>
    public const string RootPathConfigurationKey = "FileStorage:RootPath";

    private readonly string _root;

    public LocalFileStore(IAppConfigurationAccessor configurationAccessor)
        : this(configurationAccessor?.Configuration?[RootPathConfigurationKey])
    {
    }

    /// <summary>For tests and tools: a store rooted at <paramref name="rootPath"/>.</summary>
    public LocalFileStore(string rootPath)
    {
        _root = Path.GetFullPath(
            string.IsNullOrWhiteSpace(rootPath)
                ? Path.Combine(AppContext.BaseDirectory, "App_Data", "FileStore")
                : rootPath);
    }

    public string Root => _root;

    public string TenantSegment(int? tenantId) =>
        tenantId.HasValue ? "tenant-" + tenantId.Value.ToString(CultureInfo.InvariantCulture) : "host";

    public async Task WriteAsync(string relativePath, byte[] content)
    {
        var fullPath = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Written to a temporary name first and moved into place, so a crash mid-write never leaves a
        // truncated file under the real name for a later read to hand out as if it were whole.
        var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await File.WriteAllBytesAsync(temporary, content ?? Array.Empty<byte>());
        File.Move(temporary, fullPath, overwrite: true);
    }

    public async Task<byte[]> ReadOrNullAsync(string relativePath)
    {
        var fullPath = Resolve(relativePath);

        return File.Exists(fullPath) ? await File.ReadAllBytesAsync(fullPath) : null;
    }

    public Task DeleteAsync(string relativePath)
    {
        var fullPath = Resolve(relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    /// <summary>Turns a stored relative path into a full one, refusing anything that escapes the root.</summary>
    private string Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException($"'{relativePath}' is not a relative file store path.");
        }

        var segments = relativePath.Split('/', '\\').Where(s => s.Length > 0).ToArray();
        if (segments.Any(s => s == "." || s == ".."))
        {
            throw new InvalidOperationException($"'{relativePath}' is not a relative file store path.");
        }

        var candidate = Path.GetFullPath(Path.Combine(new[] { _root }.Concat(segments).ToArray()));
        var rootWithSeparator = _root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"'{relativePath}' resolves outside the file store root.");
        }

        return candidate;
    }
}
