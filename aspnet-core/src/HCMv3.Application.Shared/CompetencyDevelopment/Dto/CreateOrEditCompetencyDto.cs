using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Create/Update input DTO for <see cref="Competency"/>. <c>Id == null</c> means create.</summary>
public class CreateOrEditCompetencyDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    [Required]
    public int CompetencyTypeId { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    public bool Active { get; set; }
}
