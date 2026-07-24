using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Travel"/>. <c>Id == null</c> means create; otherwise update.
/// <see cref="Travel.TotalTravelCost"/> is intentionally NOT settable here -- see that entity's remarks.
/// </summary>
public class CreateOrEditTravelDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code (legacy <c>GetMaxCode</c> parity).
    /// </summary>
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int TravelTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
