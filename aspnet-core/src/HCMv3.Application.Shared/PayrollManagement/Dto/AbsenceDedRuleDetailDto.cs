using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AbsenceDedRuleDetail"/>.
/// </summary>
public class AbsenceDedRuleDetailDto : EntityDto<int>
{
    public int AbsenceDeductionRuleId { get; set; }

    public string AbsenceDeductionRuleName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public double DaysCount { get; set; }

    public DeductionUnit DeductionUnit { get; set; }

    public double DeductionNumber { get; set; }
}
