using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SocialSecurity"/>.
/// </summary>
public class SocialSecurityDto : EntityDto<int>
{
    public int NationalityId { get; set; }

    public string NationalityName { get; set; }

    public double EarningsCap { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double ChildAllowance { get; set; }

    public double SpecialNeedChildAllowance { get; set; }
}
