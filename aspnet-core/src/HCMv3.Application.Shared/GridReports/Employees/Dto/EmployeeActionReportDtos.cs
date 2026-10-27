using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Action analysis grid report (legacy
/// <c>uscRptEmployeeAction.ascx</c>).
///
/// ATYPICAL for this family: the legacy panel has dtFrom/dtTo/txtEmpCode/cmbPersonalAction but NO
/// <c>cmbEmpStatus</c>. <see cref="GridReportInputBase.EmployeeStatus"/> is therefore inherited but
/// deliberately NOT applied by the app service, and the Angular screen hides the status dropdown.
/// </summary>
public class GetEmployeeActionReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPersonalAction</c> (value = <c>PersonalAction.Code</c>). Null/empty means "all
    /// actions" (uscRptEmployeeAction.ascx.cs:505-508).
    /// </summary>
    public string PersonalActionCode { get; set; }

    /// <summary>
    /// Legacy ordered the result by Code, then StartDate, then EndDate
    /// (uscRptEmployeeAction.ascx.cs:600). EndDate is not a stored column in HCMv3 (it mirrors
    /// StartDate -- see <see cref="EmployeeActionReportRowDto.EndDate"/>), so the third key is dropped.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, StartDate ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeAction.ascx.cs:484-643</c>: OID, Code, Name, JobTitle, Country, OrgLevel1..7,
/// WorkLocation, StartDate, EndDate, PersonalAction, ActionReason.
///
/// OrgLevel1..OrgLevel7 are kept as SEVEN columns, exactly as legacy rendered them. HCMv3's
/// <c>EmployeeOrgAssignment</c> replaced legacy's seven FKs with one FK into ABP's hierarchical
/// <c>OrganizationUnit</c> tree, so each level is resolved from that tree after materialization --
/// see <c>EmployeeOrgLevelNameResolver</c>.
/// </summary>
public class EmployeeActionReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Name" column -- FullNameAr when Arabic, else FullNameEn.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "JobTitle" column, from the org assignment current TODAY (line 551).</summary>
    public string JobTitleName { get; set; }

    /// <summary>
    /// Legacy "Country" column -- <c>rec.Employee.Country</c> (line 587). NOTE this is the EMPLOYEE's
    /// country, unlike the reference Employee Contract report where "Country" is the PayrollArea's.
    /// </summary>
    public string CountryName { get; set; }

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

    /// <summary>Legacy "WorkLocation" column, from the org assignment current TODAY.</summary>
    public string WorkLocationName { get; set; }

    public DateTime StartDate { get; set; }

    /// <summary>
    /// Legacy "EndDate" column. The legacy <c>EmployeeAction.EndDate</c> property has no backing store --
    /// its getter returns <c>StartDate</c> -- so HCMv3's entity has no EndDate column and this always
    /// equals <see cref="StartDate"/>. Kept as its own field so the ported grid keeps legacy's columns.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>Legacy "PersonalAction" column -- empty string (not null) when there is none.</summary>
    public string PersonalActionName { get; set; }

    /// <summary>Legacy "ActionReason" column -- empty string (not null) when there is none.</summary>
    public string ActionReasonName { get; set; }
}
