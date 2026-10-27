using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="CostRegisterDetail"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditCostRegisterDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int CostRegisterId { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    [Required]
    public int AccountInfoId { get; set; }

    public int? CostElement1Id { get; set; }

    public int? CostElement2Id { get; set; }

    public int? CostElement3Id { get; set; }

    public int? CostElement4Id { get; set; }

    public int? CostElement5Id { get; set; }

    public int? CostElement6Id { get; set; }

    public int? CostElement7Id { get; set; }

    public double Amount { get; set; }

    public int? CurrencyId { get; set; }
}
