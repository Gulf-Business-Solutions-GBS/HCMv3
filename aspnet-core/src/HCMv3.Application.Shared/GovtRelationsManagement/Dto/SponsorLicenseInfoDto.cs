using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SponsorLicenseInfo"/>.
/// </summary>
public class SponsorLicenseInfoDto : EntityDto<int>
{
    public int SponsorId { get; set; }

    public string SponsorName { get; set; }

    public string TradeLicenseNum { get; set; }

    public DateTime LicenseIssueDate { get; set; }

    public DateTime LicenseExpiryDate { get; set; }

    public long CivilId { get; set; }

    public string ActivityType { get; set; }
}
