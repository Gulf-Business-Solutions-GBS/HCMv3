using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TravelFeedback"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditTravelFeedbackDto
{
    public int? Id { get; set; }

    /// <summary>The employee claiming. Leave empty for an applicant's claim -- exactly one of this and
    /// <see cref="ApplicantId"/> must be set (checked by the server).</summary>
    public int? EmployeeId { get; set; }

    /// <summary>The job applicant claiming, when it is not an employee's claim.</summary>
    public int? ApplicantId { get; set; }

    public int? TravelId { get; set; }

    public DateTime ClaimDate { get; set; }

    public double ClaimAmount { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    /// <summary>
    /// Record this as final instead of sending it for approval.
    ///
    /// <para>
    /// Offered only to a user holding <c>RecordWithoutApproval</c>, and only on a type whose catalog row
    /// permits a bypass. The server checks both and refuses otherwise, so this being true is a request
    /// rather than a decision.
    /// </para>
    /// </summary>
    public bool RecordDirectly { get; set; }
}
