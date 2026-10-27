using System;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// One data row of the basic-pay (salary) Excel import, as read from the sheet and before any lookup. The
/// values are kept raw so <see cref="IEmployeeBasicPayImportService"/> can report them back in the legacy
/// wording (<c>"WageType : 'x' Not exists."</c>).
/// </summary>
public class EmployeeBasicPayImportRow
{
    /// <summary>The column headers legacy <c>ImportExcelController</c> read, spelled exactly as legacy.</summary>
    public const string EmployeeCodeColumn = "Employee Code";
    public const string EffectiveDateColumn = "EffectiveDate";
    public const string PayTypeColumn = "PayType";
    public const string PayIntervalColumn = "PayInterval";
    public const string WageTypeColumn = "WageType";
    public const string AmountColumn = "Amount";
    public const string CurrencyColumn = "Currency";

    /// <summary>
    /// Every column, in the order legacy first touched them in a row. A missing column fails the row with the
    /// first one missing in this order, as legacy's <c>DataRow[col]</c> access did.
    /// </summary>
    public static readonly string[] Columns =
    {
        EmployeeCodeColumn,
        EffectiveDateColumn,
        PayTypeColumn,
        PayIntervalColumn,
        WageTypeColumn,
        CurrencyColumn,
        AmountColumn
    };

    public string EmployeeCode { get; set; }

    /// <summary>Set when the cell holds a real Excel date; otherwise <see cref="EffectiveDateText"/> is parsed.</summary>
    public DateTime? EffectiveDateValue { get; set; }

    /// <summary>Text of the cell; parsed as <c>dd-MM-yyyy</c> when it is not a real date.</summary>
    public string EffectiveDateText { get; set; }

    public string PayType { get; set; }

    public string PayInterval { get; set; }

    /// <summary>Matched on <c>WageType.NameEn</c>, not Code -- legacy does the same.</summary>
    public string WageType { get; set; }

    /// <summary>Set when the cell holds a number; otherwise <see cref="AmountText"/> is parsed.</summary>
    public double? AmountValue { get; set; }

    public string AmountText { get; set; }

    /// <summary>Matched on <c>Currency.NameEn</c>, not Code -- legacy does the same.</summary>
    public string Currency { get; set; }
}

/// <summary>What happened to one row of the basic-pay import.</summary>
public enum EmployeeBasicPayImportRowStatus
{
    /// <summary>Blank Employee Code: legacy <c>continue</c>s -- counted in the total, but neither success nor error.</summary>
    Skipped = 0,

    /// <summary>A new detail line was written (and the header created if there was none for that date).</summary>
    Imported = 1,

    /// <summary>
    /// The record for that date already has the wage type. Legacy counts this as a success and changes nothing:
    /// the amount is not updated, and neither carry-forward nor end-dating runs.
    /// </summary>
    AlreadyPresent = 2,

    /// <summary>One or more values did not validate; <see cref="EmployeeBasicPayImportRowResult.Message"/> lists them.</summary>
    Invalid = 3
}

public class EmployeeBasicPayImportRowResult
{
    public EmployeeBasicPayImportRowStatus Status { get; set; }

    /// <summary>For <see cref="EmployeeBasicPayImportRowStatus.Invalid"/>: every message for the row, joined by ", ".</summary>
    public string Message { get; set; }
}
