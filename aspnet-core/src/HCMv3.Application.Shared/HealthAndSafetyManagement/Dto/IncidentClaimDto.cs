using System;
using Abp.Application.Services.Dto;

namespace HCMv3.HealthAndSafetyManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="IncidentClaim"/>.
/// </summary>
public class IncidentClaimDto : EntityDto<int>
{
    public DateTime ClaimDate { get; set; }

    public int IncidentId { get; set; }

    public double Amount { get; set; }

    public int CurrencyId { get; set; }

    public string CurrencyNameEn { get; set; }

    public DateTime PaymentDate { get; set; }

    public int? ClaimStatusId { get; set; }

    public string ClaimStatusNameEn { get; set; }

    public string Comments { get; set; }
}
