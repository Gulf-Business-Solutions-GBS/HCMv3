using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="RosterGroupDetail"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditRosterGroupDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int RosterGroupId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
