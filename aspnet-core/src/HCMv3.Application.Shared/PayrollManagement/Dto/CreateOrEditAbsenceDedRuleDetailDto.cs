using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="AbsenceDedRuleDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditAbsenceDedRuleDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int AbsenceDeductionRuleId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public double DaysCount { get; set; }

    public DeductionUnit DeductionUnit { get; set; }

    public double DeductionNumber { get; set; }
}
