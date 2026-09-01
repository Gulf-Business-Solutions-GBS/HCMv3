using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeExitPermit"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditEmployeeExitPermitDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    /// <summary>
    /// The government's permit number, keyed in by whoever has the document. Required here as well as in the
    /// workflow handler: this is the early, friendlier failure, the handler is the rule.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string ExternalRequestNumber { get; set; }

    [Required]
    public TravelReason TravelReason { get; set; }

    [Required]
    public DateTime ExitDate { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [StringLength(1000)]
    public string Comments { get; set; }
}
