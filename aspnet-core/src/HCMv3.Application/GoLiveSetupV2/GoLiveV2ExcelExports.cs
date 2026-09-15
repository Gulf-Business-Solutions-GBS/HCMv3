using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Abp;
using Abp.AspNetZeroCore.Net;
using Abp.Dependency;
using ClosedXML.Excel;
using HCMv3.Dto;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.Storage;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// The two workbooks V2 hands to a customer: the employee sheet to fill in, and the legacy comparison to check.
///
/// <para>Both are drawn with ClosedXML rather than the plain grid-report exporter because each needs what a
/// plain table cannot carry: the employee sheet keeps the civil ID as text and offers the company's banks as a
/// list, and the comparison colours each legacy/HCMv3 pair green or red the way the screen does.</para>
///
/// <para>Both write only what the screen sends. The comparison holds salaries: it is made in memory and handed
/// to the temporary file store the download reads from, never saved anywhere else.</para>
/// </summary>
public class GoLiveV2ExcelExports : AbpServiceBase, ITransientDependency
{
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#E9EEF6");
    private static readonly XLColor MatchFill = XLColor.FromHtml("#DDF0E4");
    private static readonly XLColor MatchText = XLColor.FromHtml("#0F5132");
    private static readonly XLColor DifferFill = XLColor.FromHtml("#F8D7DA");
    private static readonly XLColor DifferText = XLColor.FromHtml("#842029");

    private readonly ITempFileCacheManager _tempFiles;

    public GoLiveV2ExcelExports(ITempFileCacheManager tempFiles)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
        _tempFiles = tempFiles;
    }

    /// <summary>
    /// The employee sheet in the grid's own columns and order, so what the customer fills in pastes straight back
    /// into Load Employee Data. Header only when there are no rows: a blank sheet to start from.
    /// </summary>
    public FileDto EmployeeSheet(IReadOnlyList<GoLiveV2EmployeeRowDto> rows, IReadOnlyList<string> bankNames)
    {
        using var book = new XLWorkbook();
        var sheet = book.AddWorksheet(Sheet(L("GoLiveV2ExportEmployeesSheet")));

        var headers = new[]
        {
            "GoLiveV2ColCode", "GoLiveV2ColFullName", "GoLiveV2ColJoiningDate", "GoLiveV2ColCivilId",
            "GoLiveV2ColBasicSalary", "GoLiveV2ColAccommodation", "GoLiveV2ColMobile", "GoLiveV2ColTransport",
            "GoLiveV2ColFood", "GoLiveV2ColHousing", "GoLiveV2ColOther", "GoLiveV2ColAnnualLeaveOpening",
            "GoLiveV2ColBank", "GoLiveV2ColIban", "GoLiveV2ColAccountNumber",
        };
        WriteHeader(sheet, headers.Select(h => L(h)).ToArray());

        // The text columns stay text however long the number in them: a civil ID or an account number shown as
        // 2.90012E+11 cannot be pasted back.
        foreach (var textColumn in new[] { 1, 2, 4, 13, 14, 15 })
        {
            sheet.Column(textColumn).Style.NumberFormat.Format = "@";
        }

        sheet.Column(3).Style.DateFormat.Format = "dd-mm-yyyy";
        foreach (var amountColumn in Enumerable.Range(5, 8))
        {
            sheet.Column(amountColumn).Style.NumberFormat.Format = "#,##0.000";
        }

        var r = 2;
        foreach (var row in rows)
        {
            Text(sheet.Cell(r, 1), row.Code);
            Text(sheet.Cell(r, 2), row.FullName);
            if (row.JoiningDate.HasValue)
            {
                sheet.Cell(r, 3).Value = row.JoiningDate.Value.Date;
            }

            Text(sheet.Cell(r, 4), row.CivilId);
            Number(sheet.Cell(r, 5), row.BasicSalary);
            Number(sheet.Cell(r, 6), row.AccommodationAllowance);
            Number(sheet.Cell(r, 7), row.MobileAllowance);
            Number(sheet.Cell(r, 8), row.TransportAllowance);
            Number(sheet.Cell(r, 9), row.FoodAllowance);
            Number(sheet.Cell(r, 10), row.HousingAllowance);
            Number(sheet.Cell(r, 11), row.OtherAllowance);
            Number(sheet.Cell(r, 12), row.AnnualLeaveOpeningBalance);
            Text(sheet.Cell(r, 13), row.BankName);
            Text(sheet.Cell(r, 14), row.Iban);
            Text(sheet.Cell(r, 15), row.AccountNumber);
            r++;
        }

        // The Bank column offers the company's banks, from a sheet of their own, for as many rows as a customer
        // is likely to add.
        if (bankNames.Count > 0)
        {
            var banks = book.AddWorksheet(Sheet(L("GoLiveV2ExportBanksSheet")));
            for (var i = 0; i < bankNames.Count; i++)
            {
                banks.Cell(i + 1, 1).Value = bankNames[i];
            }

            banks.Column(1).AdjustToContents();
            var list = sheet.Range(2, 13, Math.Max(r, 2) + 1000, 13).CreateDataValidation();
            list.List(banks.Range(1, 1, bankNames.Count, 1));
            list.IgnoreBlanks = true;
        }

        Finish(sheet, headers.Length);
        return Save(book, L("GoLiveV2ExportEmployeesFileName"));
    }

    /// <summary>
    /// The comparison as the Lines tab shows it -- one row per employee and wage type, each legacy/HCMv3 pair
    /// coloured -- and a Summary sheet with the employee and wage type counts.
    /// </summary>
    public FileDto LegacyComparison(IReadOnlyList<GoLiveV2LegacyCompareLineDto> lines, DateTime? payMonth)
    {
        using var book = new XLWorkbook();
        var monthText = payMonth?.ToString("MMMM yyyy") ?? string.Empty;

        // ---- Summary
        var summary = book.AddWorksheet(Sheet(L("GoLiveV2CompareTabSummary")));
        summary.Cell(1, 1).Value = L("GoLiveV2CompareTitle", monthText);
        summary.Cell(1, 1).Style.Font.Bold = true;
        summary.Cell(1, 1).Style.Font.FontSize = 14;
        summary.Cell(2, 1).Value = L("GoLiveV2CompareMatchRule");
        summary.Cell(2, 1).Style.Font.Italic = true;

        var employees = lines.GroupBy(l => l.EmployeeCode).ToList();
        var employeesMatched = employees.Count(g => g.All(l => l.Status == GoLiveV2CompareStatus.Matched));
        var wageTypes = lines.GroupBy(l => l.WageTypeCode).OrderBy(g => g.Key).ToList();
        var wageTypesMatched = wageTypes.Count(g => g.All(l => l.Status == GoLiveV2CompareStatus.Matched));

        var counts = new (string Label, int Value)[]
        {
            (L("GoLiveV2CompareEmployeesTotal"), employees.Count),
            (L("GoLiveV2CompareEmployeesMatched"), employeesMatched),
            (L("GoLiveV2CompareEmployeesNotMatched"), employees.Count - employeesMatched),
            (L("GoLiveV2CompareWageTypesTotal"), wageTypes.Count),
            (L("GoLiveV2CompareWageTypesMatched"), wageTypesMatched),
            (L("GoLiveV2CompareWageTypesNotMatched"), wageTypes.Count - wageTypesMatched),
        };
        for (var i = 0; i < counts.Length; i++)
        {
            summary.Cell(4 + i, 1).Value = counts[i].Label;
            summary.Cell(4 + i, 2).Value = counts[i].Value;
            summary.Cell(4 + i, 2).Style.Font.Bold = true;
        }

        // The headline: the share of employees whose whole salary matched.
        var percentRow = 4 + counts.Length;
        summary.Cell(percentRow, 1).Value = L("GoLiveV2CompareEmployeesMatchedPercent");
        summary.Cell(percentRow, 2).Value = employees.Count == 0 ? 0 : (double)employeesMatched / employees.Count;
        summary.Cell(percentRow, 2).Style.NumberFormat.Format = "0.0%";
        summary.Cell(percentRow, 2).Style.Font.Bold = true;

        // The wage types that did not match, worst first, and on how many lines.
        var listRow = percentRow + 2;
        summary.Cell(listRow, 1).Value = L("GoLiveV2CompareWageTypesNotMatched");
        summary.Cell(listRow, 1).Style.Font.Bold = true;
        var listHeader = listRow + 1;
        summary.Cell(listHeader, 1).Value = L("GoLiveV2CompareColWageTypeCode");
        summary.Cell(listHeader, 2).Value = L("GoLiveV2CompareColWageTypeName");
        summary.Cell(listHeader, 3).Value = L("GoLiveV2CompareColLinesDiffering");
        summary.Range(listHeader, 1, listHeader, 3).Style.Font.Bold = true;
        summary.Range(listHeader, 1, listHeader, 3).Style.Fill.BackgroundColor = HeaderFill;

        var next = listHeader + 1;
        foreach (var g in wageTypes
                     .Select(g => new
                     {
                         Code = g.Key,
                         Name = g.First().WageTypeName,
                         Lines = g.Count(),
                         Differing = g.Count(l => l.Status != GoLiveV2CompareStatus.Matched),
                     })
                     .Where(w => w.Differing > 0)
                     .OrderByDescending(w => w.Differing)
                     .ThenBy(w => w.Code))
        {
            Text(summary.Cell(next, 1), g.Code);
            Text(summary.Cell(next, 2), g.Name);
            Text(summary.Cell(next, 3), $"{g.Differing} / {g.Lines}");
            next++;
        }

        summary.Columns(1, 3).AdjustToContents();
        summary.Column(1).Width = Math.Min(summary.Column(1).Width, 40);

        // ---- Lines
        var sheet = book.AddWorksheet(Sheet(L("GoLiveV2CompareTabLines")));
        var headers = new[]
        {
            "GoLiveV2CompareColCode", "GoLiveV2CompareColWageTypeCode", "GoLiveV2CompareColWageTypeName",
            "GoLiveV2CompareColRateLegacy", "GoLiveV2CompareColRateNew",
            "GoLiveV2CompareColNumberLegacy", "GoLiveV2CompareColNumberNew",
            "GoLiveV2CompareColAmountLegacy", "GoLiveV2CompareColAmountNew",
            "GoLiveV2CompareColStatus", "GoLiveV2CompareColPossibleReason",
        };
        WriteHeader(sheet, headers.Select(h => L(h)).ToArray());
        sheet.Columns(1, 3).Style.NumberFormat.Format = "@";
        sheet.Columns(4, 7).Style.NumberFormat.Format = "#,##0.######";
        sheet.Columns(8, 9).Style.NumberFormat.Format = "#,##0.000";

        var r = 2;
        foreach (var line in lines)
        {
            Text(sheet.Cell(r, 1), line.EmployeeCode);
            Text(sheet.Cell(r, 2), line.WageTypeCode);
            Text(sheet.Cell(r, 3), line.WageTypeName);

            Pair(sheet, r, 4, line.LegacyRate, line.Hcmv3Rate, line.RateMatches);
            Pair(sheet, r, 6, line.LegacyNumber, line.Hcmv3Number, line.NumberMatches);
            Pair(sheet, r, 8, line.LegacyAmount, line.Hcmv3Amount, line.AmountMatches);

            var matched = line.Status == GoLiveV2CompareStatus.Matched;
            var status = sheet.Cell(r, 10);
            status.Value = matched ? L("GoLiveV2CompareMatchedCount") : L("GoLiveV2CompareNotMatched");
            status.Style.Font.FontColor = matched ? MatchText : DifferText;
            status.Style.Font.Bold = true;

            if (!matched)
            {
                var cause = line.Cause == GoLiveV2CompareCause.None
                    ? string.Empty
                    : L("GoLiveV2CompareCause" + line.Cause) + ": ";
                Text(sheet.Cell(r, 11), cause + line.Reason);
            }

            r++;
        }

        Finish(sheet, headers.Length);
        sheet.Column(11).Width = 80;
        sheet.Column(11).Style.Alignment.WrapText = true;

        return Save(book, L("GoLiveV2ExportComparisonFileName", payMonth?.ToString("yyyy-MM") ?? string.Empty));
    }

    private static void Pair(IXLWorksheet sheet, int row, int column, decimal? legacy, decimal? hcmv3, bool matches)
    {
        Number(sheet.Cell(row, column), legacy);
        Number(sheet.Cell(row, column + 1), hcmv3);

        var range = sheet.Range(row, column, row, column + 1);
        range.Style.Fill.BackgroundColor = matches ? MatchFill : DifferFill;
        range.Style.Font.FontColor = matches ? MatchText : DifferText;
        if (!matches)
        {
            range.Style.Font.Bold = true;
        }
    }

    private static void WriteHeader(IXLWorksheet sheet, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var header = sheet.Range(1, 1, 1, headers.Count);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = HeaderFill;
        header.Style.Alignment.WrapText = true;
    }

    private static void Finish(IXLWorksheet sheet, int columns)
    {
        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, Math.Max(1, sheet.LastRowUsed()?.RowNumber() ?? 1), columns).SetAutoFilter();
        sheet.Columns(1, columns).AdjustToContents(1, 200);
        foreach (var column in sheet.Columns(1, columns))
        {
            column.Width = Math.Clamp(column.Width, 12, 45);
        }
    }

    private FileDto Save(XLWorkbook book, string fileName)
    {
        var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        _tempFiles.SetFile(file.FileToken, stream.ToArray());
        return file;
    }

    private static void Text(IXLCell cell, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            cell.SetValue(value);
        }
    }

    private static void Number(IXLCell cell, double? value)
    {
        if (value.HasValue)
        {
            cell.Value = value.Value;
        }
    }

    private static void Number(IXLCell cell, decimal? value)
    {
        if (value.HasValue)
        {
            cell.Value = value.Value;
        }
    }

    /// <summary>Excel refuses sheet names over 31 characters or with any of : \ / ? * [ ].</summary>
    private static string Sheet(string name)
    {
        var clean = new string((name ?? "Sheet").Where(c => !":\\/?*[]".Contains(c)).ToArray());
        return clean.Length > 31 ? clean[..31] : clean;
    }
}
