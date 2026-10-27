using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AbsenceDeductionRule"/>.
/// </summary>
public class AbsenceDeductionRuleDto : EntityDto<int>
{
    public string Code { get; set; }

    public string RuleName { get; set; }
}
