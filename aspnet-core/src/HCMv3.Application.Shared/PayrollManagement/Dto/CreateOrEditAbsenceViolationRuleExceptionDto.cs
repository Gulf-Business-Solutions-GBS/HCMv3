using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="AbsenceViolationRuleException"/>. <c>Id == null</c> means
/// create; otherwise update.
/// </summary>
public class CreateOrEditAbsenceViolationRuleExceptionDto
{
    public int? Id { get; set; }

    [Required]
    public int AbsenceViolationRuleId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
