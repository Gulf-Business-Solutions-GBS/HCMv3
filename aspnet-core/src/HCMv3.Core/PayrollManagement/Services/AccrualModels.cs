using System;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// One slab-wise indemnity/gratuity calculation row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.IndemnitySlab</c>, a plain, non-persisted result POCO -- it was
/// never an XPO business object in the legacy source either, so it is kept as a plain class here too, not an
/// entity).
/// </summary>
public class IndemnitySlab
{
    public int SrNo { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double TotalDays { get; set; }

    public double UnpaidDays { get; set; }

    public double ActualDays { get; set; }

    public double IndemnityRate { get; set; }

    public double IndemnityDays { get; set; }

    public double BasisDays { get; set; }
}

/// <summary>
/// Result of a forfeit-calculation call (<c>CalculateMonthlyForfeitAsync</c>/<c>CalculateForfeitForLeaveAsync</c>/
/// <c>CalculateForfeitForPublicHolidayAsync</c>). The legacy methods were <c>void</c> and mutated
/// <c>EmployeeAbsenceQuota</c>/<c>EmployeeAbsQuotaForfeit</c> XPO objects in-place via <c>Session</c>-tracked
/// auto-save; this port's domain service performs the equivalent persistence directly via injected
/// repositories (see <see cref="IAccrualService"/> remarks) and returns this summary so callers/tests can
/// observe what happened without needing to re-query.
/// </summary>
public class ForfeitCalculationResult
{
    /// <summary>Total forfeited days computed for the period (0 if nothing was forfeited).</summary>
    public double TotalForfeitDays { get; set; }

    /// <summary>True if a new <c>EmployeeAbsQuotaForfeit</c> row was inserted.</summary>
    public bool ForfeitRecordCreated { get; set; }

    /// <summary>True if an existing <c>EmployeeAbsQuotaForfeit</c> row was updated.</summary>
    public bool ForfeitRecordUpdated { get; set; }

    /// <summary>True if an existing <c>EmployeeAbsQuotaForfeit</c> row was deleted (forfeit dropped to 0).</summary>
    public bool ForfeitRecordDeleted { get; set; }
}
