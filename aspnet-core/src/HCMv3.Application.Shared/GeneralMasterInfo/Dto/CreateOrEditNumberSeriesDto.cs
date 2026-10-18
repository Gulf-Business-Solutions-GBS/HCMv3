using System.ComponentModel.DataAnnotations;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="NumberSeries"/>. <c>Id == null</c> means create; otherwise update.
/// Unlike other GeneralMasterInfo masters, Code is required as-is (the legacy entity does not
/// auto-generate it) and there's no NameEn/NameAr.
/// </summary>
public class CreateOrEditNumberSeriesDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string Name { get; set; }

    public long Start { get; set; }

    [StringLength(64)]
    public string Prefix { get; set; }

    [Required]
    [StringLength(64)]
    public string Formatting { get; set; }

    /// <summary>Which employees this series numbers; <c>ALL</c> = everyone. Legacy <c>NumberSeries.EmploymentType</c>.</summary>
    public EmploymentType EmploymentType { get; set; }
}
