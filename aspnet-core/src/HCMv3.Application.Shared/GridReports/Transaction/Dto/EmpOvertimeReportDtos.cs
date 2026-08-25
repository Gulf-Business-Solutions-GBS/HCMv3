using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees.Dto;

namespace HCMv3.GridReports.Transaction.Dto;

/// <summary>
/// Filter input for the Employee Overtime analysis grid report (legacy <c>uscRptEmpOvertime.ascx</c>).
/// Adds the report-specific <c>cmbOvertimeType</c> combo. Legacy has no employee-status filter.
/// </summary>
public class GetEmpOvertimeReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbOvertimeType</c>: OvertimeType.Code. Null/empty means "all overtime types".</summary>
    public string OvertimeTypeCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row: one employee, overtime hours SUMMED over the window and split into four legacy buckets.
/// Mirrors legacy <c>Table1()</c> (uscRptEmpOvertime.ascx.cs:498-676). Drill-down <c>Table2()</c> (the
/// same employee's individual overtime rows) is NOT PORTED -- DevExpress drill-down UI plumbing.
///
/// FIDELITY NOTE: legacy buckets hours by comparing <c>OvertimeType.NameEn</c> against four hardcoded
/// English literals ("Normal Day OT", "Weekend OT", "Public Holiday OT", "Special OT") rather than a
/// coded/typed classification -- any overtime type named differently falls into none of the four buckets.
/// Reproduced exactly (see the app service and its pinning test).
/// </summary>
public class EmpOvertimeReportRowDto : EntityDto<int>, IEmployeeOrgLevelRow
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    public string PayrollAreaName { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }
    public long? OrganizationUnitId { get; set; }

    public double NormalHours { get; set; }

    public double WeekendHours { get; set; }

    public double PublicHolidayHours { get; set; }

    public double SpecialHours { get; set; }

    public double TotalHours { get; set; }

    /// <summary>
    /// Not ported on <c>EmployeeOvertime</c> (see the entity's remarks: the wage-rate calculation was
    /// never brought over). Always 0 -- see <see cref="EmpOvertimeReportAppService"/> remarks.
    /// </summary>
    public double ExpectedOvertimeAmount { get; set; }

    public string CountryName { get; set; }
}
