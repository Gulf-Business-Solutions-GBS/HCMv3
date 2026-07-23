using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="WageAccGroupDetail"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditWageAccGroupDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int WageAccGroupId { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    [Required]
    public int AccountInfoId { get; set; }

    public OffsetAccountInfoType OffsetAccountInfoType { get; set; }

    public int? OffsetAccountInfoId { get; set; }

    public int? CostElement1Id { get; set; }

    public int? CostElement2Id { get; set; }

    public int? CostElement3Id { get; set; }

    public int? CostElement4Id { get; set; }

    public int? CostElement5Id { get; set; }

    public int? CostElement6Id { get; set; }

    public int? CostElement7Id { get; set; }

    public double CostPercentage { get; set; }

    public bool MergeOffsetAccount { get; set; }
}
