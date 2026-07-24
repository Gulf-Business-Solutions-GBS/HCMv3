using Abp.Application.Services.Dto;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="GovtJobTitle"/>.
/// </summary>
public class GovtJobTitleDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int GovtJobCategoryId { get; set; }

    public string GovtJobCategoryName { get; set; }
}
