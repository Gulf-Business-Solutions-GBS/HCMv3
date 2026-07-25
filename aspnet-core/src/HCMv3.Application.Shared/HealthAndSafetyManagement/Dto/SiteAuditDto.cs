using System;
using Abp.Application.Services.Dto;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SiteAudit"/>.
/// </summary>
public class SiteAuditDto : EntityDto<int>
{
    public DateTime AuditDate { get; set; }

    public DateTime AuditTime { get; set; }

    public int LocationId { get; set; }

    public string LocationName { get; set; }

    public int ConductedById { get; set; }

    public string ConductedByCode { get; set; }

    public string Comments { get; set; }

    public DateTime ReassessmentDate { get; set; }
}
