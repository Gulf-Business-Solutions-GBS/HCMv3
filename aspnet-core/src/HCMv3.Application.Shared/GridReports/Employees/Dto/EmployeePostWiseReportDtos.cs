using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Post Wise analysis grid report (legacy
/// <c>uscRptEmployeePostWise.ascx</c>).
///
/// DATE-LESS report: the legacy markup hides BOTH <c>dtFrom</c> and <c>dtTo</c>
/// (<c>uscRptEmployeePostWise.ascx:12-13,19,22 Visible="false"</c>), <c>Table1()</c> never reads them, and
/// <c>GetParameterStr</c> logs only the status and code. Everything in the report is "as of today".
/// <see cref="GridReportInputBase.FromDate"/>/<see cref="GridReportInputBase.ToDate"/> are inherited from
/// the shared base but are IGNORED, exactly as legacy ignored them.
/// </summary>
public class GetEmployeePostWiseReportInput : GridReportInputBase
{
    /// <summary>Legacy sorted the rows by employee code (<c>newds.OrderBy(r =&gt; r.Code)</c>, line 645).</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeePostWise.ascx.cs:465-696</c>: OID, Code, EmployeeName, Status, JoiningDate, JobTitle,
/// OrgLevel1..7, BasicSalary, Allowance, TotalSalary, AccommodationType, TransportationType,
/// ServiceDuration, LastWorkingDay, Age.
///
/// OrgLevel1..OrgLevel7 are kept as SEVEN columns, exactly as legacy rendered them. HCMv3's
/// <c>EmployeeOrgAssignment</c> replaced legacy's seven FKs with one FK into ABP's hierarchical
/// <c>OrganizationUnit</c> tree, so each level is resolved from that tree after materialization --
/// see <c>EmployeeOrgLevelNameResolver</c>.
///
/// <see cref="Age"/> and <see cref="ServiceDurationYears"/> are stamped onto the row AFTER the query
/// materializes, not computed in SQL: they depend on "now" and on a rounding format that has no faithful
/// SQL translation. They are therefore not sortable server-side; the underlying
/// <see cref="BirthDate"/>/<see cref="JoiningDate"/>/<see cref="LastWorkingDay"/> columns are.
/// </summary>
public class EmployeePostWiseReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    /// <summary>
    /// Legacy "EmployeeName" column -- <c>rec.FullNameEn</c> at line 634, the ENGLISH name even under the
    /// Arabic aspect (unlike almost every other report in this family). Kept as legacy had it.
    /// </summary>
    public string EmployeeName { get; set; }

    public EmployeeStatus Status { get; set; }

    public DateTime JoiningDate { get; set; }

    /// <summary>Resolved from the org assignment current TODAY (legacy line 573 used <c>DateTime.Now</c>).</summary>
    public string JobTitleName { get; set; }

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

    /// <summary>Legacy "BasicSalary" (line 671): the amount of wage type code <c>1000</c>, or 0.</summary>
    public double BasicSalary { get; set; }

    /// <summary>
    /// Legacy "Allowance" (line 672): the sum of every OTHER wage type flagged
    /// <c>IncludeInBasicPay</c> on the basic-pay record current today.
    /// </summary>
    public double Allowance { get; set; }

    /// <summary>Legacy "TotalSalary" (line 673): the sum of ALL wage-type amounts, including non-basic ones.</summary>
    public double TotalSalary { get; set; }

    /// <summary>Legacy "AccommodationType" -- always the English name (line 613).</summary>
    public string AccommodationTypeName { get; set; }

    /// <summary>
    /// Legacy "TransportationType" -- the accommodation SERVICE type current today (line 623); English
    /// name, as legacy had it.
    /// </summary>
    public string TransportationTypeName { get; set; }

    /// <summary>
    /// Legacy "LastWorkingDay" -- the latest <c>LastDayInService</c> of a non-revoked separation. Null
    /// here where legacy wrote <c>default(DateTime)</c>, which the grid rendered as an empty cell.
    /// </summary>
    public DateTime? LastWorkingDay { get; set; }

    /// <summary>Carried so <see cref="Age"/> can be stamped after materialization; also sortable.</summary>
    public DateTime BirthDate { get; set; }

    /// <summary>
    /// Legacy "ServiceDuration" (line 677-679): years between joining and the last working day, or
    /// between joining and today when still in service, as <c>totalDays / 365.25</c> to one decimal.
    /// Legacy produced a STRING ("0.0"); a number is produced here so the grid can right-align and total it.
    /// </summary>
    public double ServiceDurationYears { get; set; }

    /// <summary>Legacy "Age" (line 638): whole years plus months/12, rounded to one decimal.</summary>
    public double Age { get; set; }
}
