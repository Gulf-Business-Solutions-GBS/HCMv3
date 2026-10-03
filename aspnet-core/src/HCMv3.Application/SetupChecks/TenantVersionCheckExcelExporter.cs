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
/// A build comparison as a workbook: one sheet of setup items, status coloured as on screen, and one of every change.
/// Made in memory and handed to the temporary file store the download reads from.
/// </summary>
public class TenantVersionCheckExcelExporter : AbpServiceBase, ITransientDependency
{
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#E9EEF6");
    private static readonly XLColor DoneFill = XLColor.FromHtml("#DDF0E4");
    private static readonly XLColor ActionFill = XLColor.FromHtml("#F8D7DA");
    private static readonly XLColor ReviewFill = XLColor.FromHtml("#FFF3CD");

    private readonly ITempFileCacheManager _tempFiles;

    public TenantVersionCheckExcelExporter(ITempFileCacheManager tempFiles)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
        _tempFiles = tempFiles;
    }

    public FileDto Export(BuildComparisonDto comparison)
    {
        using var book = new XLWorkbook();

        // ---- setup items
        var setup = book.AddWorksheet(Sheet(L("SetupCheckSetupRequired")));
        var heading = $"{L("TenantVersionCheck")}: {comparison.FromVersion} - {comparison.ToVersion}";
        setup.Cell(1, 1).Value = heading;
        setup.Cell(1, 1).Style.Font.Bold = true;
        setup.Cell(1, 1).Style.Font.FontSize = 14;

        const int headerRow = 3;
        var setupHeaders = new[]
        {
            L("Status"), L("Type"), L("SetupCheckWhatWasAdded"), L("SetupCheckWhatToDo"), L("SetupCheckTechnicalName"),
            L("Version"), L("SetupCheckCameWith"), L("SetupCheckFixOn"),
        };
        WriteHeader(setup, headerRow, setupHeaders);

        var r = headerRow + 1;
        foreach (var item in comparison.SetupItems)
        {
            var status = setup.Cell(r, 1);
            status.SetValue(L("SetupItemStatus_" + item.Status));
            if (item.Status != SetupItemStatus.Info)
            {
                status.Style.Fill.BackgroundColor = item.Status switch
                {
                    SetupItemStatus.ActionRequired => ActionFill,
                    SetupItemStatus.Review => ReviewFill,
                    _ => DoneFill,
                };
            }

            setup.Cell(r, 2).SetValue(L("SetupItemKind_" + item.Kind));
            setup.Cell(r, 3).SetValue(item.Title);
            setup.Cell(r, 4).SetValue(item.Detail);
            setup.Cell(r, 5).SetValue(item.TechnicalName);
            setup.Cell(r, 6).SetValue(item.Version);
            setup.Cell(r, 7).SetValue(item.ChangeSubject);
            setup.Cell(r, 8).SetValue(item.FixScreenName);
            r++;
        }

        Finish(setup, headerRow, r, setupHeaders.Length);

        // ---- every change
        var changes = book.AddWorksheet(Sheet(L("SetupCheckAllChanges")));
        var changeHeaders = new[] { L("Version"), L("Date"), L("Module"), L("SetupCheckChange"), L("SetupCheckSetupItems") };
        WriteHeader(changes, 1, changeHeaders);

        r = 2;
        foreach (var change in comparison.Changes)
        {
            changes.Cell(r, 1).SetValue(change.Version);
            changes.Cell(r, 2).Value = change.Date;
            changes.Cell(r, 2).Style.DateFormat.Format = "dd-mm-yyyy";
            changes.Cell(r, 3).SetValue(change.ModuleName);
            changes.Cell(r, 4).SetValue(change.Subject);
            if (change.SetupItemCount > 0)
            {
                changes.Cell(r, 5).Value = change.SetupItemCount;
            }

            r++;
        }

        Finish(changes, 1, r, changeHeaders.Length);

        var file = new FileDto($"{L("TenantVersionCheck")} {comparison.FromVersion} - {comparison.ToVersion}.xlsx",
            MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        _tempFiles.SetFile(file.FileToken, stream.ToArray());
        return file;
    }

    private static void WriteHeader(IXLWorksheet sheet, int row, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(row, i + 1).Value = headers[i];
        }

        var header = sheet.Range(row, 1, row, headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = HeaderFill;
    }

    private static void Finish(IXLWorksheet sheet, int headerRow, int nextRow, int columns)
    {
        sheet.SheetView.FreezeRows(headerRow);
        sheet.Range(headerRow, 1, Math.Max(headerRow, nextRow - 1), columns).SetAutoFilter();
        sheet.Columns(1, columns).AdjustToContents(headerRow, 200);
        foreach (var column in sheet.Columns(1, columns))
        {
            column.Width = Math.Clamp(column.Width, 10, 70);
        }
    }

    /// <summary>Excel refuses sheet names over 31 characters or with any of : \ / ? * [ ].</summary>
    private static string Sheet(string name)
    {
        var clean = new string((name ?? "Sheet").Where(c => !":\\/?*[]".Contains(c)).ToArray());
        return clean.Length > 31 ? clean[..31] : clean;
    }
}
