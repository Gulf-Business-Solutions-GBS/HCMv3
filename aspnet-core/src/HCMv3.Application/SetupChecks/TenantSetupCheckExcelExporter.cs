using System;
using System.IO;
using System.Linq;
using Abp;
using Abp.AspNetZeroCore.Net;
using Abp.Dependency;
using ClosedXML.Excel;
using HCMv3.Dto;
using HCMv3.SetupChecks.Dto;
using HCMv3.Storage;

namespace HCMv3.SetupChecks;

/// <summary>
/// The tenant setup check report as a workbook: a heading block (tenant, release, date) and one row per check, the
/// result coloured as on screen. Made in memory and handed to the temporary file store the download reads from.
/// </summary>
public class TenantSetupCheckExcelExporter : AbpServiceBase, ITransientDependency
{
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#E9EEF6");
    private static readonly XLColor OkFill = XLColor.FromHtml("#DDF0E4");
    private static readonly XLColor MissingFill = XLColor.FromHtml("#F8D7DA");
    private static readonly XLColor OtherFill = XLColor.FromHtml("#FFF3CD");

    private const int FirstTableRow = 6;

    private readonly ITempFileCacheManager _tempFiles;

    public TenantSetupCheckExcelExporter(ITempFileCacheManager tempFiles)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
        _tempFiles = tempFiles;
    }

    public FileDto Export(TenantSetupCheckReportDto report)
    {
        using var book = new XLWorkbook();
        var sheet = book.AddWorksheet(Sheet(L("TenantSetupCheck")));

        sheet.Cell(1, 1).Value = L("TenantSetupCheck");
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Cell(2, 1).Value = L("Tenant");
        sheet.Cell(2, 2).SetValue(report.TenantName);
        sheet.Cell(3, 1).Value = L("Version");
        sheet.Cell(3, 2).SetValue(report.Version + " - " + L(report.OnlyThisVersion ? "SetupCheckOnlyThisVersion" : "SetupCheckUpToThisVersion"));
        sheet.Cell(4, 1).Value = L("SetupCheckCheckedOn");
        sheet.Cell(4, 2).Value = report.CheckedOn;
        sheet.Cell(4, 2).Style.DateFormat.Format = "dd-mm-yyyy";

        var headers = new[]
        {
            L("Code"), L("Module"), L("Version"), L("Severity"), L("SetupCheckWhatIsNeeded"), L("SetupCheckWhy"),
            L("Result"), L("SetupCheckMissingCount"), L("SetupCheckCheckedCount"), L("SetupCheckExamples"), L("SetupCheckFixOn"),
        };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(FirstTableRow, i + 1).Value = headers[i];
        }

        var header = sheet.Range(FirstTableRow, 1, FirstTableRow, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = HeaderFill;

        var r = FirstTableRow + 1;
        foreach (var item in report.Items)
        {
            sheet.Cell(r, 1).SetValue(item.Code);
            sheet.Cell(r, 2).SetValue(item.ModuleName);
            sheet.Cell(r, 3).SetValue(item.IntroducedIn);
            sheet.Cell(r, 4).SetValue(L("SetupCheckSeverity_" + item.Severity));
            sheet.Cell(r, 5).SetValue(item.Title);
            sheet.Cell(r, 6).SetValue(item.Why);

            var result = sheet.Cell(r, 7);
            result.SetValue(L("SetupCheckOutcome_" + item.Outcome) + (item.Message == null ? string.Empty : ": " + item.Message));
            result.Style.Fill.BackgroundColor = item.Outcome switch
            {
                SetupCheckOutcome.Ok => OkFill,
                SetupCheckOutcome.Missing => MissingFill,
                _ => OtherFill,
            };

            sheet.Cell(r, 8).Value = item.MissingCount;
            sheet.Cell(r, 9).Value = item.CheckedCount;
            sheet.Cell(r, 10).SetValue(string.Join(", ", item.Examples));
            sheet.Cell(r, 11).SetValue(item.FixScreenName == null ? item.FixScreenCode : $"{item.FixScreenName} ({item.FixScreenCode})");
            r++;
        }

        sheet.SheetView.FreezeRows(FirstTableRow);
        sheet.Range(FirstTableRow, 1, Math.Max(FirstTableRow, r - 1), headers.Length).SetAutoFilter();
        sheet.Columns(1, headers.Length).AdjustToContents(FirstTableRow, 200);
        foreach (var column in sheet.Columns(1, headers.Length))
        {
            column.Width = Math.Clamp(column.Width, 10, 60);
        }

        foreach (var wrapped in new[] { 5, 6, 10 })
        {
            sheet.Column(wrapped).Style.Alignment.WrapText = true;
        }

        var fileName = $"{L("TenantSetupCheck")} - {report.TenantName} - {report.Version}.xlsx";
        var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        _tempFiles.SetFile(file.FileToken, stream.ToArray());
        return file;
    }

    /// <summary>Excel refuses sheet names over 31 characters or with any of : \ / ? * [ ].</summary>
    private static string Sheet(string name)
    {
        var clean = new string((name ?? "Sheet").Where(c => !":\\/?*[]".Contains(c)).ToArray());
        return clean.Length > 31 ? clean[..31] : clean;
    }
}
