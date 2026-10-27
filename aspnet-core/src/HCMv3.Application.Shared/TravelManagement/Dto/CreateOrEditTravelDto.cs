using System;
using System.Collections.Generic;
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

    /// <summary>The employee travelling. Leave empty for an applicant trip -- exactly one of this and
    /// <see cref="ApplicantId"/> must be set (checked by the server).</summary>
    public int? EmployeeId { get; set; }

    /// <summary>The job applicant travelling, when the trip is not an employee's.</summary>
    public int? ApplicantId { get; set; }

    [Required]
    public int TravelTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>Why the trip is being made -- see <c>Travel.TravelPurpose</c>.</summary>
    [StringLength(4000)]
    public string TravelPurpose { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    /// <summary>
    /// The legs, on CREATE only. The travel desk books on somebody's behalf, and that booking goes through
    /// the same approval chain as a self-service request -- which refuses a trip with no legs. So the legs
    /// have to arrive with the header, in the same entity graph, rather than being added afterwards through
    /// <c>ITravelDetailsAppService</c>. Ignored on update: an existing trip's legs are edited there.
    /// </summary>
    public List<CreateOrEditTravelDetailDto> TravelDetails { get; set; }
}
