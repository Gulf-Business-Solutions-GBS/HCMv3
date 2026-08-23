using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GridReports.Employees.Dto;

/// <summary>
/// Filter input for the Employee Salary Change analysis grid report (legacy
/// <c>uscRptEmployeeSalaryChange.ascx</c>).
///
/// This is the only report in this batch whose legacy screen shows ONLY the date range: it has no
/// <c>txtEmpCode</c> and no <c>cmbEmpStatus</c> control at all (<c>uscRptEmployeeSalaryChange.ascx:11-19</c>,
/// and its <c>GetParameterStr</c> logs only From/To). The two inherited filters are still honoured when a
/// caller sends them -- filtering down can only ever be safe -- but the Angular screen hides them,
/// matching legacy.
/// </summary>
public class GetEmployeeSalaryChangeReportInput : GridReportInputBase
{
    /// <summary>Legacy applied no default sort; employee code ascending keeps paging deterministic.</summary>
    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One wage-type cell trio of the salary-change pivot: what the component paid BEFORE the window, what it
/// pays at the END of the window, and the difference. Legacy materialized these as three DYNAMIC
/// DataTable columns per wage type ("Old X", "Adj X", "Final X" -- lines 483-500).
/// </summary>
public class EmployeeSalaryChangeComponentDto
{
    public string WageTypeCode { get; set; }

    public string WageTypeName { get; set; }

    /// <summary>Legacy "Old {name}": the amount on the last basic pay STARTING BEFORE the From date.</summary>
    public double OldAmount { get; set; }

    /// <summary>Legacy "Adj {name}": <see cref="FinalAmount"/> minus <see cref="OldAmount"/>.</summary>
    public double AdjustmentAmount { get; set; }

    /// <summary>Legacy "Final {name}": the amount on the last basic pay STARTING ON OR BEFORE the To date.</summary>
    public double FinalAmount { get; set; }
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptEmployeeSalaryChange.ascx.cs:436-600</c>: OID, Code, Employee Name, OrgLevel1..7, Status,
/// Parameter Start Date, Parameter End Date, then a DYNAMIC block of "Old/Adj/Final {wage type}" columns
/// plus the three totals.
///
/// ATYPICAL FOR THIS FAMILY -- read before changing: this report is a PIVOT. Its column count depends on
/// the data (one column trio per wage type that appears anywhere in <c>EmployeeBasicPayDetail</c>), which
/// no other report in the wave does. That block cannot be a fixed set of DTO properties, so it is carried
/// as <see cref="Components"/>: the SAME list, in the same order, on every row (absent wage types come
/// back as zeros, exactly as legacy's <c>ContainsKey ? ... : 0</c> did). The Angular grid and the Excel
/// export both build their dynamic headers from that list.
///
/// DEVIATIONS:
/// <list type="bullet">
/// <item><description>OrgLevel1..7 collapse into the single <see cref="OrganizationUnitName"/> column.</description></item>
/// <item><description>Legacy keyed its per-component dictionary on <c>WageType.NameEn</c> (line 616), so two
/// distinct wage types sharing a name silently shared a value. Here the pivot is keyed on the wage-type
/// ID, which is identical whenever names are unique and correct when they are not.</description></item>
/// <item><description>Legacy read employee/org names as <c>NameEn</c> only, ignoring the Arabic aspect
/// on this one screen (lines 526/511-518). Kept as legacy had it.</description></item>
/// </list>
/// </summary>
public class EmployeeSalaryChangeReportRowDto : EntityDto<int>
{
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee Name" -- <c>emp.FullNameEn</c>, English regardless of culture.</summary>
    public string EmployeeName { get; set; }

    /// <summary>Replaces the legacy OrgLevel1..7 block; as of the assignment current TODAY (legacy line 507).</summary>
    public string OrganizationUnitName { get; set; }

    public EmployeeStatus Status { get; set; }

    /// <summary>Legacy "Parameter Start Date": the report's From date, echoed onto every row.</summary>
    public DateTime ParameterStartDate { get; set; }

    /// <summary>Legacy "Parameter End Date": the report's To date, echoed onto every row.</summary>
    public DateTime ParameterEndDate { get; set; }

    /// <summary>
    /// The basic-pay record the "Old" column trio was read from -- the last one STARTING BEFORE the From
    /// date (legacy lines 544-549). Null when the employee had no pay before the window. Exposed because
    /// a salary-change figure is unauditable without knowing which two records were compared.
    /// </summary>
    public int? OldBasicPayId { get; set; }

    /// <summary>
    /// The basic-pay record the "Final" column trio was read from -- the last one STARTING ON OR BEFORE
    /// the To date (legacy lines 552-556).
    /// </summary>
    public int? FinalBasicPayId { get; set; }

    /// <summary>The dynamic pivot block. Same wage types, same order, on every row.</summary>
    public List<EmployeeSalaryChangeComponentDto> Components { get; set; } = new();

    public double OldTotal { get; set; }

    public double AdjustmentTotal { get; set; }

    public double FinalTotal { get; set; }
}
