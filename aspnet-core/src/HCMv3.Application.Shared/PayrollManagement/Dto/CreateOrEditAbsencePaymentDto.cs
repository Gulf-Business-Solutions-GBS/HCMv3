using System.ComponentModel.DataAnnotations;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="AbsencePayment"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditAbsencePaymentDto
{
    public int? Id { get; set; }

    [Required]
    public int AbsenceTypeId { get; set; }

    [Required]
    public int WageTypeId { get; set; }
}
