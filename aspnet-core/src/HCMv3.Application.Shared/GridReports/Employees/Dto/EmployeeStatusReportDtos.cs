using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Status analysis grid report (legacy
/// <c>uscRptEmployeeStatus.ascx</c>).
///
/// DELIBERATE DEVIATION -- read this before "fixing" it: legacy renders all four filters but
/// <c>Table1()</c> has the status and employee-code filters COMMENTED OUT (lines 490-501) and never reads
/// the dates, so on the legacy screen typing an employee code changed nothing. This port WIRES THE
/// STATUS AND CODE FILTERS UP (they are the standard shared filters and a dead filter box is a defect,
/// not a contract) and hides the two date pickers, which have no meaning at all in this report.
/// </summary>
public class GetEmployeeStatusReportInput : GridReportInputBase
{
    /// <summary>Legacy applied no default sort; employee code ascending keeps paging deterministic.</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeStatus.ascx.cs:484-676</c>: OID, Code, Employee, JobTitle, IsActive, Status,
/// OrgLevel1..7, BirthDate, JoiningDate, BasicPay, Allowance, TotalSalary, WorkLocation, SeperationDate,
/// LastDate.
///
/// DEVIATIONS:
/// <list type="bullet">
/// <item><description>OrgLevel1..OrgLevel7 are kept as SEVEN columns, as in legacy. HCMv3's
/// <c>EmployeeOrgAssignment</c> replaced legacy's seven FKs with one FK into ABP's hierarchical
/// <c>OrganizationUnit</c> tree, so each level is resolved from that tree after materialization --
/// see <c>EmployeeOrgLevelNameResolver</c>.</description></item>
/// <item><description>The legacy <c>SeperationDate</c> and <c>LastDate</c> columns are DROPPED. They are
/// declared in the legacy DataTable but the only two lines that would fill them are commented out
/// (lines 606-607 in the projection, 667-668 in the fill loop), so legacy always rendered them empty.
/// Porting two permanently blank columns would be porting a bug.</description></item>
/// <item><description>Legacy read JobTitle/WorkLocation/OrgLevels from <c>Employee.CurrentJobtitle</c>/
/// <c>CurrentWorkLocation</c>/<c>OrgLevelN</c> computed properties. HCMv3's Employee has none of those, so
/// they come from the org assignment current TODAY -- which is exactly what "Current*" meant in legacy.</description></item>
/// </list>
/// </summary>
public class EmployeeStatusReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    /// <summary>Legacy "IsActive" column (line 587): <c>Status == EmployeeStatus.Active</c>.</summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Legacy "Status" column (lines 620-640) -- NOT <c>Employee.Status</c>. It is a derived lifecycle
    /// label picked by a first-match ladder: an employee with a Full-and-Final payment gets an EMPTY
    /// label (legacy's assignment there is commented out, line 625-628), else a separation makes it
    /// "Separation", else a probation end date in the future makes it "Probationary", else joining
    /// inside the CURRENT CALENDAR YEAR makes it "New Join", else empty.
    /// The three literals are legacy's own constants (lines 49-51) and are intentionally NOT localized,
    /// so saved filters and downstream sheets keep matching.
    /// </summary>
    public string StatusLabel { get; set; }

    /// <summary>
    /// Legacy <c>OrgLevel1</c>..<c>OrgLevel7</c> columns. HCMv3 stores ONE
    /// <c>EmployeeOrgAssignment.OrganizationUnitId</c> into Zero's hierarchical OrganizationUnit tree, so
    /// each level is the ancestor-or-self of that leaf classified at the matching level. The values are
    /// stamped after materialization by <c>EmployeeOrgLevelNameResolver</c> and are therefore NOT
    /// server-sortable.
    /// </summary>
    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>Leaf org unit the seven levels are resolved from. See <see cref="IEmployeeOrgLevelRow"/>.</summary>
    public long? OrganizationUnitId { get; set; }

    public DateTime BirthDate { get; set; }

    public DateTime JoiningDate { get; set; }

    /// <summary>Legacy "BasicPay" (line 646): sum of wage type code <c>1000</c> on the basic pay current today.</summary>
    public double BasicPay { get; set; }

    /// <summary>
    /// Legacy "Allowance" (line 647): sum of every OTHER wage type flagged <c>IncludeInBasicPay</c>.
    /// </summary>
    public double Allowance { get; set; }

    /// <summary>
    /// Legacy "TotalSalary" (line 648): the basic-pay HEADER amount <c>EmployeeBasicPay.Amount</c> --
    /// deliberately not the sum of the details, which is what the sibling PostWise report uses.
    /// </summary>
    public double TotalSalary { get; set; }

    public string WorkLocationName { get; set; }
}
