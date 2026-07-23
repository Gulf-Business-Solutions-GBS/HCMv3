using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="WageAccount"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditWageAccountDto
{
    public int? Id { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    /// <summary>"Effective Date" in the legacy UI.</summary>
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public int AccountInfoId { get; set; }

    public int? OffsetAccountInfoId { get; set; }

    public int? CostElement1Id { get; set; }

    public int? CostElement2Id { get; set; }

    public int? CostElement3Id { get; set; }

    public int? CostElement4Id { get; set; }

    public int? CostElement5Id { get; set; }

    public int? CostElement6Id { get; set; }

    public int? CostElement7Id { get; set; }

    public double CostPercentage { get; set; }
}
