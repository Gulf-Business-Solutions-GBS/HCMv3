using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Abp.Dependency;

namespace HCMv3.SetupChecks;

/// <summary>Something a change added that a tenant may have to set up.</summary>
public record ReleaseSetupNote(SetupItemKind Kind, string Text);

/// <summary>One change to HCMv3 (a git commit): the build it arrived in, when, and what it says it did.</summary>
/// <param name="Number">The build number it arrived in; a build holds every change numbered up to its own.</param>
public record ReleaseChange(int Number, string Hash, DateTime Date, string Subject, string Module, IReadOnlyList<ReleaseSetupNote> Setup);

/// <summary>The version this build is, and every change that went into it.</summary>
public interface IReleaseLog
{
    /// <summary>E.g. <c>3.0.531</c>: the base version and the build number.</summary>
    string Version { get; }

    int Number { get; }

    DateTime? BuiltOn { get; }

    /// <summary>Newest first.</summary>
    IReadOnlyList<ReleaseChange> Changes { get; }

    /// <summary>The version a build number is shown as, e.g. 498 gives <c>3.0.498</c>.</summary>
    string VersionOf(int number);

    /// <summary>The build number in a version such as <c>3.0.498</c>; null when it is not one of ours.</summary>
    int? NumberOf(string version);
}

/// <summary>
/// The build's own version number and change log, generated from git by the build
/// (<c>tools/release-log/build-release-log.ps1</c>) and embedded in this assembly.
///
/// <para><b>Nobody types a version.</b> The build number is the count of commits in the build, so every deploy with new
/// code has a higher number than the one before, and "what changed since build P" is every change numbered above P.</para>
///
/// <para>A build made where git was not available has no log: it reports the base version, build 0, and no changes.</para>
/// </summary>
public class ReleaseLog : IReleaseLog, ISingletonDependency
{
    public const string ResourceName = "HCMv3.Releases.release-log.json";
    private const string FallbackBaseVersion = "3.0";

    private static readonly Lazy<ReleaseLog> EmbeddedLog = new(() =>
    {
        using var stream = typeof(ReleaseLog).Assembly.GetManifestResourceStream(ResourceName);
        if (stream == null)
        {
            return new ReleaseLog((string)null);
        }

        using var reader = new StreamReader(stream);
        return new ReleaseLog(reader.ReadToEnd());
    });

    private readonly string _baseVersion;

    /// <summary>Used by dependency injection: the log embedded in this build.</summary>
    public ReleaseLog() : this(Embedded)
    {
    }

    private ReleaseLog(ReleaseLog other)
    {
        _baseVersion = other._baseVersion;
        Number = other.Number;
        BuiltOn = other.BuiltOn;
        Changes = other.Changes;
    }

    /// <summary>Reads a log as the build script writes it; null or unreadable JSON gives the empty log.</summary>
    public ReleaseLog(string json)
    {
        _baseVersion = FallbackBaseVersion;
        Changes = Array.Empty<ReleaseChange>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            _baseVersion = root.GetProperty("baseVersion").GetString() ?? FallbackBaseVersion;
            Number = root.GetProperty("number").GetInt32();
            BuiltOn = root.TryGetProperty("builtOn", out var builtOn) && builtOn.TryGetDateTime(out var at) ? at : null;
            Changes = root.GetProperty("changes").EnumerateArray().Select(ReadChange).OrderByDescending(c => c.Number).ToList();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            _baseVersion = FallbackBaseVersion;
            Number = 0;
            BuiltOn = null;
            Changes = Array.Empty<ReleaseChange>();
        }
    }

    /// <summary>The log embedded in this build, for code that cannot take a dependency.</summary>
    public static ReleaseLog Embedded => EmbeddedLog.Value;

    public string Version => VersionOf(Number);

    public int Number { get; }

    public DateTime? BuiltOn { get; }

    public IReadOnlyList<ReleaseChange> Changes { get; }

    public string VersionOf(int number) => $"{_baseVersion}.{number}";

    public int? NumberOf(string version)
    {
        var prefix = _baseVersion + ".";
        return version != null && version.StartsWith(prefix, StringComparison.Ordinal)
               && int.TryParse(version[prefix.Length..], out var number) && number >= 0
            ? number
            : null;
    }

    private static ReleaseChange ReadChange(JsonElement change)
    {
        var setup = new List<ReleaseSetupNote>();
        if (change.TryGetProperty("setup", out var notes) && notes.ValueKind == JsonValueKind.Array)
        {
            foreach (var note in notes.EnumerateArray())
            {
                if (Enum.TryParse<SetupItemKind>(note.GetProperty("kind").GetString(), out var kind))
                {
                    setup.Add(new ReleaseSetupNote(kind, note.GetProperty("text").GetString()));
                }
            }
        }

        return new ReleaseChange(
            change.GetProperty("n").GetInt32(),
            change.GetProperty("hash").GetString(),
            DateTime.Parse(change.GetProperty("date").GetString(), System.Globalization.CultureInfo.InvariantCulture),
            change.GetProperty("subject").GetString(),
            change.TryGetProperty("module", out var module) ? module.GetString() : null,
            setup);
    }
}
