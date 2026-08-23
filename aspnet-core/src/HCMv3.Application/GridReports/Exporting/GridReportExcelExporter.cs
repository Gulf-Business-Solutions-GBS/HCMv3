using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Abp.AspNetZeroCore.Net;
using Abp.Dependency;
using HCMv3.Dto;
using HCMv3.Storage;
using MiniExcelLibs;

namespace HCMv3.GridReports.Exporting;

/// <inheritdoc cref="IGridReportExcelExporter"/>
/// <remarks>
/// Uses MiniExcel + <see cref="ITempFileCacheManager"/>, i.e. exactly the same plumbing as the
/// repository's existing <c>AuditLogListExcelExporter</c>/<c>UserListExcelExporter</c> (see
/// <c>MiniExcelExcelExporterBase</c>) -- rather than ClosedXML -- so grid reports download through the
/// standard <c>/File/DownloadTempFile</c> endpoint like every other export in the app.
///
/// It writes a <see cref="DataTable"/> instead of the base class's
/// <c>List&lt;Dictionary&lt;string, object&gt;&gt;</c> for two reasons that matter for reports:
/// a DataTable keeps column ORDER and TYPE (dates stay real dates in Excel), and it still emits the
/// header row when the filtered result set is empty -- the dictionary path cannot infer columns from
/// zero rows and throws.
/// </remarks>
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
        IReadOnlyList<GridReportColumn<TRow>> columns)
    {
        var file = new FileDto(fileName, MimeTypeNames.ApplicationVndOpenxmlformatsOfficedocumentSpreadsheetmlSheet);

        var table = BuildDataTable(rows, columns);

        using (var stream = new MemoryStream())
        {
            await stream.SaveAsAsync(table, printHeader: true);
            _tempFileCacheManager.SetFile(file.FileToken, stream.ToArray());
        }

        return file;
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
