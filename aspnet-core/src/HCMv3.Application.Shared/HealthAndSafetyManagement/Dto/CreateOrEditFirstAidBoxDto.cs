using System;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="FirstAidBox"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditFirstAidBoxDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    public int LocationId { get; set; }

    [Required]
    public DateTime AllocationDate { get; set; }

    [Required]
    public int ResponsiblePersonId { get; set; }

    public string Comments { get; set; }
}
