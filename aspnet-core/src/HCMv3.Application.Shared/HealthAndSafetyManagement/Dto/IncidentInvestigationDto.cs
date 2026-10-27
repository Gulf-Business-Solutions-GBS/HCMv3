using System;
using Abp.Application.Services.Dto;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="IncidentInvestigation"/>.
/// </summary>
public class IncidentInvestigationDto : EntityDto<int>
{
    public DateTime InvestigationDate { get; set; }

    public DateTime InvestigationTime { get; set; }

    public int IncidentId { get; set; }

    public int RootCauseId { get; set; }

    public string RootCauseNameEn { get; set; }

    public string RootCauseAnalysis { get; set; }

    public string Comments { get; set; }
}
