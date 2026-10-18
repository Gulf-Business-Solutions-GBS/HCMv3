namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Output of <c>TravelDetailsAppService.CalculateExpectedPerDiemAsync</c> -- on-demand replacement for the
/// legacy XAF non-persistent <c>PerDiemRate</c>/<c>PerDiemAmount</c> properties. See
/// <see cref="TravelDetail"/> class remarks.
/// </summary>
public class CalculateExpectedPerDiemOutput
{
    public double PerDiemRate { get; set; }

    /// <summary>Legacy <c>PerDiemAmount</c>: Round(TotalNights x PerDiemRate, 3). Not reduced by the pay percentage.</summary>
    public double PerDiemAmount { get; set; }

    /// <summary>The leg's <c>PerDiemPayPercentage</c>.</summary>
    public double PerDiemPayPercentage { get; set; }

    /// <summary>
    /// What is actually paid for the leg: <c>PerDiemAmount * PerDiemPayPercentage / 100</c>, exactly as legacy's
    /// payroll <c>GetTravelPerDiemPay</c> computed it (no further rounding).
    /// </summary>
    public double PerDiemPayAmount { get; set; }
}
