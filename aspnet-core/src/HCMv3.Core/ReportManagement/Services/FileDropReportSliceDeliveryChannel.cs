using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Timing;
using Castle.Core.Logging;
using HCMv3.Configuration;
using Microsoft.Extensions.Configuration;

namespace HCMv3.ReportManagement.Services;

/// <summary>
/// Writes a rendered slice to a folder on the server for later collection -- the
/// <see cref="ReportDestinationMethod.Offline"/> destination.
///
/// WHY THIS EXISTS ALONGSIDE E-MAIL. Everything else in the module -- scope resolution, the
/// slice/recipient intersection, bursting -- can only really be trusted once somebody has opened two
/// department heads' files and seen that they differ. E-mail makes that proof wait on a configured mail
/// server. This channel makes the same bursting observable with nothing but a folder, so the
/// distribution logic can be verified against real data before any SMTP credentials exist.
///
/// WHERE THE FILES GO, AND WHY NOT WHERE THE ADMINISTRATOR SAYS. <c>StorageFolder</c> on the
/// destination row is administrator-entered text. Treating it as a path would let anyone who can reach
/// the API write payroll spreadsheets anywhere the service account can write -- a web root, a startup
/// folder. It is therefore a RELATIVE subfolder beneath a root fixed by deployment configuration, and
/// the resolved path is checked to be inside that root before anything is written. Segments are
/// sanitised first; the containment check is the backstop for whatever sanitising misses.
///
/// The tenant id is part of the path rather than left to the folder name, so two tenants that happen to
/// choose the same <c>StorageFolder</c> cannot write into each other's drop.
/// </summary>
public class FileDropReportSliceDeliveryChannel : IReportSliceDeliveryChannel, ITransientDependency
{
    /// <summary>
    /// Optional. Absent, files land under the application's own directory: enough to verify a schedule,
    /// but not where a real deployment should leave payroll data.
    /// </summary>
    public const string RootPathConfigurationKey = "ReportDistribution:FileDropRootPath";

    private readonly IConfigurationRoot _appConfiguration;

    public ILogger Logger { get; set; }

    public FileDropReportSliceDeliveryChannel(IAppConfigurationAccessor configurationAccessor)
    {
        _appConfiguration = configurationAccessor.Configuration;
        Logger = NullLogger.Instance;
    }

    public ReportDestinationMethod Method => ReportDestinationMethod.Offline;

    public Task DeliverAsync(
        ReportSchedule schedule,
        ReportScheduleDestination destination,
        ReportBurstSlice slice,
        ReportSliceRecipient recipient,
        RenderedReportSlice rendered)
    {
        if (rendered?.Content == null || rendered.Content.Length == 0)
        {
            // The executor already skips empty slices, so reaching here means a renderer reported rows
            // and produced no bytes. A 0-byte file would be logged as a successful delivery.
            throw new InvalidOperationException("Nothing to write: the rendered slice is empty.");
        }

        var directory = ResolveDirectory(schedule, destination);
        Directory.CreateDirectory(directory);

        var path = ClaimPath(directory, BuildFileName(rendered, recipient));

        File.WriteAllBytes(path, rendered.Content);

        // Logged because the path is the only way anybody finds these files: the run log records the
        // file name, not the folder the channel chose for it.
        Logger.Info($"Report schedule {schedule.Id}: wrote slice '{slice?.Key}' to {path}");

        return Task.CompletedTask;
    }

    /// <summary>Resolves the drop root, honouring configuration and falling back to the app directory.</summary>
    public string ResolveRoot()
    {
        var configured = _appConfiguration?[RootPathConfigurationKey];

        return Path.GetFullPath(
            string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(AppContext.BaseDirectory, "App_Data", "ReportDrops")
                : configured);
    }

    /// <summary>Builds the target directory and proves it is inside the configured root.</summary>
    private string ResolveDirectory(ReportSchedule schedule, ReportScheduleDestination destination)
    {
        var root = ResolveRoot();

        // The schedule's own tenant, not the ambient session: a scheduled run has no session at all.
        var tenantSegment = "tenant-" + schedule.TenantId.ToString(CultureInfo.InvariantCulture);
        var daySegment = Clock.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var parts = new[] { root, tenantSegment }
            .Concat(SafeSegments(destination?.StorageFolder))
            .Concat(new[] { daySegment })
            .ToArray();

        var candidate = Path.GetFullPath(Path.Combine(parts));

        // Backstop. Sanitising segments should already make this unreachable; if it is ever reached,
        // refusing is the only safe answer -- the alternative is writing outside the drop root.
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The destination folder '{destination?.StorageFolder}' resolves outside the report drop root.");
        }

        return candidate;
    }

    /// <summary>
    /// Splits the administrator's folder into segments, dropping anything that could climb out of the
    /// root or name something other than a plain folder.
    /// </summary>
    private static string[] SafeSegments(string storageFolder)
    {
        if (string.IsNullOrWhiteSpace(storageFolder))
        {
            return new[] { "default" };
        }

        var segments = storageFolder
            .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(Sanitize)
            .Where(s => s.Length > 0)
            .ToArray();

        return segments.Length == 0 ? new[] { "default" } : segments;
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();

        // A segment of nothing but dots is how ".." survives naive filtering; collapse it away.
        return cleaned.Length > 0 && cleaned.All(c => c == '.') ? string.Empty : cleaned;
    }

    /// <summary>
    /// One file per RECIPIENT, not per slice. Two recipients of the same slice can hold different
    /// scopes and so receive genuinely different files; sharing a name would let the second overwrite
    /// the first and destroy exactly the evidence this channel exists to produce.
    /// </summary>
    private static string BuildFileName(RenderedReportSlice rendered, ReportSliceRecipient recipient)
    {
        var stem = Path.GetFileNameWithoutExtension(rendered.FileName);
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "report";
        }

        var extension = Path.GetExtension(rendered.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".xlsx";
        }

        var who = FirstNonBlank(
            recipient?.Email,
            recipient?.DisplayName,
            recipient?.EmployeeId?.ToString(CultureInfo.InvariantCulture),
            "recipient");

        return Sanitize($"{stem} - {who}") + extension;
    }

    private static string FirstNonBlank(params string[] candidates)
    {
        return candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? "recipient";
    }

    /// <summary>
    /// Returns a path that does not already exist, suffixing a counter rather than overwriting. Two
    /// runs of the same schedule on the same day would otherwise leave only the later one, which reads
    /// as the earlier run never having happened.
    /// </summary>
    private static string ClaimPath(string directory, string fileName)
    {
        var candidate = Path.Combine(directory, fileName);
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        var stem = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        for (var i = 2; i < 1000; i++)
        {
            candidate = Path.Combine(directory, $"{stem} ({i}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"Could not find a free file name for '{fileName}' in {directory}.");
    }
}
