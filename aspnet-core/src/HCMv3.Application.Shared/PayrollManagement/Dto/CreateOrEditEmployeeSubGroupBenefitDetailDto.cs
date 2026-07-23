using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSubGroupBenefitDetail"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeSubGroupBenefitDetailDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeSubGroupBenefitId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public int DependantTypeId { get; set; }

    public double Amount { get; set; }

    public double AgeLimit { get; set; }
}
