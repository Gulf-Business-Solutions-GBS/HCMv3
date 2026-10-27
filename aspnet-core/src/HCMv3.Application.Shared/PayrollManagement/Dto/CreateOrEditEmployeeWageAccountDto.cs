using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeWageAccount"/>. <c>Id == null</c> means create;
/// otherwise update. EndDate is always server-managed (auto-chained) and is not part of the input.
/// </summary>
public class CreateOrEditEmployeeWageAccountDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int WageTypeId { get; set; }

    /// <summary>"Effective Date" in the legacy UI.</summary>
    [Required]
    public DateTime StartDate { get; set; }

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

    public bool MergeOffsetAccount { get; set; }
}
