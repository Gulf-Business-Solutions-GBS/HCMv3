using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AbsenceViolationRuleDetail"/>.
/// </summary>
public class AbsenceViolationRuleDetailDto : EntityDto<int>
{
    public int AbsenceViolationRuleId { get; set; }

    public string AbsenceViolationRuleName { get; set; }

    public int Step { get; set; }

    public int DayInterval { get; set; }

    public AbsenceViolationAction Action { get; set; }

    public int? ActionTypeId { get; set; }
}
