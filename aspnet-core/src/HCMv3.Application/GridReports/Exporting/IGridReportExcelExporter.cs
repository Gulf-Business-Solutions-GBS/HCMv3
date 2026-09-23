using System.Collections.Generic;
using System.Threading.Tasks;
using HCMv3.Dto;

namespace HCMv3.GridReports.Exporting;

public interface IGridReportExcelExporter
{
    /// <summary>Legacy grid export default: three decimals (<c>GridReportHelper.GetDecimalRounding</c>).</summary>
    const int DefaultDecimalPlaces = 3;

    /// <param name="decimalPlaces">
    /// Decimals shown on every numeric column; legacy applied <c>ManageReportSettings.DecimalInReports</c>
    /// as an <c>N{decimals}</c> display format, zero meaning three.
    /// </param>
    Task<FileDto> ExportAsync<TRow>(
        string fileName,
        IReadOnlyList<TRow> rows,
        IReadOnlyList<GridReportColumn<TRow>> columns,
        int decimalPlaces = DefaultDecimalPlaces);
}
