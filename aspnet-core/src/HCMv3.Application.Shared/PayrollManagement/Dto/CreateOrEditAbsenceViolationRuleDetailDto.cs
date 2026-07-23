using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="AbsenceViolationRuleDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditAbsenceViolationRuleDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int AbsenceViolationRuleId { get; set; }

    [Required]
    public int Step { get; set; }

    public int DayInterval { get; set; }

    public AbsenceViolationAction Action { get; set; }

    /// <summary>
    /// Deferred polymorphic reference — see <see cref="AbsenceViolationRuleDetail.ActionTypeId"/> remarks.
    /// </summary>
    public int? ActionTypeId { get; set; }
}
