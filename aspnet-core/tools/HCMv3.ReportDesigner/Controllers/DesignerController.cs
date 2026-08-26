using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;

namespace HCMv3.ReportDesigner.Controllers
{
    public class DesignerController : Controller
    {
        // Bundled with this project (CopyToOutputDirectory) - always available.
        private const string DefaultReportFile = "rptPayslipEn.HCMv3.mrt";

        // Auto-converted reports produced by the migration pipeline. Gitignored/regenerable,
        // read directly from ReportMigrator's output folder so we don't duplicate 481 files.
        private readonly IWebHostEnvironment _env;

        public DesignerController(IWebHostEnvironment env)
        {
            _env = env;
        }

        private string BundledReportsPath => Path.Combine(_env.ContentRootPath, "Reports");

        private string MrtAllPath => Path.GetFullPath(Path.Combine(
            _env.ContentRootPath, "..", "HCMv3.ReportMigrator", "out", "mrt-all"));

        public IActionResult Index(string file)
        {
            var otherReports = new List<string>();
            try
            {
                if (Directory.Exists(MrtAllPath))
                {
                    otherReports = Directory.GetFiles(MrtAllPath, "*.mrt")
                        .Select(Path.GetFileName)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }
            catch (IOException)
            {
                // out/mrt-all is regenerable pipeline output; if it's missing or unreadable,
                // just fall back to the bundled payslip only.
            }

            var selected = string.IsNullOrWhiteSpace(file) ? DefaultReportFile : Path.GetFileName(file);

            ViewBag.DefaultReportFile = DefaultReportFile;
            ViewBag.SelectedReportFile = selected;
            ViewBag.OtherReports = otherReports;
            return View();
        }

        public IActionResult GetReport(string file)
        {
            var report = new StiReport();
            report.Load(ResolveReportPath(file));
            return StiNetCoreDesigner.GetReportResult(this, report);
        }

        public IActionResult DesignerEvent()
        {
            return StiNetCoreDesigner.DesignerEventResult(this);
        }

        private string ResolveReportPath(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || file == DefaultReportFile)
            {
                return Path.Combine(BundledReportsPath, DefaultReportFile);
            }

            // Only allow a bare filename resolved against the known mrt-all folder -
            // never trust a caller-supplied path directly.
            var safeName = Path.GetFileName(file);
            var candidate = Path.Combine(MrtAllPath, safeName);
            if (System.IO.File.Exists(candidate))
            {
                return candidate;
            }

            return Path.Combine(BundledReportsPath, DefaultReportFile);
        }
    }
}
