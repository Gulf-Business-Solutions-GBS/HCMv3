using System.ComponentModel.DataAnnotations;

namespace HCMv3.PerformanceManagement.Dto;

/// <summary>
/// Input for the appraisal-progress chart, ported from legacy
/// <c>HCM10.Web/UserControls/PerformanceAppraisal/uscPerformanceAppraisal.ascx.cs</c>'s
/// <c>cmbPAPeriod</c> combo.
/// </summary>
public class GetPerformanceAppraisalDashboardInput
{
    /// <summary>
    /// Legacy matched the period by its NAME (<c>x.Period.Name == cmbPAPeriod.Text</c>, line 114). This
    /// port keys on the id instead -- <c>PAPeriod.Name</c> is not unique-constrained, so matching on it
    /// would be ambiguous.
    /// </summary>
    [Required]
    public int PAPeriodId { get; set; }
}

/// <summary>One stage (Plan / Mid-Year / End-Year) of the legacy full-stacked bar chart.</summary>
public class PerformanceAppraisalStageStatusDto
{
    /// <summary>"PLAN", "MID-YEAR" or "END-YEAR" -- legacy's series point arguments (lines 140-150).</summary>
    public string Stage { get; set; }

    /// <summary>Legacy series 1: both appraiser AND appraisee submitted.</summary>
    public int Completed { get; set; }

    /// <summary>Legacy series 2: the balance -- total employees minus completed minus pending-by-appraisee.</summary>
    public int PendingByAppraiser { get; set; }

    /// <summary>Legacy series 3: appraiser submitted, appraisee has not.</summary>
    public int PendingByAppraisee { get; set; }
}

/// <summary>Output of <c>GetPerformanceAppraisalDashboard</c>.</summary>
public class PerformanceAppraisalDashboardDto
{
    public int PAPeriodId { get; set; }

    public string PAPeriodName { get; set; }

    /// <summary>Legacy <c>count</c> (line 118) -- active employees visible to the caller. Also the chart's
    /// X-axis maximum (legacy line 175).</summary>
    public int EmployeeCount { get; set; }

    public PerformanceAppraisalStageStatusDto Plan { get; set; }

    public PerformanceAppraisalStageStatusDto MidYearReview { get; set; }

    public PerformanceAppraisalStageStatusDto EndYearReview { get; set; }
}
