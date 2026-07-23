using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="GratuityAccrualSummary"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditGratuityAccrualSummaryDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public double GratuityDaysAccrued { get; set; }

    public double GratuityDaysBalance { get; set; }

    public double GratuityAmount { get; set; }

    public double GratuityAmountPaid { get; set; }
}
