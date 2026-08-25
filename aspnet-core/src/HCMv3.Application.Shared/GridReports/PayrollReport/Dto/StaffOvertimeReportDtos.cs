using HCMv3.GridReports.Dto;
using HCMv3.PayrollManagement;

namespace HCMv3.GridReports.PayrollReport.Dto;

/// <summary>
/// Filter input for the Staff Overtime grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptStaffOvertime.ascx</c>).
/// </summary>
public class GetStaffOvertimeReportInput : GridReportInputBase
{
    public int? Month { get; set; }

    public int? Year { get; set; }

    /// <summary>Legacy <c>cmbRunType</c>. Null means "all run types".</summary>
    public PayrollRunType? PayrollRunType { get; set; }

    /// <summary>Legacy <c>cmbPayrollArea</c>: filters via the org assignment current as of EndDate.</summary>
    public int? PayrollAreaId { get; set; }

    protected override string DefaultSorting => "EmployeeCode ASC";
}

/// <summary>
/// One grid row of legacy <c>Table1()</c> (lines 523-712): per-employee overtime hours/amounts for the
/// reported month, split into Normal Day (1007/7007), Weekend (1008/7008), Public Holiday (1009/7009) and
/// Special (1042/7042) -- each column SUMS the regular AND arrear wage type together.
/// </summary>
public class StaffOvertimeReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitle { get; set; }

    public string PayrollAreaName { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public double NormalDay { get; set; }

    public double NormalDayAmount { get; set; }

    public double Weekend { get; set; }

    public double WeekendAmount { get; set; }

    public double PublicHoliday { get; set; }

    public double PublicHolidayAmount { get; set; }

    public double SpecialDay { get; set; }

    public double SpecialDayAmount { get; set; }
}
