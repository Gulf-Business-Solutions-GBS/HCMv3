namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// ONE COLUMN of a customer-specific Journal Voucher grid report.
///
/// The JV family (legacy <c>uscRptPayrollJV_Custom0NN.ascx</c>) is unusual: several of the variants build
/// a DIFFERENT <c>DataTable</c> -- different columns, in a different order -- depending on the legacy
/// <c>cmbType</c> combo ("Main Format", "Provision Format", "Missing Accounts", "EmployeeWise BreakDown",
/// ...). A JV app service therefore returns a superset row DTO plus THIS list, describing exactly which
/// fields the chosen format shows and in which order. The Angular grid and the exported xlsx are both
/// driven from the same server-side list, so they cannot drift apart.
/// </summary>
public class PayrollJVReportColumnDto
{
    public PayrollJVReportColumnDto()
    {
    }

    public PayrollJVReportColumnDto(string field, string header, bool localize = true)
    {
        Field = field;
        Header = header;
        Localize = localize;
    }

    /// <summary>camelCase row-DTO property name the Angular grid reads.</summary>
    public string Field { get; set; }

    /// <summary>Localization key (when <see cref="Localize"/>) or already-resolved caption.</summary>
    public string Header { get; set; }

    /// <summary>
    /// False for captions that are DATA rather than keys. Passing data through the localize pipe would
    /// render it as <c>[Some Text]</c> -- ABP's not-found fallback wraps unknown keys in brackets.
    /// </summary>
    public bool Localize { get; set; } = true;
}
