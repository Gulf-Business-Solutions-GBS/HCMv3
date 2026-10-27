using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Payroll JV (Custom 051) grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom051.ascx</c>).
///
/// ATYPICAL FILTER PANEL: Month / Year / Payroll Area only (uscRptPayrollJV_Custom051.ascx.cs:62-74 and
/// <c>GetParameterStr</c>, lines 483-505). There is NO date range, NO employee-code box, NO employee-status
/// combo and NO run-type combo, so all four inherited base filters are unused and the Angular screen
/// switches them off rather than forking <c>&lt;grid-report-filter&gt;</c>.
/// </summary>
public class GetPayrollJVCustom051ReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>txtMonth</c>; blank meant the CURRENT month (lines 434-436).</summary>
    [Range(1, 12)]
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>; blank meant the CURRENT year (lines 434-436).</summary>
    [Range(1900, 2999)]
    public int? Year { get; set; }

    /// <summary>
    /// Legacy <c>cmbPayrollArea</c>, taken here as an id rather than the legacy code.
    ///
    /// LEGACY DEFECT REPRODUCED -- THIS FILTER DOES NOTHING. See
    /// <c>PayrollJVCustom051ReportAppService</c> remarks: legacy applied it to a query variable it then
    /// threw away.
    /// </summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>
    /// Legacy applied no sort to the grid; the rows arrive in the order <c>Table1()</c> appended them and
    /// legacy kept that order in a technical <c>SortOrder</c> column (line 566). That column is
    /// <see cref="PayrollJVCustom051ReportRowDto.SortOrder"/> here and is the default sort so server-side
    /// paging is deterministic.
    /// </summary>
    protected override string DefaultSorting => "SortOrder ASC";
}

/// <summary>
/// One column of the Custom 051 journal-voucher sheet.
///
/// The captions are LITERAL ERP IMPORT HEADERS (<c>"Amount|Debit"</c>, <c>"Subsidiary: Entity Type"</c>,
/// <c>"#headerconnector"</c>, ...), not localization keys: the finance system this file is imported into
/// matches on the exact header text, so translating them would break the import. They are therefore
/// exported through <c>GridReportColumn.FromLiteralHeader</c> and rendered untranslated in Angular.
/// </summary>
public class PayrollJVCustom051ReportColumnDto
{
    public string Name { get; set; }

    /// <summary>True when the value lives in <see cref="PayrollJVCustom051ReportRowDto.Amounts"/>.</summary>
    public bool IsNumeric { get; set; }
}

public class PayrollJVCustom051ReportOutput : PagedResultDto<PayrollJVCustom051ReportRowDto>
{
    /// <summary>
    /// The sheet's column list, in legacy declaration order (lines 526-553). Served from the backend so the
    /// grid and the exported xlsx are driven by ONE list and cannot drift apart.
    /// </summary>
    public List<PayrollJVCustom051ReportColumnDto> Columns { get; set; } = new();
}

/// <summary>
/// One journal-voucher line of legacy <c>Table1()</c> (uscRptPayrollJV_Custom051.ascx.cs:508-702).
///
/// The 28 legacy columns are carried in two dictionaries rather than 28 properties because the sheet is a
/// flat ERP import layout whose headers are punctuation-bearing literals that cannot be C# identifiers
/// (<c>"//"</c>, <c>"#headerconnector"</c>, <c>"Amount|Debit"</c>). This mirrors the dictionary-keyed shape
/// already used by <c>PayrollSheetCustom023ReportRowDto</c>.
/// </summary>
public class PayrollJVCustom051ReportRowDto : EntityDto<int>
{
    /// <summary>Legacy <c>"SortOrder"</c> column (line 566): the emit order of <c>Table1()</c>.</summary>
    public int SortOrder { get; set; }

    /// <summary>Text-valued columns, keyed by the literal legacy caption.</summary>
    public Dictionary<string, string> Values { get; set; } = new();

    /// <summary>Numeric columns (the four amount columns and Rate), keyed by the literal legacy caption.</summary>
    public Dictionary<string, double> Amounts { get; set; } = new();
}
