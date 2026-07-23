using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="EmployeeSubGroupPayment"/>. <c>Id == null</c> means create;
/// otherwise update.
/// </summary>
public class CreateOrEditEmployeeSubGroupPaymentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeSubGroupId { get; set; }

    public int? WageTypeId { get; set; }

    public double Amount { get; set; }

    public PayInterval PayInterval { get; set; }

    public CustomStartMonth PayMonth { get; set; }
}
