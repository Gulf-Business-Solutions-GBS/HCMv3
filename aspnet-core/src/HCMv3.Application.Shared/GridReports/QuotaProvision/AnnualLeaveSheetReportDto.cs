using System;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.QuotaProvision.Dto;

/// <summary>
/// Input for the Annual Leave Sheet report -- port of legacy <c>uscRptAnnualLeaveSheet.ascx(.cs)</c>,
/// <c>Table1()</c>. <see cref="GridReportInputBase.FromDate"/> is the legacy <c>dtFrom</c> "As On Date"
/// (the legacy screen has no To Date). Legacy also had no Employee Status combo on this screen, so
/// <see cref="GridReportInputBase.EmployeeStatus"/> is ignored -- the employee set is hard-coded to Active.
/// </summary>
public class GetAnnualLeaveSheetReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbPayrollArea</c> selected Code. Null/blank means "all payroll areas".</summary>
    public string PayrollAreaCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode asc";
}

public class AnnualLeaveSheetReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string OrgLevel1 { get; set; }

    public string OrgLevel2 { get; set; }

    public string OrgLevel3 { get; set; }

    public string OrgLevel4 { get; set; }

    public string OrgLevel5 { get; set; }

    public string OrgLevel6 { get; set; }

    public string OrgLevel7 { get; set; }

    public string JobTitleName { get; set; }

    public string CostCenterName { get; set; }

    public DateTime AsOnDate { get; set; }

    public double PerYearQuota { get; set; }

    public double MaxLimit { get; set; }

    public double OpeningBalance { get; set; }

    public double QuotaAccrued { get; set; }

    public double QuotaUsed { get; set; }

    public double QuotaAdjusted { get; set; }

    /// <summary>Only meaningful (legacy only showed the column) when <c>ForfeitRule == Monthly</c>.</summary>
    public double QuotaForfeited { get; set; }

    public double QuotaBalance { get; set; }
}
