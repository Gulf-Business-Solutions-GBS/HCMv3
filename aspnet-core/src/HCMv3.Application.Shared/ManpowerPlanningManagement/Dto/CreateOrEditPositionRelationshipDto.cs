using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="PositionRelationship"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditPositionRelationshipDto
{
    public int? Id { get; set; }

    [Required]
    public int PositionId { get; set; }

    public PositionRelationType RelationType { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
