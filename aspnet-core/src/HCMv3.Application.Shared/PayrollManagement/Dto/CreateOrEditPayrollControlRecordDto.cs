using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="PayrollControlRecord"/>. <c>Id == null</c> means create;
/// otherwise update. CalendarMonthStart/CalendarMonthEnd are always server-computed (see
/// <see cref="PayrollPeriodShiftCalculator"/>) and are not part of the input.
/// </summary>
public class CreateOrEditPayrollControlRecordDto
{
    public int? Id { get; set; }

    [Required]
    public int PayrollAreaId { get; set; }

    [Required]
    public int PayrollMonth { get; set; }

    [Required]
    public int PayrollYear { get; set; }

    public bool PeriodLocked { get; set; }

    public bool PeriodClosed { get; set; }
}
