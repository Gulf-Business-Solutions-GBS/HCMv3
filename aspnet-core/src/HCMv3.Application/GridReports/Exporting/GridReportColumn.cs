using System;

namespace HCMv3.GridReports.Exporting;

/// <summary>
/// One exported column of a grid report: the localization key used for the Excel header cell plus a
/// selector producing the cell value from a row DTO.
///
/// This is the ONLY place a report declares its export shape. The legacy
/// <c>ASPxGridViewExporter</c> exported whatever the grid was showing; here the report declares the
/// same column list once and both the Angular table headers and this exporter follow it.
/// </summary>
public class GridReportColumn<TRow>
{
    public GridReportColumn(string localizationKey, Func<TRow, object> valueSelector)
    {
        LocalizationKey = localizationKey;
        ValueSelector = valueSelector;
    }

    /// <summary>
    /// Creates a column whose header is ALREADY-RESOLVED TEXT rather than a localization key. Needed only
    /// by pivot reports whose columns come from data (e.g. one column per wage type in the Employee
    /// Salary Change report): such a header can never be a key, and passing it as one would make ABP
    /// render it as "[Basic Salary]" -- its not-found fallback wraps unknown keys in brackets.
    /// </summary>
    public static GridReportColumn<TRow> FromLiteralHeader(string header, Func<TRow, object> valueSelector)
    {
        return new GridReportColumn<TRow>(header, valueSelector) { LiteralHeader = header };
    }

    public string LocalizationKey { get; }

    /// <summary>Non-null on data-driven pivot columns; see <see cref="FromLiteralHeader"/>.</summary>
    public string LiteralHeader { get; private init; }

    public Func<TRow, object> ValueSelector { get; }
}
