using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSubGroupHealthReimburse"/>. <c>Id == null</c> means
/// create; otherwise update.
/// </summary>
public class CreateOrEditEmployeeSubGroupHealthReimburseDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeSubGroupId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public int DependantTypeId { get; set; }

    public double Amount { get; set; }

    public double AgeLimit { get; set; }
}
