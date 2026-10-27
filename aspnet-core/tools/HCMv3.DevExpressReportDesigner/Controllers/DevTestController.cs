using System;
using System.IO;
using HCMv3.DevExpressReportDesigner.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace HCMv3.DevExpressReportDesigner.Controllers;

/// <summary>
/// Dev-only proof harness: constructs the rebound rptPayslipEn report and exports it to PDF directly,
/// bypassing the Designer/Viewer web UI entirely. This isolates "does the report render with real
/// HCMv3 data" from "does the DevExpress web widget wiring work" -- the same separation
/// HCMv3.MrtRender provides for the Stimulsoft track. Not linked from any navigation; hit it directly.
/// </summary>
[Route("dev-test")]
public class DevTestController : Controller
{
    [HttpGet("render-pdf")]
    public IActionResult RenderPdf()
    {
        using var report = PayslipEnReportFactory.Build();
        report.CreateDocument();

        using var stream = new MemoryStream();
        report.ExportToPdf(stream);
        var bytes = stream.ToArray();

        var outDir = Path.Combine(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, "rptPayslipEn.devexpress.pdf");
        System.IO.File.WriteAllBytes(outPath, bytes);

        return Content($"wrote {outPath} ({bytes.Length} bytes), pages={report.Pages.Count}", "text/plain");
    }
}
