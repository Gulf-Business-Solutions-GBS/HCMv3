using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Abp.Dependency;
using ClosedXML.Excel;

namespace HCMv3.GoLiveSetup;

/// <summary>One row of the legacy salary register export, as read from the file.</summary>
public class GoLiveLegacyRow
{
    public string EmployeeCode { get; set; }

    public DateTime? PayMonth { get; set; }

    public string WageTypeCode { get; set; }

    public string WageTypeName { get; set; }

    public string BillingType { get; set; }

    public decimal? Rate { get; set; }

    public decimal? Number { get; set; }

    public decimal? Amount { get; set; }
}

/// <summary>What could be read from the file: its rows, and how many had no employee or wage type code.</summary>
public class GoLiveLegacyFile
{
    public List<GoLiveLegacyRow> Rows { get; } = new();

    public int Unreadable { get; set; }
}

/// <summary>
/// Reads the legacy export however it was saved: an Excel workbook, or text separated by commas, tabs,
/// semicolons or pipes -- with the header row or without, because SQL Server Management Studio leaves the
/// headers out of "Save Results As" unless told otherwise. Without headers the columns are taken in the
/// query's own order. SSMS writes an empty value as the word NULL; that reads as blank.
///
/// <para>The file holds salaries. It is read in memory, and nothing of it is logged or kept.</para>
/// </summary>
public class GoLiveLegacyFileReader : ITransientDependency
{
    /// <summary>The query's columns, in the order it selects them.</summary>
    public static readonly string[] Columns =
    {
        "EmployeeCode", "PayMonth", "WageTypeCode", "WageTypeName",
        "BillingType", "Rate", "Number", "Amount",
    };

    public GoLiveLegacyFile Read(string fileName, byte[] content)
    {
        var table = (fileName ?? string.Empty).EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? ReadWorkbook(content)
            : ReadText(content);

        return ToRows(table);
    }

    private static GoLiveLegacyFile ToRows(List<string[]> table)
    {
        var file = new GoLiveLegacyFile();
        if (table.Count == 0)
        {
            return file;
        }

        // Headers when the first row names the columns; otherwise the query's order.
        var first = table[0].Select(Normalise).ToList();
        var hasHeaders = first.Contains("employeecode") && first.Contains("wagetypecode");
        var index = Columns.ToDictionary(
            c => c,
            c => hasHeaders ? first.IndexOf(c.ToLowerInvariant()) : Array.IndexOf(Columns, c));

        foreach (var cells in table.Skip(hasHeaders ? 1 : 0))
        {
            if (cells.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            string Cell(string column)
            {
                var i = index[column];
                var value = i >= 0 && i < cells.Length ? cells[i]?.Trim() : null;
                return string.IsNullOrEmpty(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase) ? null : value;
            }

            var row = new GoLiveLegacyRow
            {
                EmployeeCode = Cell("EmployeeCode"),
                PayMonth = Date(Cell("PayMonth")),
                WageTypeCode = Cell("WageTypeCode"),
                WageTypeName = Cell("WageTypeName"),
                BillingType = Cell("BillingType"),
                Rate = Number(Cell("Rate")),
                Number = Number(Cell("Number")),
                Amount = Number(Cell("Amount")),
            };

            if (row.EmployeeCode == null || row.WageTypeCode == null)
            {
                file.Unreadable++;
                continue;
            }

            file.Rows.Add(row);
        }

        return file;
    }

    private static List<string[]> ReadWorkbook(byte[] content)
    {
        var table = new List<string[]>();
        using var stream = new MemoryStream(content);
        using var book = new XLWorkbook(stream);

        var used = book.Worksheets.FirstOrDefault()?.RangeUsed();
        if (used == null)
        {
            return table;
        }

        foreach (var row in used.Rows())
        {
            table.Add(row.Cells(1, used.ColumnCount()).Select(CellText).ToArray());
        }

        return table;
    }

    /// <summary>A number at full precision, a date as yyyy-MM-dd -- never as the sheet happens to format it.</summary>
    private static string CellText(IXLCell cell)
    {
        var value = cell.Value;
        if (value.IsNumber)
        {
            return value.GetNumber().ToString("R", CultureInfo.InvariantCulture);
        }

        if (value.IsDateTime)
        {
            return value.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return value.IsBlank ? string.Empty : value.ToString(CultureInfo.InvariantCulture);
    }

    private static List<string[]> ReadText(byte[] content)
    {
        var text = new StreamReader(new MemoryStream(content), Encoding.UTF8, detectEncodingFromByteOrderMarks: true).ReadToEnd();
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToList();
        if (lines.Count == 0)
        {
            return new List<string[]>();
        }

        // The separator is whichever of these splits the first line into the most columns.
        var separator = new[] { ',', '\t', ';', '|' }
            .OrderByDescending(s => Split(lines[0], s).Length)
            .First();

        return lines.Select(l => Split(l, separator)).ToList();
    }

    /// <summary>One line into cells, keeping a separator inside double quotes, and "" as a quote.</summary>
    private static string[] Split(string line, char separator)
    {
        var cells = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    cell.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == separator)
            {
                cells.Add(cell.ToString());
                cell.Clear();
            }
            else
            {
                cell.Append(c);
            }
        }

        cells.Add(cell.ToString());
        return cells.ToArray();
    }

    private static string Normalise(string header) =>
        new string((header ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static decimal? Number(string text) =>
        text != null && decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static DateTime? Date(string text)
    {
        if (text == null)
        {
            return null;
        }

        var formats = new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.fff", "dd/MM/yyyy", "dd-MM-yyyy" };
        return DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.Date
            : null;
    }
}
