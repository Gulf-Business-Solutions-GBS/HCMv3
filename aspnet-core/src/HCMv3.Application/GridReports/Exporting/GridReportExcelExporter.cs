using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.AspNetZeroCore.Net;
using Abp.Dependency;
using HCMv3.Dto;
using HCMv3.Storage;
using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using MiniExcelLibs.OpenXml;

namespace HCMv3.GridReports.Exporting;

/// <summary>
/// Writes a grid report's rows to an .xlsx through MiniExcel. Numeric columns get the tenant's
/// <c>DecimalInReports</c> as their cell format, which is how legacy's <c>ASPxGridViewExporter</c> carried the
/// grid's <c>N{decimals}</c> display format into the sheet: the stored value stays exact, only the display
/// rounds.
/// </summary>
public class GridReportExcelExporter : HCMv3ServiceBase, IGridReportExcelExporter, ITransientDependency
{
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public GridReportExcelExporter(ITempFileCacheManager tempFileCacheManager)
    {
        _tempFileCacheManager = tempFileCacheManager;
    }

    public async Task<FileDto> ExportAsync<TRow>(
        string fileName,
        IReadOnlyList<TRow> rows,
        IReadOnlyList<GridReportColumn<TRow>> columns,
        int decimalPlaces = IGridReportExcelExporter.DefaultDecimalPlaces)
    {
        var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);
        var table = BuildDataTable(rows, columns);
        var configuration = BuildNumberFormats(table, decimalPlaces);

        using (var stream = new MemoryStream())
        {
            await stream.SaveAsAsync(table, printHeader: true, configuration: configuration);
            _tempFileCacheManager.SetFile(file.FileToken, stream.ToArray());
        }

        return file;
    }

    /// <summary>
    /// A column whose values are all fractional numbers is formatted to <paramref name="decimalPlaces"/>;
    /// text, dates and integers are left alone.
    /// </summary>
    private static OpenXmlConfiguration BuildNumberFormats(DataTable table, int decimalPlaces)
    {
        var decimals = decimalPlaces <= 0 ? IGridReportExcelExporter.DefaultDecimalPlaces : decimalPlaces;
        var format = "#,##0." + new string('0', decimals);

        var numericColumns = new List<DynamicExcelColumn>();
        foreach (DataColumn column in table.Columns)
        {
            var values = table.Rows.Cast<DataRow>()
                .Select(r => r[column])
                .Where(v => v != null && v != DBNull.Value)
                .ToList();

            if (values.Count > 0 && values.All(v => v is double || v is decimal || v is float))
            {
                numericColumns.Add(new DynamicExcelColumn(column.ColumnName) { Format = format });
            }
        }

        return numericColumns.Count == 0
            ? null
            : new OpenXmlConfiguration { DynamicColumns = numericColumns.ToArray() };
    }

    private DataTable BuildDataTable<TRow>(
        IReadOnlyList<TRow> rows,
        IReadOnlyList<GridReportColumn<TRow>> columns)
    {
        var table = new DataTable();

        foreach (var column in columns)
        {
            // Excel column headers must be unique inside a DataTable; suffix duplicates rather than
            // letting DataTable throw on two report columns that localize to the same caption.
            // Pivot reports supply already-resolved headers (one column per wage type, etc.); everything
            // else supplies a localization key.
            var header = column.LiteralHeader ?? L(column.LocalizationKey);
            var uniqueHeader = header;
            var suffix = 2;
            while (table.Columns.Contains(uniqueHeader))
            {
                uniqueHeader = header + " (" + suffix++ + ")";
            }

            table.Columns.Add(new DataColumn(uniqueHeader, typeof(object)));
        }

        foreach (var row in rows)
        {
            var dataRow = table.NewRow();
            for (var i = 0; i < columns.Count; i++)
            {
                dataRow[i] = columns[i].ValueSelector(row) ?? (object)DBNull.Value;
            }

            table.Rows.Add(dataRow);
        }

        return table;
    }
}
