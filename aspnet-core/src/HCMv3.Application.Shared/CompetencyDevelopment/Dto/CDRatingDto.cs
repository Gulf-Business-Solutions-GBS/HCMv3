using Abp.Application.Services.Dto;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Read/list DTO for <see cref="CDRating"/>.</summary>
public class CDRatingDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}
