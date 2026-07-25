using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Create/Update input DTO for <see cref="ReportType"/>. <c>Id == null</c> means create.</summary>
public class CreateOrEditReportTypeDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }
}
