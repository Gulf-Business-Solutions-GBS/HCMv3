using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// The legacy <c>cmbType</c> combo of <c>uscRptPayrollJV_Custom030.ascx</c>. Five genuinely different
/// sheets, so it is a filter rather than a formatting option.
///
/// This customer's vouchers are laid out as a two-column PIVOT -- "Ops" and "Admin", the two values of
/// CostElement1.Code ("1" and "2") -- rather than as one line per account. That shape is unique to
/// Custom 030 within its family.
/// </summary>
public enum PayrollJVCustom030ReportType
{
    /// <summary>Legacy <c>"Salary JV"</c> (lines 763-1010): wage-type rows plus a Net Pay row.</summary>
    SalaryJV = 0,

    /// <summary>Legacy <c>"Missing Accounts"</c> (lines 1013-1073).</summary>
    MissingAccounts = 1,

    /// <summary>Legacy <c>"EmployeeWise BreakDown"</c> (lines 1075-1141).</summary>
    EmployeeWiseBreakDown = 2,

    /// <summary>Legacy <c>"Indemnity JV"</c> (lines 1143-1408): a four-line balance movement.</summary>
    IndemnityJV = 3,

    /// <summary>Legacy <c>"Annual Leave JV"</c> (lines 1410-1812): a five-line balance movement.</summary>
    AnnualLeaveJV = 4
}

/// <summary>
/// Filter input for the Payroll JV (Custom 030) grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom030.ascx</c>).
///
/// ATYPICAL FILTER PANEL: Payroll Area / Run Type / Month / Year / Type / Employee Code. There is no date
/// range and no employee-status combo, so those two inherited base filters are unused and the Angular
/// screen switches them off rather than forking <c>&lt;grid-report-filter&gt;</c>.
///
/// NOTE: the Indemnity JV and Annual Leave JV sheets IGNORE Payroll Area, Run Type and Employee Code --
/// they re-query from scratch (lines 1157-1163, 1431-1438). Reproduced; see the app service remarks.
/// </summary>
public class GetPayrollJVCustom030ReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>; blank meant the CURRENT month.</summary>
    [Range(1, 12)]
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>; blank meant the CURRENT year.</summary>
    [Range(1900, 2999)]
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c>, taken here as an id rather than the legacy code.</summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>. Null means "all run types" (legacy applied no predicate).</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>Legacy <c>cmbType</c>.</summary>
    public PayrollJVCustom030ReportType ReportType { get; set; }

    /// <summary>
    /// Legacy applied no sort; rows arrive in the order <c>Table1()</c> appended them. That order is
    /// <see cref="PayrollJVCustom030ReportRowDto.SerialNumber"/> here and is the default sort so
    /// server-side paging is deterministic -- which matters more than usual on the two balance sheets,
    /// whose four/five rows are only meaningful in order.
    /// </summary>
    protected override string DefaultSorting => "SerialNumber ASC";
}

/// <summary>
/// One column of the Custom 030 sheet currently selected by
/// <see cref="GetPayrollJVCustom030ReportInput.ReportType"/>. Captions are legacy verbatim (including
/// <c>"Lowwo"</c>, legacy's own misspelling of the Lowvo total) because the customer reconciles the export
/// against the legacy file, so they are exported via <c>GridReportColumn.FromLiteralHeader</c> rather than
/// as localization keys.
/// </summary>
public class PayrollJVCustom030ReportColumnDto
{
    public string Name { get; set; }

    /// <summary>True when the value lives in <see cref="PayrollJVCustom030ReportRowDto.Amounts"/>.</summary>
    public bool IsNumeric { get; set; }
}

public class PayrollJVCustom030ReportOutput : PagedResultDto<PayrollJVCustom030ReportRowDto>
{
    /// <summary>
    /// The column list of the SELECTED report type, in legacy declaration order. Served from the backend so
    /// the grid and the exported xlsx are driven by ONE list and cannot drift apart.
    /// </summary>
    public List<PayrollJVCustom030ReportColumnDto> Columns { get; set; } = new();
}

/// <summary>
/// One row of legacy <c>Table1()</c> (uscRptPayrollJV_Custom030.ascx.cs:558-1815).
///
/// Dictionary-keyed for the same reason as its siblings: the five report types share no column set
/// (8 / 10 / 7 / 5 / 5 columns). The one date column (<c>ForPeriod</c> on Missing Accounts) is carried in
/// <see cref="Values"/> as an invariant <c>yyyy-MM-dd</c> string.
/// </summary>
public class PayrollJVCustom030ReportRowDto : EntityDto<int>
{
    /// <summary>
    /// Legacy "Oid" column. On Salary JV / Missing Accounts / EmployeeWise BreakDown legacy filled it with
    /// a running 1-based counter; on the two balance sheets it wrote a counter it never incremented, so
    /// every row read 0 (lines 1345, 1356, ...). The port always writes the true emit order, because the
    /// column is the page key here rather than decoration.
    /// </summary>
    public int SerialNumber { get; set; }

    /// <summary>Text-valued columns, keyed by the literal legacy caption.</summary>
    public Dictionary<string, string> Values { get; set; } = new();

    /// <summary>Numeric columns, keyed by the literal legacy caption.</summary>
    public Dictionary<string, double> Amounts { get; set; } = new();
}
