using System;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.QuotaProvision.Dto;

/// <summary>
/// Input for the Quota Provision As On Date report -- port of legacy
/// <c>uscRptQuotaProvisionAsOnDate.ascx(.cs)</c>, <c>Table1()</c>. <see cref="GridReportInputBase.FromDate"/>
/// is the legacy <c>dtFrom</c> "As On Date" (the legacy screen has no To Date, and no Payroll Area or
/// Employee Status filter either -- neither <c>cmbPayrollArea</c> nor <c>cmbEmpStatus</c> exist in the
/// legacy markup, so those <c>Session</c> reads in <c>Table1()</c> are always empty/no-op).
/// </summary>
public class GetQuotaProvisionAsOnDateReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbLeaveType</c> selected Code. Required -- legacy returns an empty table without one.</summary>
    public string LeaveTypeCode { get; set; }

    /// <summary>Legacy <c>chkWithProjection</c>: project accrual beyond the last Time-Engine-evaluated day up to As-On-Date.</summary>
    public bool WithProjection { get; set; }

    protected override string DefaultSorting => "EmployeeCode asc";
}

public class QuotaProvisionAsOnDateReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string Status { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public string JobTitleName { get; set; }

    public string CostCenterName { get; set; }

    public string LeaveTypeName { get; set; }

    public bool Separation { get; set; }

    public bool FnfPayment { get; set; }

    public DateTime AsOnDate { get; set; }

    public DateTime? DeductionStart { get; set; }

    public DateTime? DeductionEnd { get; set; }

    public double TotalDaysAllowed { get; set; }

    public double MaxLimit { get; set; }

    public double SystemOpeningBalanceDays { get; set; }

    public double AccruedQuota { get; set; }

    public double UsedQuota { get; set; }

    public double AdjustedQuota { get; set; }

    public double ForfeitQuota { get; set; }

    public double BalanceQuota { get; set; }

    public double BalanceProvision { get; set; }

    public string Comment { get; set; }
}
