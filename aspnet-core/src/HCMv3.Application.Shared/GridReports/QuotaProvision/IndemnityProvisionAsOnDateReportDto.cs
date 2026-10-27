using System;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.QuotaProvision.Dto;

/// <summary>
/// Input for the Indemnity Provision As On Date report -- port of legacy
/// <c>uscRptIndemnityProvisionAsOnDate.ascx(.cs)</c>, <c>Table1()</c>. <see cref="GridReportInputBase.FromDate"/>
/// is the legacy <c>dtFrom</c> "As On Date" (the legacy screen has no To Date).
/// </summary>
public class GetIndemnityProvisionAsOnDateReportInput : GridReportInputBase
{
    protected override string DefaultSorting => "EmployeeCode asc";
}

public class IndemnityProvisionAsOnDateReportRowDto
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

    public string CostCenterGroupName { get; set; }

    public bool Separation { get; set; }

    public bool FnfPayment { get; set; }

    public DateTime AsOnDate { get; set; }

    public DateTime? DeductionStart { get; set; }

    public DateTime? DeductionEnd { get; set; }

    public double IndemnityRate { get; set; }

    public double IndemnityQuota { get; set; }

    public double IndemnityAmount { get; set; }

    public double PaidAmount { get; set; }

    public double BalanceAmount { get; set; }

    public string Comment { get; set; }
}
