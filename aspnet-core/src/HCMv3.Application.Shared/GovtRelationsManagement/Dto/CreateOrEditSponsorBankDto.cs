using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GovtRelationsManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="SponsorBank"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditSponsorBankDto
{
    public int? Id { get; set; }

    [Required]
    public int SponsorId { get; set; }

    public int? EmployeeBankId { get; set; }

    [Required]
    public int PaymentBankId { get; set; }

    [StringLength(256)]
    public string AccountNumber { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(64)]
    public string IBAN { get; set; }
}
