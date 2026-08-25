using System;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.QuotaProvision.Dto;

/// <summary>
/// Shared input for the Yearly Leave Indemnity Detail Custom016/Custom023 reports -- port of legacy
/// <c>uscRptYearlyLeaveIndemnityDetail_Custom016.ascx(.cs)</c> and
/// <c>uscRptYearlyLeaveIndemnityDetail_Custom023.ascx(.cs)</c>, whose <c>Table1()</c> bodies are BYTE-FOR-BYTE
/// IDENTICAL business logic (diffed against each other -- only screen/session-key names differ). Both legacy
/// screens are ported to separate app services (different legacy screen names/permissions/execution-log
/// entries, matching how every other Custom0xx pair in this codebase is ported), sharing this one input/row
/// DTO pair and the query logic in <see cref="YearlyLeaveIndemnityDetailReportQueryHelper"/>.
///
/// <see cref="GridReportInputBase.ToDate"/> is the legacy <c>txtYear</c>/<c>EndDate</c> "As On Date" (year-end
/// snapshot); <see cref="GridReportInputBase.FromDate"/> is unused by this screen (there is no From filter in
/// the legacy markup) but inherited for filter-contract consistency.
/// </summary>
public class GetYearlyLeaveIndemnityDetailReportInput : GridReportInputBase
{
    /// <summary>Legacy <c>payrollArea</c> Session key. Null/blank means "all payroll areas".</summary>
    public string PayrollAreaCode { get; set; }

    protected override string DefaultSorting => "EmployeeCode asc";
}

public class YearlyLeaveIndemnityDetailReportRowDto
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    public string Status { get; set; }

    public DateTime JoiningDate { get; set; }

    public DateTime? LastDayInService { get; set; }

    public string JobTitleName { get; set; }

    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }

    public string CostCenterCode { get; set; }

    public string CostCenterName { get; set; }

    public string CostCenterGroupName { get; set; }

    public double TotalDaysAllowed { get; set; }

    public double OpeningBalance { get; set; }

    public double AccruedQuota { get; set; }

    public double UsedQuota { get; set; }

    public double AdjustedQuota { get; set; }

    public double ForfeitQuota { get; set; }

    public double SalaryInProvision { get; set; }

    public double ClosingBalance { get; set; }

    public double ClosingProvision { get; set; }

    public double IndemnityClosingBalance { get; set; }

    public double IndemnityClosingBalanceAmount { get; set; }
}
