using System;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SocialSecurityRule"/>.
/// </summary>
public class SocialSecurityRuleDto : EntityDto<int>
{
    public int SocialSecurityId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int QualificationId { get; set; }

    public string QualificationName { get; set; }

    public Gender Gender { get; set; }

    public int? MaritalStatusId { get; set; }

    public string MaritalStatusName { get; set; }

    public int Dependant { get; set; }

    public bool Disability { get; set; }

    public double Amount { get; set; }
}
