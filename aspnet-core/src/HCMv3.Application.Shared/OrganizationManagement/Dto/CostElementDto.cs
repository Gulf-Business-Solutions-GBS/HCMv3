using Abp.Application.Services.Dto;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="CostElement"/>.
/// </summary>
public class CostElementDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int? CostElementTypeId { get; set; }

    public string CostElementTypeName { get; set; }
}
