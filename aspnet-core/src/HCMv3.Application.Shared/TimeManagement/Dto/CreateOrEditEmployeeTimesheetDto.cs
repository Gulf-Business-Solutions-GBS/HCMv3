using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeTimesheet"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeTimesheetDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime TransDate { get; set; }

    public TimeSpan ActualStartTime { get; set; }

    public TimeSpan BreakStartTime { get; set; }

    public TimeSpan BreakEndTime { get; set; }

    public TimeSpan ActualEndTime { get; set; }

    public double TotalHours { get; set; }

    public int Approved { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
