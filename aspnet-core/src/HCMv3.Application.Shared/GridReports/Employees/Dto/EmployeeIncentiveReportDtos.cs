using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the MASTER (level 1) grid of the Employee Incentive analysis report (legacy
/// <c>uscRptEmployeeIncentive.ascx</c>, <c>Table1()</c>, lines 468-528).
///
/// This is one of the few reports in the family that uses the date range for real: legacy shows both date
/// pickers (uscRptEmployeeIncentive.ascx:18-21, no Visible="false") and <c>GetParameterStr</c> logs them
/// (lines 452-464).
/// </summary>
public class GetEmployeeIncentiveReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy applied no sort; the master grid's key was the department name (<c>GetKey()</c> case 1,
    /// line 388), so that is the deterministic default here.
    /// </summary>
    protected override string DefaultSorting => "OrganizationUnitName ASC";
}

/// <summary>
/// Filter input for the DETAIL (level 2) grid -- legacy <c>Table2(selectedColumnValue)</c>
/// (lines 529-644), reached by clicking a master row.
///
/// ATYPICAL: the reference report (<c>uscRptEmployeeContract</c>) has a single level; this one has TWO.
/// Legacy passed the clicked row's key (the department NAME) back through the callback panel; this port
/// passes the org unit ID instead, which is stable under renames and localization.
/// </summary>
public class GetEmployeeIncentiveDetailReportInput : GridReportInputBase
{
    /// <summary>
    /// The master row that was drilled into. Required -- legacy never rendered Table2 without a selected
    /// key (<c>selectedColumnValue</c>, line 328).
    /// </summary>
    public long OrganizationUnitId { get; set; }

    /// <summary>Legacy grid was unsorted; employee code is the deterministic default for server paging.</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One MASTER row -- legacy <c>Table1()</c> columns "Department" and "Employee" (a COUNT, despite the
/// caption; lines 492-493 / 512-517).
///
/// DEVIATION: legacy grouped on <c>Employee.CurrentDepartment</c>, i.e. the org unit of the employee's
/// current assignment whose <c>OrganizationUnitType</c> was literally named "Department"
/// (OrgService.OrgUT_Department). HCMv3 classifies org units by NUMBERED level
/// (<c>OrganizationUnitType.AllowedCodes</c> = Level0..Level9) and has no "Department" type, so this port
/// groups on the assignment's own (leaf) organization unit.
/// </summary>
public class EmployeeIncentiveDepartmentRowDto
{
    /// <summary>Row key for the drill-down. Null when the employee has no current org assignment.</summary>
    public long? OrganizationUnitId { get; set; }

    /// <summary>Legacy "Department" column value (<c>g.Key.NameEn</c>, line 515).</summary>
    public string OrganizationUnitName { get; set; }

    /// <summary>Legacy "Employee" column -- <c>g.Count()</c>, i.e. number of incentive TRANSACTIONS.</summary>
    public int EmployeeCount { get; set; }
}

/// <summary>
/// One DETAIL row -- legacy <c>Table2()</c> (lines 558-576 define the columns).
///
/// Legacy column set: OID, Code, Employee, Job Title, Company Code, Company Name, Dept Code, Department,
/// Sub-Dept Code, Sub-Dept, Branch Code, Branch, CostCenter Code, Cost Center, Payroll Area, Wage Type,
/// Payment Date, Payment Number, Payment Unit.
///
/// DEVIATION: the four Company/Dept/Sub-Dept/Branch code+name pairs came from legacy's NAMED org unit
/// types (<c>Employee.CurrentCompany</c>/<c>CurrentDepartment</c>/<c>CurrentSubDepartment</c>/
/// <c>CurrentBranch</c>). HCMv3 has no named org unit types -- only numbered levels -- so they are replaced
/// by the numbered ancestor chain <see cref="OrgLevel1"/>..<see cref="OrgLevel7"/>, which is a superset of
/// the information. No legacy column is dropped without a replacement.
/// </summary>
public class EmployeeIncentiveDetailRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    /// <summary>Legacy "Code" column.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column -- legacy always used FullNameEn here (line 585).</summary>
    public string EmployeeName { get; set; }

    /// <summary>Legacy "Job Title" column, "" when unset (line 586).</summary>
    public string JobTitle { get; set; }

    /// <summary>Legacy "CostCenter Code" column.</summary>
    public string CostCenterCode { get; set; }

    /// <summary>Legacy "Cost Center" column.</summary>
    public string CostCenterName { get; set; }

    /// <summary>Legacy "Payroll Area" column.</summary>
    public string PayrollAreaName { get; set; }

    /// <summary>Legacy "Wage Type" column.</summary>
    public string WageTypeName { get; set; }

    /// <summary>Legacy "Payment Date" column.</summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>Legacy "Payment Number" column.</summary>
    public double PaymentNumber { get; set; }

    /// <summary>Legacy "Payment Unit" column.</summary>
    public PaymentUnit PaymentUnit { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public long? OrganizationUnitId { get; set; }
}
