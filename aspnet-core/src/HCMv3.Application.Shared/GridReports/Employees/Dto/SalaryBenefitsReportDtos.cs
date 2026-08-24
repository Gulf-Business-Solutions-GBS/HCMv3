using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Salary Benefits analysis grid report (legacy
/// <c>uscRptSalaryBenefits.ascx</c>). Beyond the common filters legacy adds one report-specific combo,
/// <c>cmbPayrollArea</c>, matched on the PayrollArea CODE against the employee's org assignment current
/// today (uscRptSalaryBenefits.ascx.cs:539-547).
///
/// INHERITED-BUT-UNUSED: <see cref="GridReportInputBase.FromDate"/>/<see cref="GridReportInputBase.ToDate"/>
/// are NOT used. Legacy's <c>Table1()</c> pins EVERY date-effective lookup to <c>DateTime.Now</c>
/// (its local <c>EndDate</c> variable, line 518) and never consults the pickers; the report is
/// "as of today" by construction.
/// </summary>
public class GetSalaryBenefitsReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c>: matched on the PayrollArea CODE (line 541). Null or blank means
    /// "all payroll areas".
    /// </summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>
    /// Legacy applied no explicit sort (line 797 enumerates <c>newds</c> in provider order). Employee
    /// code ascending is used so server-side paging is deterministic.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// Paged result PLUS the dynamic wage-type column list, because this report's grid shape is data-driven
/// (see <see cref="SalaryBenefitsReportRowDto.WageAmounts"/>). Legacy discovered the same list per
/// execution at uscRptSalaryBenefits.ascx.cs:549-556 and added one grid column per entry; the client
/// here does the same from <see cref="WageTypeNames"/> instead of guessing from the returned rows,
/// which would drop a wage type whose page happens to be all-zero.
/// </summary>
public class SalaryBenefitsReportOutput : PagedResultDto<SalaryBenefitsReportRowDto>
{
    /// <summary>
    /// Wage type names in legacy's order -- <c>WageType.PayrollOrder</c> ascending (line 555).
    /// These are exactly the keys of every row's <see cref="SalaryBenefitsReportRowDto.WageAmounts"/>.
    /// </summary>
    public List<string> WageTypeNames { get; set; } = new();
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptSalaryBenefits.ascx.cs:514-885</c>.
///
/// LEGACY COLUMN SET (lines 638-671): OID, Code, "Employee NameEn", Status, JoiningDate, JobTitle,
/// OrgLevel1..OrgLevel7, EffectiveDate, ONE COLUMN PER WAGE TYPE (dynamic), TotalSalary, Manager,
/// WorkflowGroup, RecruitmentType, Sponsor, SeparationType, SubmissionDate, LastDayInService,
/// RevokeDate, RevokeComments, ServiceDuration, Age, Accommodation, TransportationType.
///
/// ATYPICAL: THE COLUMN SET IS DYNAMIC. Legacy adds one <c>double</c> column per wage type appearing in
/// any basic pay current today (lines 549-556 build the list, 664-667 add the columns), so the grid
/// shape depends on master data. Those amounts are carried here in <see cref="WageAmounts"/>, keyed by
/// the wage type name, and the client renders one column per key. The key list is stable within one
/// response because it is built once per request from the same wage-type query legacy used.
///
/// OrgLevel1..OrgLevel7 are kept as SEVEN columns, exactly as legacy rendered them. HCMv3's
/// <c>EmployeeOrgAssignment</c> replaced legacy's seven FKs with one FK into ABP's hierarchical
/// <c>OrganizationUnit</c> tree, so each level is resolved from that tree after materialization --
/// see <c>EmployeeOrgLevelNameResolver</c>.
///
/// DEVIATION 2 -- legacy's "WorkflowGroup" column (line 843) is DROPPED. It reads
/// <c>EmployeeOrgAssignment.WorkflowGroup</c>, which is explicitly still deferred in HCMv3's
/// <c>EmployeeOrgAssignment</c> ("STILL DEFERRED ... Position/WorkflowGroup fields" in that entity's
/// class remarks). There is no column to read; it is dropped rather than faked.
///
/// DEVIATION 3 -- legacy's "RevokeComments" column (line 858) is DROPPED. HCMv3's
/// <c>EmployeeSeparation</c> ports <c>RevokeDate</c> but has no RevokeComments property.
/// </summary>
public class SalaryBenefitsReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>Legacy "Code" column (line 801).</summary>
    public string EmployeeCode { get; set; }

    /// <summary>
    /// Legacy "Employee NameEn" column -- legacy always wrote <c>FullNameEn</c> here (line 802) even in
    /// the Arabic aspect, and the Arabic caption block only renames the header. Reproduced as-is.
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "Status" column -- the enum name (line 803).</summary>
    public string Status { get; set; }

    public DateTime JoiningDate { get; set; }

    /// <summary>Legacy "JobTitle" column -- <c>Employee.CurrentJobtitle</c>, i.e. today's assignment.</summary>
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

    /// <summary>
    /// Legacy "EffectiveDate" column -- StartDate of the basic pay current today (line 831). Null when
    /// the employee has no current basic pay (legacy left the cell DBNull).
    /// </summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>
    /// Legacy's DYNAMIC per-wage-type columns (lines 832-835), keyed by wage type name (NameAr when
    /// Arabic and it is set, else NameEn -- exactly legacy's fallback). A wage type with no detail row
    /// for this employee is 0, not missing, as in legacy. Empty when the employee has no current basic
    /// pay: legacy skipped the whole block in that case (the <c>if (empBasicSalary != null)</c> guard).
    /// </summary>
    public Dictionary<string, double> WageAmounts { get; set; } = new();

    /// <summary>Legacy "TotalSalary" column -- <c>EmployeeBasicPay.Amount</c> (line 837).</summary>
    public double? TotalSalary { get; set; }

    /// <summary>
    /// Legacy "Manager" column -- <c>Manager.CodeWithFullNameEn</c> of the manager assignment current
    /// today (line 745). Legacy's persistent alias is <c>Concat(Code, ' ', FullNameEn)</c>.
    /// </summary>
    public string ManagerName { get; set; }

    /// <summary>Legacy "RecruitmentType" column (line 844).</summary>
    public string RecruitmentTypeName { get; set; }

    /// <summary>
    /// Legacy "Sponsor" column -- <c>Sponsor.NameEn</c> of the sponsor record current today (line 776);
    /// legacy hard-coded NameEn here regardless of aspect, reproduced as-is.
    /// </summary>
    public string SponsorName { get; set; }

    /// <summary>Legacy "SeparationType" column -- from the LATEST separation by submission date (line 853).</summary>
    public string SeparationType { get; set; }

    public DateTime? SubmissionDate { get; set; }

    public DateTime? LastDayInService { get; set; }

    public DateTime? RevokeDate { get; set; }

    /// <summary>
    /// Legacy "ServiceDuration" column (lines 862-864): years to one decimal, as a STRING. Measured to
    /// the last day in service of the latest NON-REVOKED separation when there is one, otherwise to
    /// today. Kept a string because legacy formatted it with <c>"0.0"</c> and the grid showed text.
    /// </summary>
    public string ServiceDuration { get; set; }

    /// <summary>Legacy "Age" column (line 790): years to one decimal, computed against today.</summary>
    public double Age { get; set; }

    /// <summary>
    /// Legacy "Accommodation" column -- <c>AccommodationType.NameEn</c> of the accommodation current
    /// today (line 867); legacy hard-coded NameEn, reproduced as-is.
    /// </summary>
    public string AccommodationTypeName { get; set; }

    /// <summary>
    /// Legacy "TransportationType" column -- <c>AccommodationServiceType.NameEn</c> of the
    /// accommodation service current today (line 868); legacy hard-coded NameEn.
    /// </summary>
    public string TransportationTypeName { get; set; }
}
