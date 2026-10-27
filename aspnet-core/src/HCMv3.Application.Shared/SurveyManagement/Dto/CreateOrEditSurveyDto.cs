using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SurveyManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="Survey"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditSurveyDto
{
    public int? Id { get; set; }

    [StringLength(GeneralMasterInfo.SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(GeneralMasterInfo.SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(GeneralMasterInfo.SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
