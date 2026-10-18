using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="Institution"/>.
/// </summary>
public class InstitutionDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int? UniversityId { get; set; }

    public string UniversityName { get; set; }

    public int? CountryId { get; set; }

    public string CountryName { get; set; }
}
