using Abp.Application.Services.Dto;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SponsorSignatory"/>.
/// </summary>
public class SponsorSignatoryDto : EntityDto<int>
{
    public int SponsorId { get; set; }

    public string SponsorName { get; set; }

    public int SignatoryId { get; set; }

    public string SignatoryName { get; set; }
}
