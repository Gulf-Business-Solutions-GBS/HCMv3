using System;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.QuotaProvision.Dto;

/// <summary>
/// Input for the Leave Quota Balance report -- port of legacy <c>uscRptLeaveQuotaBalance.ascx(.cs)</c>,
/// <c>Table1()</c>. Legacy has no From/To date field (the As-On-Date is fixed to "today") and REQUIRES a
/// leave type: with no <see cref="LeaveTypeCode"/> legacy returned an empty table (line 611-614), reproduced
/// here by <see cref="GetLeaveQuotaBalanceReportInput"/>'s callers.
/// </summary>
public class GetLeaveQuotaBalanceReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbLeaveType</c> selected Code. Required -- see class remarks.</summary>
    public string LeaveTypeCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode asc";
}

public class LeaveQuotaBalanceReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string JobTitleName { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public string LeaveTypeName { get; set; }

    public DateTime? DeductionStart { get; set; }

    public DateTime? DeductionEnd { get; set; }

    public double TotalDaysAllowed { get; set; }

    public double AccruedQuota { get; set; }

    public double UsedQuota { get; set; }

    public double AdjustedQuota { get; set; }

    public double ForfeitedQuota { get; set; }

    public double BalanceQuota { get; set; }

    public string Comment { get; set; }

    public string WorkLocationName { get; set; }
}
