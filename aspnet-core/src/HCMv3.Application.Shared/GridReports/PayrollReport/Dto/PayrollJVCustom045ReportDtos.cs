using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// The legacy <c>cmbType</c> combo of <c>uscRptPayrollJV_Custom045.ascx</c>. Each value produces a
/// COMPLETELY DIFFERENT sheet -- different columns, different grain, different aggregation -- so it is a
/// filter, not a formatting option. Legacy returned an empty table when the combo was blank
/// (uscRptPayrollJV_Custom045.ascx.cs:562-565); here the value is required and defaults to
/// <see cref="SalaryJV"/>.
/// </summary>
public enum PayrollJVCustom045ReportType
{
    /// <summary>Legacy <c>"Salary JV"</c> (lines 758-1000): the 30-column ERP import sheet.</summary>
    SalaryJV = 0,

    /// <summary>Legacy <c>"Missing Accounts"</c> (lines 990-1042): lines with no account mapping.</summary>
    MissingAccounts = 1,

    /// <summary>Legacy <c>"EmployeeWise BreakDown"</c> (lines 1044-1140).</summary>
    EmployeeWiseBreakDown = 2
}

/// <summary>
/// Filter input for the Payroll JV (Custom 045) grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom045.ascx</c>).
///
/// ATYPICAL FILTER PANEL: Payroll Area / Run Type / Month / Year / Type / Employee Code
/// (<c>GetParameterStr</c>, lines 525-548). There is no date range and no employee-status combo, so those
/// two inherited base filters are unused and the Angular screen switches them off rather than forking
/// <c>&lt;grid-report-filter&gt;</c>. The inherited <c>EmployeeCode</c> IS used.
/// </summary>
public class GetPayrollJVCustom045ReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>; blank meant the CURRENT month (lines 475-479).</summary>
    [Range(1, 12)]
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>; blank meant the CURRENT year (lines 475-479).</summary>
    [Range(1900, 2999)]
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c>, taken here as an id rather than the legacy code.</summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>. Null means "all run types" (legacy applied no predicate).</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>Legacy <c>cmbType</c>.</summary>
    public PayrollJVCustom045ReportType ReportType { get; set; }

    /// <summary>
    /// Legacy applied no sort; rows arrive in the order <c>Table1()</c> appended them, which legacy carried
    /// in its "Oid" serial column. That serial is <see cref="PayrollJVCustom045ReportRowDto.SerialNumber"/>
    /// here and is the default sort so server-side paging is deterministic.
    /// </summary>
    protected override string DefaultSorting => "SerialNumber ASC";
}

/// <summary>
/// One column of the Custom 045 sheet currently selected by
/// <see cref="GetPayrollJVCustom045ReportInput.ReportType"/>.
///
/// The Salary JV captions are LITERAL ERP IMPORT HEADERS (<c>"lines.debit.value.amount"</c>,
/// <c>"lines.subsidiary#type"</c>, ...) matched by exact text by the finance system that consumes the
/// file, so they are never translated; the other two sheets keep their legacy captions verbatim for the
/// same reason (they are reconciled against legacy output). All of them are exported through
/// <c>GridReportColumn.FromLiteralHeader</c>.
/// </summary>
public class PayrollJVCustom045ReportColumnDto
{
    public string Name { get; set; }

    /// <summary>True when the value lives in <see cref="PayrollJVCustom045ReportRowDto.Amounts"/>.</summary>
    public bool IsNumeric { get; set; }
}

public class PayrollJVCustom045ReportOutput : PagedResultDto<PayrollJVCustom045ReportRowDto>
{
    /// <summary>
    /// The column list of the SELECTED report type, in legacy declaration order. Served from the backend
    /// so the grid and the exported xlsx are driven by ONE list and cannot drift apart.
    /// </summary>
    public List<PayrollJVCustom045ReportColumnDto> Columns { get; set; } = new();
}

/// <summary>
/// One row of legacy <c>Table1()</c> (uscRptPayrollJV_Custom045.ascx.cs:557-1145).
///
/// The columns are carried in dictionaries rather than as properties because the three report types share
/// no column set at all (30 / 13 / 17 columns) and several captions cannot be C# identifiers
/// (<c>"lines.debit.value.amount"</c>, <c>"Fc amount"</c>, <c>"Dr/Cr"</c>). Same dictionary-keyed shape as
/// <c>PayrollSheetCustom023ReportRowDto</c>.
///
/// DATE COLUMNS: the Missing Accounts sheet has one (<c>ForPeriod</c>). It is carried in
/// <see cref="Values"/> formatted invariantly as <c>yyyy-MM-dd</c> rather than in a third dictionary --
/// unambiguous regardless of the reader's locale.
/// </summary>
public class PayrollJVCustom045ReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Oid" column: the 1-based emit order of <c>Table1()</c>.</summary>
    public int SerialNumber { get; set; }

    /// <summary>Text-valued columns, keyed by the literal legacy caption.</summary>
    public Dictionary<string, string> Values { get; set; } = new();

    /// <summary>Numeric columns, keyed by the literal legacy caption.</summary>
    public Dictionary<string, double> Amounts { get; set; } = new();
}
