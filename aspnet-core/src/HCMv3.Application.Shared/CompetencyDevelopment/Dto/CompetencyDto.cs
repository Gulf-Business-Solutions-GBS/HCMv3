using Abp.Application.Services.Dto;

namespace HCMv3.CompetencyDevelopment.Dto;

/// <summary>Read/list DTO for <see cref="Competency"/>.</summary>
public class CompetencyDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int CompetencyTypeId { get; set; }

    public string CompetencyTypeName { get; set; }

    public string Comments { get; set; }

    public bool Active { get; set; }
}
