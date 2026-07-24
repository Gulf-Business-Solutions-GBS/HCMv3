using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SponsorJobTitle"/>. <see cref="CitizenQuotaBalance"/>/
/// <see cref="NonCitizenQuotaBalance"/> are DTO-only computed properties (Accrued - Used), not stored
/// columns on the entity.
/// </summary>
public class SponsorJobTitleDto : EntityDto<int>
{
    public int SponsorId { get; set; }

    public string SponsorName { get; set; }

    public int GovtJobTitleId { get; set; }

    public string GovtJobTitleName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int NoofPosition { get; set; }

    public int CitizenPercent { get; set; }

    public int CitizenQuotaAccrued { get; set; }

    public int CitizenQuotaUsed { get; set; }

    public int CitizenQuotaBalance => CitizenQuotaAccrued - CitizenQuotaUsed;

    public int NonCitizenPercent { get; set; }

    public int NonCitizenQuotaAccrued { get; set; }

    public int NonCitizenQuotaUsed { get; set; }

    public int NonCitizenQuotaBalance => NonCitizenQuotaAccrued - NonCitizenQuotaUsed;
}
