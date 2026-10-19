namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Output of <c>GetTravelForEdit</c>.
/// </summary>
public class GetTravelForEditOutput
{
    public CreateOrEditTravelDto Travel { get; set; }

    /// <summary>
    /// Read-only, for display. Kept out of <see cref="CreateOrEditTravelDto"/> so it is never posted back:
    /// the server works it out from the legs (legacy <c>Travel.TotalTravelCost</c>, disabled on the form).
    /// </summary>
    public double TotalTravelCost { get; set; }
}
