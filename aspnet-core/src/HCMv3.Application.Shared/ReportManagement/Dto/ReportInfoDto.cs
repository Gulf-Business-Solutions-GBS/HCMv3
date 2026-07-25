using Abp.Application.Services.Dto;

namespace HCMv3.ReportManagement.Dto;

/// <summary>Read/list DTO for <see cref="ReportInfo"/>.</summary>
public class ReportInfoDto : EntityDto<int>
{
    public string ReportName { get; set; }

    public int ReportTypeId { get; set; }

    public string ReportTypeName { get; set; }

    public int DomainTypeId { get; set; }

    public string DomainTypeName { get; set; }

    public string Description { get; set; }

    public string ReportQuery { get; set; }

    public bool IsActive { get; set; }
}
