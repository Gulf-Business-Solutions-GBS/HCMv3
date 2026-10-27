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
/// All TEN legacy org-level columns are kept: OrgLevel1..OrgLevel7 for the assignment current as of the
/// transfer date, plus Previous OrgLevel2/3/4 for the assignment that preceded it. HCMv3's
/// <c>EmployeeOrgAssignment</c> models legacy's 7 discrete <c>OrgLevel1..OrgLevel7</c> FKs as a SINGLE
/// <c>OrganizationUnitId</c> pointing into Zero's already-hierarchical
/// <c>Abp.Organizations.OrganizationUnit</c> tree (see the entity's class remarks), so each level is the
/// ancestor-or-self of that leaf classified at the matching level; the values are stamped after
/// materialization by <c>EmployeeOrgLevelNameResolver</c> and are therefore NOT server-sortable.
/// </summary>
public class EmployeeTransferReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
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
    /// Legacy OrgLevel1..OrgLevel7 (lines 608-621), resolved from the org unit of the assignment that was
    /// current AS OF THE TRANSFER DATE. Stamped post-materialization, so NOT server-sortable.
    /// </summary>
    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    /// <summary>
    /// Legacy "Previous OrgLevel2" (line 610), resolved from the org unit of the latest assignment
    /// STARTING STRICTLY BEFORE the transfer date. Legacy rendered only levels 2, 3 and 4 as "Previous".
    /// </summary>
    public string PreviousOrgLevel2 { get; set; }

    /// <summary>Legacy "Previous OrgLevel3" (line 613).</summary>
    public string PreviousOrgLevel3 { get; set; }

    /// <summary>Legacy "Previous OrgLevel4" (line 616).</summary>
    public string PreviousOrgLevel4 { get; set; }

    /// <summary>Leaf org unit of the assignment current as of the transfer date. See <see cref="IEmployeeOrgLevelRow"/>.</summary>
    public long? OrganizationUnitId { get; set; }

    /// <summary>Leaf org unit of the PREVIOUS assignment; input to the Previous OrgLevel2/3/4 columns.</summary>
    public long? PreviousOrganizationUnitId { get; set; }

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
