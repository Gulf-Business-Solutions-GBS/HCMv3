using System;
using Abp.Application.Services.Dto;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="RiskRegister"/>.
/// </summary>
public class RiskRegisterDto : EntityDto<int>
{
    public DateTime ReportingDate { get; set; }

    public DateTime ReportingTime { get; set; }

    public int RiskTypeId { get; set; }

    public string RiskTypeNameEn { get; set; }

    public string Description { get; set; }

    public int ImpactId { get; set; }

    public string ImpactNameEn { get; set; }

    public int PossibilityId { get; set; }

    public string PossibilityNameEn { get; set; }

    public int ReportedById { get; set; }

    public string ReportedByCode { get; set; }

    public int LocationId { get; set; }

    public string LocationName { get; set; }

    public int? AssignedToId { get; set; }

    public string AssignedToCode { get; set; }

    public string RiskMitigation { get; set; }

    public int? ActionId { get; set; }

    public string ActionNameEn { get; set; }

    public int? StatusId { get; set; }

    public string StatusNameEn { get; set; }

    public DateTime ReassessmentDate { get; set; }
}
