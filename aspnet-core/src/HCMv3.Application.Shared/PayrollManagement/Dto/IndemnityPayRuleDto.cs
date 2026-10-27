using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="IndemnityPayRule"/>.
/// </summary>
public class IndemnityPayRuleDto : EntityDto<int>
{
    public PersonnelAdministration.SeparationType SeparationType { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double MaxNumber { get; set; }

    public Unit MaxUnit { get; set; }

    public double MaxPayPercentage { get; set; }

    public int? CountryId { get; set; }

    public string CountryName { get; set; }
}
