using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Payroll Analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollAnalysis.ascx</c>).
///
/// ATYPICAL FILTER PANEL: legacy's panel is Payroll Area / Run Type / Month / Year -- no date range, no
/// employee code, no employee status. <c>GetParameterStr</c> (uscRptPayrollAnalysis.ascx.cs:571-591) logs
/// exactly those four. The inherited base filters are unused and the Angular screen hides them.
/// </summary>
public class GetPayrollAnalysisReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>txtMonth</c>. Blank meant the CURRENT month (lines 497-498), and Month+Year become the
    /// LAST day of that month (line 502).
    /// </summary>
    [Range(1, 12)]
    public int? Month { get; set; }

    /// <summary>Legacy <c>txtYear</c>. Blank means the CURRENT year -- see <see cref="Month"/>.</summary>
    [Range(1900, 2999)]
    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c> / <c>Session[RunType]</c> (lines 618-632). Null means "all run types".</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>
    /// Legacy <c>cmbPayrollArea</c>, which held the payroll area CODE and resolved it to an object before
    /// filtering (lines 635-641). This port takes the id directly -- same filter, one query fewer.
    /// </summary>
    public int? PayrollAreaId { get; set; }

    /// <summary>
    /// Legacy grid was unsorted. Total amount descending is the natural reading order for a spend-by-unit
    /// summary and is deterministic, which server-side paging requires.
    /// </summary>
    protected override string DefaultSorting => "TotalAmount DESC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptPayrollAnalysis.ascx.cs:592-685).
///
/// SUMMARY GRAIN -- NOT A PER-EMPLOYEE SHEET. One row per (OrgLevel1 unit, <c>SalaryRegister.ForPeriod</c>),
/// the value being the SUM of every employee's NET PAY (wage type 9000) under that top-level unit.
///
/// Legacy declared three columns (lines 601-607): <c>OID</c> (the OrgLevel1 unit's Oid, a drill-down key),
/// the localized OrgLevel1 caption, and <c>Amount</c>. OID is not reproduced -- see
/// <see cref="OrgLevel1Name"/> remarks.
/// </summary>
public class PayrollAnalysisReportRowDto
{
    /// <summary>
    /// Legacy OrgLevel1 column -- <c>Employee.OrgLevel1.NameAr/NameEn</c> per culture (line 658).
    ///
    /// MODEL ADAPTATION: HCMv3 has no OrgLevel1..7 fields; the employee's unit is the single
    /// <c>EmployeeOrgAssignment.OrganizationUnitId</c> leaf in the real org tree, so "OrgLevel1" is the
    /// leaf's ancestor-or-self classified as Level1.
    ///
    /// DELIBERATE DEVIATION -- NO OID COLUMN: legacy grouped by (OrgLevel1.Oid, name) and rendered the Oid
    /// as a hidden-ish first column used only as the drill-down key; the drill-down is not ported, so the
    /// key has no reader value (the same call the reference EmployeeContract port made about its OID
    /// column). The consequence is that the grouping key here is the NAME, so two distinct level-1 units
    /// sharing a name would merge into one row. Pinned by
    /// <c>Should_Group_Level1_Units_That_Share_A_Name_Into_One_Row</c>.
    /// </summary>
    public string OrgLevel1Name { get; set; }

    /// <summary>
    /// Legacy group key <c>period</c> (<c>SalaryRegister.ForPeriod</c>, line 659). NOT a legacy output
    /// column -- and it cannot vary here anyway, since the query pins <c>ForPeriod</c> to a single date.
    /// Projected because it costs nothing and makes the row self-describing; excluded from the export,
    /// which stays legacy-exact.
    /// </summary>
    public DateTime Period { get; set; }

    /// <summary>Legacy "Amount" column (Arabic caption "المبلغ الإجمالي", line 604) -- SUM of wage type 9000.</summary>
    public double TotalAmount { get; set; }
}
