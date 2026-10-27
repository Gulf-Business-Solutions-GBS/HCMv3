using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Create/Update input DTO for <see cref="CompetencyType"/>. <c>Id == null</c> means create.</summary>
public class CreateOrEditCompetencyTypeDto
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

    [StringLength(50)]
    public string BackColor { get; set; }

    [StringLength(50)]
    public string ForeColor { get; set; }
}
