using System;
using System.Collections.Generic;
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
/// What's New &amp; Setup as a workbook: one row per feature, and one more row per setup check a feature needs, the
/// setup status coloured as on screen. Made in memory and handed to the temporary file store the download reads from.
/// </summary>
public class TenantSetupCheckExcelExporter : AbpServiceBase, ITransientDependency
{
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#E9EEF6");
    private static readonly XLColor OkFill = XLColor.FromHtml("#DDF0E4");
    private static readonly XLColor MissingFill = XLColor.FromHtml("#F8D7DA");
    private static readonly XLColor OtherFill = XLColor.FromHtml("#FFF3CD");

    private readonly ITempFileCacheManager _tempFiles;

    public TenantSetupCheckExcelExporter(ITempFileCacheManager tempFiles)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
        _tempFiles = tempFiles;
    }

    /// <param name="releases">The releases to list.</param>
    /// <param name="status">When exporting what an update brought: the versions, for the heading. Null for one release.</param>
    public FileDto Export(IReadOnlyList<ReleaseChangesDto> releases, ReleaseUpdateStatusDto status)
    {
        using var book = new XLWorkbook();
        var sheet = book.AddWorksheet(Sheet(L("TenantSetupCheck")));

        sheet.Cell(1, 1).Value = L("TenantSetupCheck");
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        if (status != null)
        {
            sheet.Cell(2, 1).Value = L("WhatsNewInstalledVersion");
            sheet.Cell(2, 2).SetValue(status.CurrentVersion);
            sheet.Cell(3, 1).Value = L("WhatsNewPreviousVersion");
            sheet.Cell(3, 2).SetValue(status.PreviousVersion ?? L("WhatsNewNoPreviousVersion"));
        }

        const int headerRow = 5;
        var headers = new[]
        {
            L("SetupCheckRelease"), L("Module"), L("WhatsNewFeature"), L("WhatsNewWhatChanged"), L("WhatsNewSetup"),
            L("Code"), L("SetupCheckWhatIsNeeded"), L("Result"), L("SetupCheckMissingCount"), L("SetupCheckExamples"),
            L("SetupCheckFixOn"),
        };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(headerRow, i + 1).Value = headers[i];
        }

        var header = sheet.Range(headerRow, 1, headerRow, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = HeaderFill;

        var r = headerRow + 1;
        foreach (var release in releases)
        {
            foreach (var feature in release.Features)
            {
                sheet.Cell(r, 1).SetValue(release.Version);
                sheet.Cell(r, 2).SetValue(feature.ModuleName);
                sheet.Cell(r, 3).SetValue(feature.Title);
                sheet.Cell(r, 4).SetValue(feature.Description);
                var setup = sheet.Cell(r, 5);
                setup.SetValue(L("FeatureSetupStatus_" + feature.SetupStatus));
                setup.Style.Fill.BackgroundColor = feature.SetupStatus switch
                {
                    FeatureSetupStatus.NeedsSetup => MissingFill,
                    FeatureSetupStatus.CheckFailed => OtherFill,
                    _ => OkFill,
                };
                r++;

                foreach (var item in feature.SetupItems)
                {
                    sheet.Cell(r, 6).SetValue(item.Code);
                    sheet.Cell(r, 7).SetValue(item.Title);
                    var result = sheet.Cell(r, 8);
                    result.SetValue(L("SetupCheckOutcome_" + item.Outcome) + (item.Message == null ? string.Empty : ": " + item.Message));
                    result.Style.Fill.BackgroundColor = item.Outcome switch
                    {
                        SetupCheckOutcome.Ok => OkFill,
                        SetupCheckOutcome.Missing => MissingFill,
                        _ => OtherFill,
                    };
                    sheet.Cell(r, 9).Value = item.MissingCount;
                    sheet.Cell(r, 10).SetValue(string.Join(", ", item.Examples));
                    sheet.Cell(r, 11).SetValue(item.FixScreenName == null ? item.FixScreenCode : $"{item.FixScreenName} ({item.FixScreenCode})");
                    r++;
                }
            }
        }

        sheet.SheetView.FreezeRows(headerRow);
        sheet.Range(headerRow, 1, Math.Max(headerRow, r - 1), headers.Length).SetAutoFilter();
        sheet.Columns(1, headers.Length).AdjustToContents(headerRow, 200);
        foreach (var column in sheet.Columns(1, headers.Length))
        {
            column.Width = Math.Clamp(column.Width, 10, 60);
        }

        foreach (var wrapped in new[] { 4, 7, 10 })
        {
            sheet.Column(wrapped).Style.Alignment.WrapText = true;
        }

        var version = releases.Count == 1 ? releases[0].Version : status?.CurrentVersion;
        var file = new FileDto($"{L("TenantSetupCheck")} {version}.xlsx".Replace("&", "and"),
            MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
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
