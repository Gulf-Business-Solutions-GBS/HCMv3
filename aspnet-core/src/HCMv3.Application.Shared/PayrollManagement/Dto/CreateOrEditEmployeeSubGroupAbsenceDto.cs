using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSubGroupAbsence"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeSubGroupAbsenceDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeSubGroupId { get; set; }

    [Required]
    public int AbsenceTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public double ChangeYear { get; set; }

    public int TotalDays { get; set; }

    public int MaxLimit { get; set; }

    public int? AbsenceDeductionRuleId { get; set; }

    public int MaxLimitReachNotificationInDays { get; set; }
}
