using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using HCMv3.Configuration;
using Microsoft.Extensions.Configuration;

namespace HCMv3.Storage;

/// <summary>One folder the server administrator has approved for storing files.</summary>
/// <param name="Name">The name an administrator picks it by, e.g. <c>Default</c> or <c>Archive</c>.</param>
/// <param name="Path">Where it actually is on the server. Server layout, so only ever shown to a host user.</param>
/// <param name="IsDefault">The root used when nothing has been chosen.</param>
public sealed record FileStoreRoot(string Name, string Path, bool IsDefault);

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
/// <b>Roots are named in configuration, never typed by an administrator.</b> A path typed on a settings
/// screen is a path this process then writes to as the service account: pointed at the web root it turns an
/// upload into a served file, and pointed at another tenant's folder it hands over their scans. So
/// <c>appsettings.json</c> lists the folders the SERVER administrator approves, by name, and a tenant may
/// only choose among those names (<see cref="Roots"/>). The same reasoning applies as to the connection
/// string: which disk to write to is deployment configuration, not business configuration.
/// </para>
///
/// <para>
/// Paths are always RELATIVE to one of those roots, and every resolved path is checked to be inside it. A
/// relative path is data that reaches this class from the database; resolving it without the check would let
/// a tampered row read or overwrite any file the service account can reach. A path stored under a root other
/// than the default carries that root's name as a first segment -- <c>@archive/tenant-3/attachments/...</c>
/// -- so a file stays readable after a tenant is moved to a different disk, and so a future cloud provider
/// can be named the same way.
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

    /// <summary>The folder segment that holds one tenant's files ("host" for the host), under the default root.</summary>
    string TenantSegment(int? tenantId);

    /// <summary>
    /// The same segment under a named root: <c>@archive/tenant-3</c>, or plain <c>tenant-3</c> when
    /// <paramref name="rootName"/> is the default one, blank, or not configured.
    /// </summary>
    string TenantSegment(int? tenantId, string rootName);

    /// <summary>
    /// Whether <paramref name="relativePath"/> is one of <paramref name="tenantId"/>'s own files, whichever
    /// root it was stored under. Callers use this before handing a file to somebody.
    /// </summary>
    bool BelongsToTenant(string relativePath, int? tenantId);

    /// <summary>The roots configuration approves, default first. Never empty.</summary>
    IReadOnlyList<FileStoreRoot> Roots { get; }
}

/// <inheritdoc cref="IFileStore"/>
public class LocalFileStore : IFileStore, ISingletonDependency
{
    /// <summary>
    /// Optional, and the same as <c>FileStorage:Roots:Default</c>. Absent, files land under the
    /// application's own directory (<c>App_Data/FileStore</c>): fine for a developer machine, but a real
    /// deployment should point this at a disk that is backed up and is NOT inside the web root, because a
    /// redeploy replaces the application folder and everything under it.
    /// </summary>
    public const string RootPathConfigurationKey = "FileStorage:RootPath";

    /// <summary>
    /// Section holding the approved roots by name: <c>"FileStorage": { "Roots": { "Archive": "E:/HCMv3Archive" } }</c>.
    /// A blank value means the built-in default location, so one root can be named without giving a path.
    /// </summary>
    public const string RootsConfigurationSection = "FileStorage:Roots";

    /// <summary>The root used by anything that has not chosen one. Always present.</summary>
    public const string DefaultRootName = "Default";

    /// <summary>Marks a path's first segment as naming a root rather than a folder.</summary>
    public const char RootPrefix = '@';

    private readonly Dictionary<string, string> _roots;

    public LocalFileStore(IAppConfigurationAccessor configurationAccessor)
        : this(configurationAccessor?.Configuration)
    {
    }

    private LocalFileStore(IConfiguration configuration)
        : this(configuration?[RootPathConfigurationKey], ReadNamedRoots(configuration))
    {
    }

    /// <summary>For tests and tools: a store with one default root at <paramref name="rootPath"/>.</summary>
    public LocalFileStore(string rootPath)
        : this(rootPath, null)
    {
    }

    /// <summary>For tests and tools: a store with a default root and the named ones given.</summary>
    public LocalFileStore(string defaultRootPath, IEnumerable<KeyValuePair<string, string>> namedRoots)
    {
        _roots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [DefaultRootName] = FullPath(defaultRootPath)
        };

        foreach (var (name, path) in namedRoots ?? Array.Empty<KeyValuePair<string, string>>())
        {
            // A name with a slash or a dot in it could not be told apart from a folder in a stored path.
            if (string.IsNullOrWhiteSpace(name) || name.Any(c => c is '/' or '\\' or '.' or ':' or RootPrefix))
            {
                continue;
            }

            _roots[name.Trim()] = FullPath(path);
        }
    }

    /// <summary>Where the default root resolves to. Kept for the tests and tools that assert on it.</summary>
    public string Root => _roots[DefaultRootName];

    public IReadOnlyList<FileStoreRoot> Roots => _roots
        .Select(r => new FileStoreRoot(r.Key, r.Value, IsDefaultName(r.Key)))
        .OrderByDescending(r => r.IsDefault)
        .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public string TenantSegment(int? tenantId) =>
        tenantId.HasValue ? "tenant-" + tenantId.Value.ToString(CultureInfo.InvariantCulture) : "host";

    public string TenantSegment(int? tenantId, string rootName)
    {
        var segment = TenantSegment(tenantId);

        // An unconfigured name is not an error here: configuration may have changed under a stored choice,
        // and a new file is better written to the default root than not written at all. Where the choice is
        // read (IFileStorageRootResolver) it is reported.
        if (string.IsNullOrWhiteSpace(rootName) || IsDefaultName(rootName) || !_roots.ContainsKey(rootName.Trim()))
        {
            return segment;
        }

        return RootPrefix + rootName.Trim() + "/" + segment;
    }

    public bool BelongsToTenant(string relativePath, int? tenantId)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return false;
        }

        var (_, remainder) = SplitRoot(relativePath);

        return remainder.StartsWith(TenantSegment(tenantId) + "/", StringComparison.Ordinal);
    }

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

    private static string FullPath(string rootPath) =>
        Path.GetFullPath(
            string.IsNullOrWhiteSpace(rootPath)
                ? Path.Combine(AppContext.BaseDirectory, "App_Data", "FileStore")
                : rootPath);

    private static bool IsDefaultName(string name) =>
        string.Equals(name?.Trim(), DefaultRootName, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<KeyValuePair<string, string>> ReadNamedRoots(IConfiguration configuration) =>
        configuration?.GetSection(RootsConfigurationSection)
            .GetChildren()
            .Select(c => new KeyValuePair<string, string>(c.Key, c.Value))
        ?? Array.Empty<KeyValuePair<string, string>>();

    /// <summary>
    /// Splits a leading <c>@name/</c> off a stored path, defaulting when there is none.
    ///
    /// <para>
    /// A path that starts with <c>@</c> but names no root -- <c>@Archive</c> with no slash, or a bare
    /// <c>@/</c> -- returns a blank name, which <see cref="Resolve"/> refuses. Treating it as an ordinary
    /// folder called "@Archive" under the default root would be the worse answer: the same string would
    /// mean two different files depending on a trailing slash.
    /// </para>
    /// </summary>
    private static (string rootName, string remainder) SplitRoot(string relativePath)
    {
        if (relativePath.Length == 0 || relativePath[0] != RootPrefix)
        {
            return (DefaultRootName, relativePath);
        }

        var slash = relativePath.IndexOfAny(new[] { '/', '\\' });

        return slash <= 1
            ? (string.Empty, relativePath)
            : (relativePath.Substring(1, slash - 1), relativePath.Substring(slash + 1));
    }

    /// <summary>Turns a stored relative path into a full one, refusing anything that escapes its root.</summary>
    private string Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException($"'{relativePath}' is not a relative file store path.");
        }

        var (rootName, remainder) = SplitRoot(relativePath);
        if (!_roots.TryGetValue(rootName, out var root))
        {
            // Falling back to the default root would read or overwrite the wrong file under a name that
            // happens to be the same, so a root that configuration no longer lists stops here.
            throw new InvalidOperationException(
                $"'{relativePath}' names the file store root '{rootName}', which is not configured.");
        }

        var segments = remainder.Split('/', '\\').Where(s => s.Length > 0).ToArray();
        if (segments.Length == 0 || segments.Any(s => s == "." || s == ".."))
        {
            throw new InvalidOperationException($"'{relativePath}' is not a relative file store path.");
        }

        var candidate = Path.GetFullPath(Path.Combine(new[] { root }.Concat(segments).ToArray()));
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"'{relativePath}' resolves outside the file store root.");
        }

        return candidate;
    }
}
