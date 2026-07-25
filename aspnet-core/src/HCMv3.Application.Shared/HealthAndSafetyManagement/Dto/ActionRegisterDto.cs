using System;
using Abp.Application.Services.Dto;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="ActionRegister"/>.
/// </summary>
public class ActionRegisterDto : EntityDto<int>
{
    public DateTime ActionDate { get; set; }

    public string Description { get; set; }

    public int IncidentId { get; set; }

    public int RiskId { get; set; }

    public int AssignedToId { get; set; }

    public string AssignedToCode { get; set; }

    public int? StatusId { get; set; }

    public string StatusNameEn { get; set; }
}
