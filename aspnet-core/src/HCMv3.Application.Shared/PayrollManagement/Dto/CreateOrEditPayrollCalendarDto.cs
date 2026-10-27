using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="PayrollCalendar"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditPayrollCalendarDto
{
    public int? Id { get; set; }

    [Required]
    public int PayrollAreaId { get; set; }

    [Required]
    public int PayrollMonth { get; set; }

    [Required]
    public int PayrollYear { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
