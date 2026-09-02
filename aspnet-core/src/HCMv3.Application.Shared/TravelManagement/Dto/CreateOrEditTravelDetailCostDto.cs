using System.ComponentModel.DataAnnotations;

namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TravelDetailCost"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditTravelDetailCostDto
{
    public int? Id { get; set; }

    [Required]
    public int TravelDetailId { get; set; }

    [Required]
    public int ExpenseTypeId { get; set; }

    /// <summary>What <see cref="Amount"/> is in. See <c>TravelDetailCost.CurrencyId</c>.</summary>
    public int? CurrencyId { get; set; }

    public double Amount { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}
