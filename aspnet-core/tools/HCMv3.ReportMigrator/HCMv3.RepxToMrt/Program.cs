using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stimulsoft.Report.Import;

namespace HCMv3.RepxToMrt;

/// <summary>
/// Batch converter: DevExpress <c>.repx</c> layouts -> Stimulsoft <c>.mrt</c> layouts.
///
/// <para>
/// The conversion is driven by <see cref="StiDevExpressHelper"/>, which is Stimulsoft's own
/// open-source importer (Importing.Tools / Import.XtraReports2025) vendored into this repo.
/// Stimulsoft ships it as source, not as a package: the designer hook
/// <c>StiOptions.Designer.MainMenu.ImportXtraReports</c> is a deliberately-null extension point that
/// the host application is expected to fill in. There is no DevExpress dependency -- the helper is a
/// pure XML parser over the .repx document.
/// </para>
///
/// <para>
/// Every gap in the helper's coverage is reported through its own <c>ThrowError</c> path and lands in
/// <c>_convert.log</c> plus <c>_warnings.tsv</c>, so the warning histogram doubles as the worklist for
/// improving the importer. Conversion never fails because of a warning.
/// </para>
///
/// Usage: HCMv3.RepxToMrt &lt;inputDir&gt; &lt;outputDir&gt; [file1.repx file2.repx ...]
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: HCMv3.RepxToMrt <inputDir> <outputDir> [specific .repx files...]");
            return 2;
        }

        var inDir = args[0];
        var outDir = args[1];
        Directory.CreateDirectory(outDir);

        var files = args.Length > 2
            ? args.Skip(2).ToArray()
            : Directory.GetFiles(inDir, "*.repx");

        var ok = 0;
        var fail = 0;
        var warningHistogram = new Dictionary<string, int>(StringComparer.Ordinal);

        using var log = new StreamWriter(Path.Combine(outDir, "_convert.log"));
        using var warnings = new StreamWriter(Path.Combine(outDir, "_warnings.tsv"));
        warnings.WriteLine("Report\tWarning");

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = StiDevExpressHelper.Import(File.ReadAllBytes(file));
                sw.Stop();

                if (result.Report == null)
                {
                    fail++;
                    log.WriteLine($"FAIL\t{name}\t{string.Join(" | ", result.Errors ?? new List<string>())}");
                    continue;
                }

                result.Report.Save(Path.Combine(outDir, name + ".mrt"));
                ok++;

                var errors = result.Errors ?? new List<string>();
                log.WriteLine($"OK\t{name}\t{sw.ElapsedMilliseconds}ms\twarnings={errors.Count}");
                foreach (var e in errors)
                {
                    var flat = e.Replace('\r', ' ').Replace('\n', ' ');
                    log.WriteLine("\t\t" + flat);
                    warnings.WriteLine(name + "\t" + flat);

                    var key = Bucket(flat);
                    warningHistogram[key] = warningHistogram.TryGetValue(key, out var n) ? n + 1 : 1;
                }
            }
            catch (Exception ex)
            {
                fail++;
                log.WriteLine($"EXC\t{name}\t{ex.GetType().Name}: {ex.ToString().Replace('\r', ' ').Replace('\n', '~')}");
            }
        }

        log.WriteLine($"TOTAL ok={ok} fail={fail}");
        log.WriteLine("--- warning histogram ---");
        foreach (var kv in warningHistogram.OrderByDescending(k => k.Value))
        {
            log.WriteLine($"{kv.Value,6}  {kv.Key}");
        }

        Console.WriteLine($"ok={ok} fail={fail} distinctWarningKinds={warningHistogram.Count}");
        foreach (var kv in warningHistogram.OrderByDescending(k => k.Value).Take(20))
        {
            Console.WriteLine($"{kv.Value,6}  {kv.Key}");
        }

        return 0;
    }

    /// <summary>
    /// Collapses a warning to its "kind" so the histogram counts gaps in the importer rather than
    /// individual control names. The helper's messages look like
    /// "Property 'StringFormat' of 'XRLabel' is not supported" -- the quoted names are the payload.
    /// </summary>
    private static string Bucket(string warning)
    {
        var parts = warning.Split('\'');
        return parts.Length >= 2 ? string.Join("|", parts.Where((_, i) => i % 2 == 1)) : warning;
    }
}
