using System;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Time Analysis By Department grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptTimeAnalysisByDept.ascx</c>).
///
/// Uses only the common filters -- <c>dtFrom</c>, <c>dtTo</c>, <c>cmbEmpStatus</c>, <c>txtEmpCode</c> --
/// so it adds no fields of its own. Legacy's <c>GetParameterStr()</c> (lines 466-478) logs employee code
/// plus the two dates.
/// </summary>
public class GetTimeAnalysisByDeptReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy left the grid unsorted. Department name ascending is the natural reading order for a
    /// department roll-up and is deterministic, which server-side paging requires. (Sorting on
    /// <c>TotalValue</c> would be meaningless -- see <see cref="TimeAnalysisByDeptReportRowDto.TotalValue"/>.)
    /// </summary>
    protected override string DefaultSorting => "DepartmentName ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (uscRptTimeAnalysisByDept.ascx.cs:481-543).
///
/// SUMMARY GRAIN, NOT PER EMPLOYEE: one row per department, over the <c>TimeRegister</c> rows in
/// the requested window. Legacy columns: <c>Department</c>, <c>TotalValue</c> and a hidden <c>Period</c>
/// (<c>ColumnMapping = MappingType.Hidden</c>, line 511).
/// </summary>
public class TimeAnalysisByDeptReportRowDto
{
    /// <summary>
    /// Legacy "Department" column -- <c>OrgService.GetOrgUnitForUC(rec.Employee, ...).NameEn</c>
    /// (line 526), i.e. the DEEPEST non-null <c>Employee.OrgLevel7..OrgLevel1</c>, rendered in ENGLISH
    /// even in Arabic.
    ///
    /// MODEL ADAPTATION: HCMv3 has no OrgLevel1..7 fields; the employee's unit is the single
    /// <c>EmployeeOrgAssignment.OrganizationUnitId</c> leaf in the real org tree, so "deepest non-null
    /// level" becomes "the deepest classified level of that leaf". Empty string when the employee has no
    /// current assignment.
    /// </summary>
    public string DepartmentName { get; set; }

    /// <summary>
    /// Legacy hidden "Period" column -- literally <c>dtToValue</c>, the report's TO date, stamped onto
    /// every row (line 535). It is not derived from the data.
    /// </summary>
    public DateTime Period { get; set; }

    /// <summary>
    /// Legacy "TotalValue" column.
    ///
    /// FLAGGED DEFECT, NOT SILENTLY "FIXED": legacy hardcodes this to <c>0</c>. The line that computed it,
    /// <c>g.Sum(r =&gt; r.TimeRegisterDetail.Sum(v =&gt; v.TimeValue))</c>, is commented out with the note
    /// "below line commented during time reg restructing" (uscRptTimeAnalysisByDept.ascx.cs:527-529), and
    /// the same is true of the three drill-down levels (<c>Table2</c> line 583, <c>Table3</c> lines
    /// 641-642, <c>Table4</c> lines 697-698). The entire report has therefore rendered zeros in production
    /// since that restructuring.
    ///
    /// It is NOT reconstructed here: the <c>TimeRegisterDetail</c>/<c>TimeValue</c> model it summed does
    /// not exist in HCMv3 either, so any replacement number would be an invention, not a port. The zero is
    /// reproduced and pinned by a test, and the defect is reported so a product decision can be made about
    /// what this column should actually total.
    /// </summary>
    public double TotalValue { get; set; }
}
