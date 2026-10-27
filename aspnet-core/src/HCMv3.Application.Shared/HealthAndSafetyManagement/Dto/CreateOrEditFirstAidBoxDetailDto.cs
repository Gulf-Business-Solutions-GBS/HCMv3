using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="FirstAidBoxDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditFirstAidBoxDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int FirstAidBoxId { get; set; }

    [Required]
    public int MedicineId { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [Required]
    public DateTime AllocationDate { get; set; }

    [Required]
    public int AllocatedById { get; set; }

    public bool IsExpired { get; set; }

    public bool IsReplaced { get; set; }

    public string Comments { get; set; }
}
