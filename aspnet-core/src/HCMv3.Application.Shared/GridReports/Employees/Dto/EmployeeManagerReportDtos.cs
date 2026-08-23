using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Manager analysis grid report (legacy
/// <c>uscRptEmployeeManager.ascx</c>). Legacy exposes exactly the four common filters
/// (<c>dtFrom</c>/<c>dtTo</c>/<c>cmbEmpStatus</c>/<c>txtEmpCode</c>) and nothing report-specific, so this
/// class adds no fields of its own beyond the default sort.
/// </summary>
public class GetEmployeeManagerReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy applied no default sort (uscRptEmployeeManager.ascx.cs:596-620 enumerates the query in
    /// provider order). Employee code ascending is used here so server-side paging is deterministic.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeManager.ascx.cs:461-641</c>: OID, Code, Name, StartDate, EndDate, JobTitle,
/// OrgLevel1..7, WorkLocation, CostCenterCode, CostCenter, PostingGroup, BudgetingGroup, WorkflowGroup,
/// Manager.
///
/// TWO STRUCTURAL DEVIATIONS, both forced by the HCMv3 entity model rather than chosen:
/// <list type="bullet">
/// <item><description>The legacy report's seven discrete <c>OrgLevel1</c>..<c>OrgLevel7</c> columns
/// collapse into the single <see cref="OrganizationUnitName"/> column, because
/// <c>EmployeeOrgAssignment</c> in HCMv3 replaced those seven FKs with one FK to ABP's hierarchical
/// <c>OrganizationUnit</c> tree (see the class remarks on <c>EmployeeOrgAssignment</c>).</description></item>
/// <item><description>The legacy <c>WorkflowGroup</c> column is DROPPED: <c>WorkflowGroup</c> is listed
/// as still-deferred on <c>EmployeeOrgAssignment</c>, so there is no field to read.</description></item>
/// </list>
/// </summary>
public class EmployeeManagerReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Code" column -- <c>rec.Employee.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Name" column -- <c>FullNameAr</c> when Arabic, else <c>FullNameEn</c>.</summary>
    public string EmployeeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>
    /// Legacy "JobTitle" column. Legacy read it from the org assignment current as of
    /// <c>DateTime.Now</c> (line 566), NOT as of the report's From date -- kept as-is.
    /// </summary>
    public string JobTitleName { get; set; }

    /// <summary>Replaces the legacy OrgLevel1..7 column block (see class remarks).</summary>
    public string OrganizationUnitName { get; set; }

    public string WorkLocationName { get; set; }

    public string CostCenterCode { get; set; }

    public string CostCenterName { get; set; }

    public string PostingGroupName { get; set; }

    public string BudgetingGroupName { get; set; }

    /// <summary>
    /// Legacy "Manager" column -- <c>Manager.CodeWithFullNameAr/En</c> (line 615). HCMv3's Employee has
    /// no <c>CodeWithFullName</c> computed property, so the same <c>Code + " " + FullName</c> text (legacy PersistentAlias
    /// <c>Concat(IsNull(Code,''), ' ', IsNull(FullNameEn,''))</c>) is composed here.
    /// </summary>
    public string ManagerName { get; set; }
}
