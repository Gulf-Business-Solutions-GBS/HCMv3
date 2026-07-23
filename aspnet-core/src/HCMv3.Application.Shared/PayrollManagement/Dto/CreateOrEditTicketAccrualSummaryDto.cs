using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TicketAccrualSummary"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditTicketAccrualSummaryDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public double TicketAccrued { get; set; }

    public double TicketUsed { get; set; }
}
