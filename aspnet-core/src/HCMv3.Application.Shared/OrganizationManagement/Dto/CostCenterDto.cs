using Abp.Application.Services.Dto;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="CostCenter"/>.
/// </summary>
public class CostCenterDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int? CostCenterGroupId { get; set; }

    public string CostCenterGroupName { get; set; }
}
