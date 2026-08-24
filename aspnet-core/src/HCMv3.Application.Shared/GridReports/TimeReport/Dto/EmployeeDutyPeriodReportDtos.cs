using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Employee Duty Period analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptEmployeeDutyPeriod.ascx</c>).
///
/// FILTER PANEL NOTE: the legacy panel reuses the control ID <c>cmbEmpStatus</c> but it is NOT an employee
/// status combo on this screen -- <c>BindDutyPeriod()</c> (uscRptEmployeeDutyPeriod.ascx.cs:186-215) binds
/// it to <c>DutyPeriod</c> with <c>ValueField = "Code"</c> and the null text "Select Duty Period", and
/// <c>GetParameterStr()</c> (line 470-471) logs it as "Duty Period : ". It is therefore carried here by
/// <see cref="DutyPeriodCode"/>, and the inherited <c>EmployeeStatus</c> is UNUSED by this report (the
/// Angular screen hides the status field rather than forking the shared filter panel).
/// </summary>
public class GetEmployeeDutyPeriodReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>Session[DutyPeriodCode]</c> (value = <c>DutyPeriod.Code</c>), applied as
    /// <c>r.DutyPeriod.Code == _code</c> (uscRptEmployeeDutyPeriod.ascx.cs:552-556). Null/empty means
    /// "all duty periods".
    /// </summary>
    public string DutyPeriodCode { get; set; }

    /// <summary>
    /// Legacy line 605: <c>OrderBy(Code).ThenBy(StartDate).ThenBy(EndDate)</c>.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, StartDate ASC, EndDate ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeDutyPeriod.ascx.cs:484-640</c> column-for-column: OID, Code, Name, JobTitle,
/// OrgLevel1..OrgLevel7, DutyPeriod, StartDate, EndDate, StartPoint, Country, WorkLocation.
///
/// OrgLevel1..OrgLevel7 are kept as SEVEN columns, exactly as legacy rendered them; HCMv3's
/// <c>EmployeeOrgAssignment</c> replaced legacy's seven FKs with one FK into ABP's hierarchical
/// <c>OrganizationUnit</c> tree, so each level is resolved after materialization -- see
/// <c>EmployeeOrgLevelNameResolver</c>.
///
/// LEGACY "AS OF" SEMANTIC (lines 570-572, deliberately reproduced): JobTitle / OrgLevel1..7 / Country /
/// WorkLocation all come from the org assignment current as of <c>DateTime.Now.Date</c> -- TODAY, not the
/// reported duty-period row's own date range and not the report's From/To window.
/// </summary>
public class EmployeeDutyPeriodReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>Legacy "Code" column -- <c>rec.Employee.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Name" column -- FullNameAr when Arabic, else FullNameEn.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "JobTitle" column -- empty string (not null) when there is none.</summary>
    public string JobTitleName { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }

    /// <summary>Legacy "DutyPeriod" column -- empty string (not null) when there is none.</summary>
    public string DutyPeriodName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Legacy "StartPoint" column (declared <c>Int16</c> there, an <c>int</c> on the entity).</summary>
    public int StartPoint { get; set; }

    /// <summary>Legacy "Country" column -- the PayrollArea's Country of the current org assignment.</summary>
    public string CountryName { get; set; }

    /// <summary>Legacy "WorkLocation" column of the current org assignment.</summary>
    public string WorkLocationName { get; set; }
}
