using System.Collections.Generic;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.QuotaProvision.Dto;

namespace HCMv3.GridReports.QuotaProvision;

/// <summary>Shared export column set for the Custom016/Custom023 Yearly Leave Indemnity Detail reports.</summary>
internal static class YearlyLeaveIndemnityDetailColumns
{
    public static IReadOnlyList<GridReportColumn<YearlyLeaveIndemnityDetailReportRowDto>> Get()
    {
        return new List<GridReportColumn<YearlyLeaveIndemnityDetailReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Name", r => r.EmployeeName),
            new("Status", r => r.Status),
            new("JoiningDate", r => r.JoiningDate),
            new("LastDayInService", r => r.LastDayInService),
            new("Jobtitle", r => r.JobTitleName),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("CostCenterCode", r => r.CostCenterCode),
            new("CostCenter", r => r.CostCenterName),
            new("CostCenterGroup", r => r.CostCenterGroupName),
            new("TotalDaysAllowed", r => r.TotalDaysAllowed),
            new("OpeningBalance", r => r.OpeningBalance),
            new("AccruedQuota", r => r.AccruedQuota),
            new("UsedQuota", r => r.UsedQuota),
            new("AdjustedQuota", r => r.AdjustedQuota),
            new("ForfeitQuota", r => r.ForfeitQuota),
            new("SalaryInProvision", r => r.SalaryInProvision),
            new("ClosingBalance", r => r.ClosingBalance),
            new("ClosingProvision", r => r.ClosingProvision),
            new("IndemnityClosingBalance", r => r.IndemnityClosingBalance),
            new("IndemnityClosingBalanceAmount", r => r.IndemnityClosingBalanceAmount)
        };
    }
}
