namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Output of <c>TravelDetailsAppService.CalculateExpectedPerDiemAsync</c> -- on-demand replacement for the
/// legacy XAF non-persistent <c>PerDiemRate</c>/<c>PerDiemAmount</c> properties. See
/// <see cref="TravelDetail"/> class remarks.
/// </summary>
public class CalculateExpectedPerDiemOutput
{
    public double PerDiemRate { get; set; }

    public double PerDiemAmount { get; set; }
}
