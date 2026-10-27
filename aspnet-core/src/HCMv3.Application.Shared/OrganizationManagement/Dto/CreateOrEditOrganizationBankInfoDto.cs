using System.ComponentModel.DataAnnotations;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OrganizationBankInfo"/>.
/// <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditOrganizationBankInfoDto
{
    public int? Id { get; set; }

    public long? OrganizationUnitId { get; set; }

    public int? BankId { get; set; }

    [Required]
    [StringLength(256)]
    public string AccountNo { get; set; }

    [StringLength(256)]
    public string SC_Code { get; set; }
}
