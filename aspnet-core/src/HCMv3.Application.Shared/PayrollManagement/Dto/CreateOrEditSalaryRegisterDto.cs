using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SalaryRegister"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditSalaryRegisterDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime ForPeriod { get; set; }

    [Required]
    public DateTime InPeriod { get; set; }

    public PayrollRunType PayrollRunType { get; set; }

    public PayrollResultType PayrollResultType { get; set; }

    /// <summary>
    /// Optional: a REGULAR run has no reason. See <see cref="SalaryRegister.PayrollReasonTypeId"/>.
    /// </summary>
    public int? PayrollReasonTypeId { get; set; }

    public DateTime OffCyclePaymentDate { get; set; }

    public bool Locked { get; set; }
}
