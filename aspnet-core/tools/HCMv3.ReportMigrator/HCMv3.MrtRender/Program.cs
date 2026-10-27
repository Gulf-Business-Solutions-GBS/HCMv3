using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stimulsoft.Report;
using Stimulsoft.Report.Dictionary;
using Stimulsoft.Report.Export;

namespace HCMv3.MrtRender;

/// <summary>
/// Renders a converted .mrt against a live HCMv3 database and writes a PDF.
///
/// <para>
/// This is the acceptance gate for a migrated report. A .mrt that opens in the designer proves
/// nothing: the interesting failure mode of this migration is a layout that binds cleanly, queries
/// successfully and returns zero rows, which looks identical to "the employee had no payslip".
/// This harness therefore prints the row count of every data source before exporting.
/// </para>
///
/// Usage:
///   HCMv3.MrtRender &lt;report.mrt&gt; &lt;out.pdf&gt; [Name=Value ...]
///
/// The connection string comes from the ConnectionStrings__Default environment variable
/// (appsettings.json is the shared team file and is deliberately never read or written here).
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("usage: HCMv3.MrtRender <report.mrt> <out.pdf> [Variable=Value ...]");
            return 2;
        }

        var mrtPath = args[0];
        var pdfPath = args[1];

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Data Source=localhost\\SQLEXPRESS;Initial Catalog=HCMv3;Integrated Security=SSPI;TrustServerCertificate=True";

        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in args.Skip(2))
        {
            var i = a.IndexOf('=');
            if (i > 0) overrides[a.Substring(0, i)] = a.Substring(i + 1);
        }

        var report = new StiReport();
        report.Load(mrtPath);

        // Every converted report carries one StiSqlDatabase per data source, all named "db", because
        // DevExpress stored the connection as <Connection Name="HCM10ConnectionString" FromAppConfig="true" />
        // and the importer has no case for FromAppConfig (788 warnings across the corpus -- the single
        // most common gap). Point them all at HCMv3 here.
        var databases = report.Dictionary.Databases.Cast<StiDatabase>().ToList();
        foreach (var database in databases)
        {
            if (database is StiSqlDatabase sql)
            {
                sql.ConnectionString = connectionString;
            }
        }
        Console.WriteLine($"databases rebound: {databases.Count}");

        foreach (var kv in overrides)
        {
            var variable = report.Dictionary.Variables.Cast<StiVariable>().FirstOrDefault(
                v => string.Equals(v.Name, kv.Key, StringComparison.OrdinalIgnoreCase));
            if (variable == null)
            {
                Console.WriteLine($"  WARNING: no report variable named '{kv.Key}'");
                continue;
            }
            variable.ValueObject = kv.Value;
            report[variable.Name] = kv.Value;
        }

        // Interpretation, not compilation. The importer emits expressions that reference data columns
        // by their raw name ("Employee Code"), which is legal in Stimulsoft's interpreter but is not a
        // valid C# identifier, so StiReport.Compile() fails with a wall of CS1003s. Interpretation is
        // also the supported mode on .NET Core.
        report.CalculationMode = Stimulsoft.Report.StiCalculationMode.Interpretation;
        foreach (var kv in overrides)
        {
            try { report[kv.Key] = kv.Value; } catch { }
        }

        report.Dictionary.Synchronize();

        var failures = 0;
        foreach (StiDataSource dataSource in report.Dictionary.DataSources)
        {
            try
            {
                dataSource.Connect();
                Console.WriteLine($"  {dataSource.Name,-26} rows={dataSource.Count}");
                if (dataSource.Count == 0) failures++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {dataSource.Name,-26} ERROR {ex.GetType().Name}: {ex.Message}");
                failures++;
            }
        }

        report.Render(false);
        Console.WriteLine($"rendered pages: {report.RenderedPages.Count}");

        using (var stream = new FileStream(pdfPath, FileMode.Create))
        {
            report.ExportDocument(StiExportFormat.Pdf, stream);
        }

        Console.WriteLine($"wrote {pdfPath} ({new FileInfo(pdfPath).Length} bytes)");

        // A PNG per page as well: the point of this harness is that a human can LOOK at the result,
        // and a PDF is awkward to eyeball in a terminal-driven workflow.
        var pngDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(pdfPath)),
                                  Path.GetFileNameWithoutExtension(pdfPath) + "_png");
        Directory.CreateDirectory(pngDir);
        var imageSettings = new Stimulsoft.Report.Export.StiImageExportSettings
        {
            ImageResolution = 120
        };
        for (var page = 0; page < report.RenderedPages.Count; page++)
        {
            imageSettings.PageRange = new StiPagesRange(StiRangeType.Pages, (page + 1).ToString(), page);
            using var pngStream = new FileStream(Path.Combine(pngDir, $"page{page + 1}.png"), FileMode.Create);
            report.ExportDocument(StiExportFormat.ImagePng, pngStream, imageSettings);
        }
        Console.WriteLine($"wrote {report.RenderedPages.Count} page image(s) to {pngDir}");
        Console.WriteLine(failures == 0
            ? "ALL DATA SOURCES RETURNED ROWS"
            : $"{failures} data source(s) empty or failed -- check before calling this report migrated");
        return 0;
    }
}
