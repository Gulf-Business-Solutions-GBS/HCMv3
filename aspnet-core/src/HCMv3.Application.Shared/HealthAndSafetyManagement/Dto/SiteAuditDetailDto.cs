using Abp.Application.Services.Dto;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SiteAuditDetail"/>.
/// </summary>
public class SiteAuditDetailDto : EntityDto<int>
{
    public int SiteAuditId { get; set; }

    public string Observation { get; set; }

    public string CorrectionAction { get; set; }
}
