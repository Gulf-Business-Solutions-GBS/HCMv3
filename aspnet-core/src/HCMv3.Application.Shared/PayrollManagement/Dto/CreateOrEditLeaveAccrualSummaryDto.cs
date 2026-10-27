using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="LeaveAccrualSummary"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditLeaveAccrualSummaryDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int AbsenceTypeId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public double AccrualDays { get; set; }

    public double UsedDays { get; set; }

    public double CumulativeAccruedDays { get; set; }
}
