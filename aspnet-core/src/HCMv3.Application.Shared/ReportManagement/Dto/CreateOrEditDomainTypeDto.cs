using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Create/Update input DTO for <see cref="DomainType"/>. <c>Id == null</c> means create.</summary>
public class CreateOrEditDomainTypeDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [StringLength(512)]
    public string EntityType { get; set; }
}
