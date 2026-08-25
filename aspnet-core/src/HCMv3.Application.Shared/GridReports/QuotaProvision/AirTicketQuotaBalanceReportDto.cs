using System;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.QuotaProvision.Dto;

/// <summary>
/// Input for the Air Ticket Quota Balance report -- port of legacy
/// <c>uscRptAirTicketQuotaBalance.ascx(.cs)</c>. <see cref="GridReportInputBase.FromDate"/> is the legacy
/// <c>dtFrom</c> "As On Date" (the legacy screen has no separate To Date). Legacy hard-codes the employee
/// set to <c>Status == Active</c> (no status combo on this screen) -- <see cref="GridReportInputBase.EmployeeStatus"/>
/// is therefore ignored by this report; it is inherited only because every report shares the base filter
/// contract.
/// </summary>
public class GetAirTicketQuotaBalanceReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>cmbTicketType</c> selected Code. Null/blank means "all ticket types" (legacy loops every TicketType).</summary>
    public string TicketTypeCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode asc, TicketTypeName asc";
}

public class AirTicketQuotaBalanceReportRowDto
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

    public string TicketTypeName { get; set; }

    public DateTime AsOnDate { get; set; }

    public DateTime? DeductionStart { get; set; }

    public DateTime? DeductionEnd { get; set; }

    public double AccruedQuota { get; set; }

    public double UsedQuota { get; set; }

    public double AdjustedQuota { get; set; }

    public double BalanceQuota { get; set; }

    public string Comment { get; set; }
}
