using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.Dto;

namespace HCMv3.GridReports.Exporting;

/// <summary>
/// Generic Excel exporter shared by every grid report -- the replacement for the legacy
/// per-report <c>ASPxGridViewExporter</c> control. Callers pass ALL rows matching the current filters
/// (never just the visible page) plus a column list; a <see cref="FileDto"/> token is returned and the
/// Angular side downloads it through <c>/File/DownloadTempFile</c>.
/// </summary>
public interface IGridReportExcelExporter
{
    Task<FileDto> ExportAsync<TRow>(
        string fileName,
        IReadOnlyList<TRow> rows,
        IReadOnlyList<GridReportColumn<TRow>> columns);
}
