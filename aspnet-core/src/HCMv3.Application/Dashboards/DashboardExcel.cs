using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace HCMv3.Dashboards;

/// <summary>
/// Sheet-building helpers every dashboard's Excel export uses, so all dashboard workbooks look alike: a bold
/// title on row 1, a shaded header on row 3 with an auto-filter, frozen header, and columns sized to fit.
/// Import with <c>using static HCMv3.Dashboards.DashboardExcel;</c>.
/// </summary>
public static class DashboardExcel
{
    public const string DateFormat = "dd/MM/yyyy";

    /// <summary>The header row every sheet uses; data starts on the row after it.</summary>
    public const int HeaderRow = 3;

    private static readonly XLColor HeaderFill = XLColor.FromHtml("#E9EEF6");

    public static void Title(IXLWorksheet sheet, string title)
    {
        sheet.Cell(1, 1).SetValue(title);
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
    }

    public static void Header(IXLWorksheet sheet, int row, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            sheet.Cell(row, i + 1).SetValue(headers[i]);
        }

        var range = sheet.Range(row, 1, row, headers.Count);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = HeaderFill;
    }

    public static void Finish(IXLWorksheet sheet, int columns, int lastRow)
    {
        if (lastRow >= 3)
        {
            sheet.Range(3, 1, lastRow, columns).SetAutoFilter();
        }

        sheet.SheetView.FreezeRows(3);
        sheet.Columns(1, columns).AdjustToContents(3, Math.Min(lastRow, 500));
    }

    public static void SetCell(IXLCell cell, object value)
    {
        switch (value)
        {
            case null:
                return;
            case DateTime date:
                cell.SetValue(date);
                cell.Style.DateFormat.Format = DateFormat;
                return;
            case int number:
                cell.SetValue(number);
                return;
            case double number:
                cell.SetValue(number);
                return;
            default:
                cell.SetValue(value.ToString());
                return;
        }
    }

    public static string Pick(bool arabic, string en, string ar) =>
        arabic ? (string.IsNullOrWhiteSpace(ar) ? en : ar) : (string.IsNullOrWhiteSpace(en) ? ar : en);

    /// <summary>Excel refuses sheet names over 31 characters, repeated names, or any of : \ / ? * [ ].</summary>
    public static string Sheet(string name, XLWorkbook book = null)
    {
        var clean = new string((name ?? "Sheet").Where(c => !":\\/?*[]".Contains(c)).ToArray()).Trim();
        if (clean.Length == 0)
        {
            clean = "Sheet";
        }

        clean = clean.Length > 31 ? clean[..31] : clean;

        if (book == null)
        {
            return clean;
        }

        var candidate = clean;
        for (var i = 2; book.Worksheets.Any(w => string.Equals(w.Name, candidate, StringComparison.OrdinalIgnoreCase)); i++)
        {
            var suffix = " " + i;
            candidate = (clean.Length + suffix.Length > 31 ? clean[..(31 - suffix.Length)] : clean) + suffix;
        }

        return candidate;
    }
}
