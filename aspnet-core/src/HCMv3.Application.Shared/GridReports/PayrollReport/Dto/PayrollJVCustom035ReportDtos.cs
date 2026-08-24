using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// The legacy <c>cmbType</c> combo of <c>uscRptPayrollJV_Custom035.ascx</c>. Each value produces a
/// different sheet, so it is a filter rather than a formatting option. Legacy returned an empty table when
/// the combo was blank (uscRptPayrollJV_Custom035.ascx.cs:563-566); here the value is required and
/// defaults to <see cref="MainFormat"/>.
///
/// Legacy's fourth option, "Provision Format", is COMMENTED OUT in its entirety (lines 873-960) and is
/// therefore not ported -- selecting it in legacy produced an empty table.
/// </summary>
public enum PayrollJVCustom035ReportType
{
    /// <summary>Legacy <c>"Main Format"</c> (lines 709-870): the 13-column voucher sheet.</summary>
    MainFormat = 0,

    /// <summary>Legacy <c>"Missing Accounts"</c> (lines 961-1026).</summary>
    MissingAccounts = 1,

    /// <summary>Legacy <c>"EmployeeWise BreakDown"</c> (lines 1028-1220).</summary>
    EmployeeWiseBreakDown = 2
}

/// <summary>
/// Filter input for the Payroll JV (Custom 035) grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom035.ascx</c>).
///
/// ATYPICAL FILTER PANEL: Payroll Area / Run Type / Month / Year / Type / Employee Code. There is no date
/// range and no employee-status combo, so those two inherited base filters are unused and the Angular
/// screen switches them off rather than forking <c>&lt;grid-report-filter&gt;</c>. The inherited
/// <c>EmployeeCode</c> IS used.
/// </summary>
public class GetPayrollJVCustom035ReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>; blank meant the CURRENT month.</summary>
    [Range(1, 12)]
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>; blank meant the CURRENT year.</summary>
    [Range(1900, 2999)]
    public int? Year { get; set; }

    /// <summary>
    /// Legacy <c>cmbPayrollArea</c>, taken here as an id rather than the legacy code. It does double duty
    /// on this screen: besides filtering employees it supplies the voucher's Company (
    /// <c>PayrollArea.FICompany</c>) and the net-salary Bank account (<c>PayrollArea.FIBank</c>), so
    /// leaving it blank blanks those cells -- see the app service remarks.
    /// </summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>. Null means "all run types" (legacy applied no predicate).</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>Legacy <c>cmbType</c>.</summary>
    public PayrollJVCustom035ReportType ReportType { get; set; }

    /// <summary>
    /// Legacy applied no sort; rows arrive in the order <c>Table1()</c> appended them, which legacy carried
    /// in its "Oid" serial column. That serial is <see cref="PayrollJVCustom035ReportRowDto.SerialNumber"/>
    /// here and is the default sort so server-side paging is deterministic.
    /// </summary>
    protected override string DefaultSorting => "SerialNumber ASC";
}

/// <summary>
/// One column of the Custom 035 sheet currently selected by
/// <see cref="GetPayrollJVCustom035ReportInput.ReportType"/>. The captions are legacy verbatim (
/// <c>"Company accounts"</c>, <c>"Offset account type"</c>, ...) because the customer reconciles the export
/// against the legacy file, so they are exported through <c>GridReportColumn.FromLiteralHeader</c> rather
/// than as localization keys.
/// </summary>
public class PayrollJVCustom035ReportColumnDto
{
    public string Name { get; set; }

    /// <summary>True when the value lives in <see cref="PayrollJVCustom035ReportRowDto.Amounts"/>.</summary>
    public bool IsNumeric { get; set; }
}

public class PayrollJVCustom035ReportOutput : PagedResultDto<PayrollJVCustom035ReportRowDto>
{
    /// <summary>
    /// The column list of the SELECTED report type, in legacy declaration order. Served from the backend so
    /// the grid and the exported xlsx are driven by ONE list and cannot drift apart.
    /// </summary>
    public List<PayrollJVCustom035ReportColumnDto> Columns { get; set; } = new();
}

/// <summary>
/// One row of legacy <c>Table1()</c> (uscRptPayrollJV_Custom035.ascx.cs:558-1224).
///
/// Dictionary-keyed for the same reason as its siblings: the three report types share no column set
/// (13 / 14 / 15 columns) and several captions cannot be C# identifiers. The one date column (
/// <c>ForPeriod</c> on Missing Accounts) is carried in <see cref="Values"/> as an invariant
/// <c>yyyy-MM-dd</c> string.
/// </summary>
public class PayrollJVCustom035ReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Oid" column: the 1-based emit order of <c>Table1()</c>.</summary>
    public int SerialNumber { get; set; }

    /// <summary>Text-valued columns, keyed by the literal legacy caption.</summary>
    public Dictionary<string, string> Values { get; set; } = new();

    /// <summary>Numeric columns, keyed by the literal legacy caption.</summary>
    public Dictionary<string, double> Amounts { get; set; } = new();
}
