using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GatePassManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="GatePass"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditGatePassDto
{
    public int? Id { get; set; }

    public int GatePassNumber { get; set; }

    [Required]
    public int GatePassTypeId { get; set; }

    [Required]
    public int GatePassMovementId { get; set; }

    public int? LocationId { get; set; }

    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public DateTime ValidUpto { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int? RequestedByEmployeeId { get; set; }

    public int? GatePassStatusId { get; set; }

    [StringLength(1000)]
    public string Comments { get; set; }
}
