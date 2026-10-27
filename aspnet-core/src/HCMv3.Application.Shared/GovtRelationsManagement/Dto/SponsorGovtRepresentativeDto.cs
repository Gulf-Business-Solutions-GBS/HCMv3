using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SponsorGovtRepresentative"/>.
/// </summary>
public class SponsorGovtRepresentativeDto : EntityDto<int>
{
    public int SponsorId { get; set; }

    public string SponsorName { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
