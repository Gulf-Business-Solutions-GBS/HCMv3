using System;
using Abp.Application.Services.Dto;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="IncidentRegister"/>.
/// </summary>
public class IncidentRegisterDto : EntityDto<int>
{
    public DateTime IncidentDate { get; set; }

    public DateTime IncidentTime { get; set; }

    public int IncidentTypeId { get; set; }

    public string IncidentTypeNameEn { get; set; }

    public string Description { get; set; }

    public int NatureId { get; set; }

    public string NatureNameEn { get; set; }

    public int SeverityId { get; set; }

    public string SeverityNameEn { get; set; }

    public int? ReportedById { get; set; }

    public string ReportedByCode { get; set; }

    public int? PersonInvolvedId { get; set; }

    public string PersonInvolvedCode { get; set; }

    public string OtherPeopleInvolved { get; set; }

    public int? LocationId { get; set; }

    public string LocationName { get; set; }

    public int? AssignedToId { get; set; }

    public string AssignedToCode { get; set; }

    public string DisabilityPercentage { get; set; }

    public int? StatusId { get; set; }

    public string StatusNameEn { get; set; }

    public string Comments { get; set; }
}
