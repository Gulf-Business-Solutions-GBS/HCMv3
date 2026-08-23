using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Transfer analysis grid report (legacy
/// <c>uscRptEmployeeTransfer.ascx</c>). Legacy exposes exactly the four common filters
/// (uscRptEmployeeTransfer.ascx.cs:445-457 <c>GetParameterStr</c> lists only those), so no
/// report-specific fields are added.
/// </summary>
public class GetEmployeeTransferReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy applied no explicit sort (uscRptEmployeeTransfer.ascx.cs:600 enumerates <c>newds</c> in
    /// provider order). Employee code ascending is used so server-side paging is deterministic.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeTransfer.ascx.cs:464-640</c>.
///
/// LEGACY COLUMN SET (lines 505-523): OID, Code, Name, TransferDate, OrgLevel1, OrgLevel2,
/// "Previous OrgLevel2", OrgLevel3, "Previous OrgLevel3", OrgLevel4, "Previous OrgLevel4", OrgLevel5,
/// OrgLevel6, OrgLevel7, WorkLocation, PreviousWorkLocation, CostCenter, PreviousCostCenter, Country.
///
/// DEVIATION -- the 10 org-level columns collapse to 2. HCMv3's <c>EmployeeOrgAssignment</c> models the
/// legacy's 7 discrete <c>OrgLevel1..OrgLevel7</c> FKs as a SINGLE <c>OrganizationUnitId</c> pointing
/// into Zero's already-hierarchical <c>Abp.Organizations.OrganizationUnit</c> tree (see the entity's
/// class remarks, and the same collapse in <c>ReportsAppService.cs:727-728</c>). There is therefore no
/// per-level column to project, and ABP's <c>OrganizationUnit</c> carries a single
/// <c>DisplayName</c> rather than NameEn/NameAr, so these two columns are culture-invariant.
/// </summary>
public class EmployeeTransferReportRowDto : EntityDto<int>
{
    /// <summary>Legacy "Code" column (line 604).</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Name" column -- <c>FullNameAr</c> when Arabic, else <c>FullNameEn</c> (line 574).</summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// Legacy "TransferDate" column -- <c>rec.StartDate</c> of the EmployeeAction (line 575/606).
    /// </summary>
    public DateTime TransferDate { get; set; }

    /// <summary>
    /// Stands in for legacy OrgLevel1..OrgLevel7 (lines 608-621): the org unit of the assignment that was
    /// current AS OF THE TRANSFER DATE. Null when there is no such assignment or it has no org unit.
    /// </summary>
    public string OrganizationUnitName { get; set; }

    /// <summary>
    /// Stands in for legacy "Previous OrgLevel2/3/4" (lines 610/613/616): the org unit of the latest
    /// assignment STARTING STRICTLY BEFORE the transfer date.
    /// </summary>
    public string PreviousOrganizationUnitName { get; set; }

    /// <summary>Legacy "WorkLocation" column -- from the assignment current as of the transfer date (line 623).</summary>
    public string WorkLocationName { get; set; }

    /// <summary>Legacy "PreviousWorkLocation" column -- from the previous assignment (line 624).</summary>
    public string PreviousWorkLocationName { get; set; }

    /// <summary>Legacy "CostCenter" column (line 626).</summary>
    public string CostCenterName { get; set; }

    /// <summary>Legacy "PreviousCostCenter" column (line 627).</summary>
    public string PreviousCostCenterName { get; set; }

    /// <summary>
    /// Legacy "Country" column -- country of the PayrollArea on the assignment current as of the
    /// transfer date (line 629).
    /// </summary>
    public string CountryName { get; set; }
}
