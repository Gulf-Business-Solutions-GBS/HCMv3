using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="PayeeBankDetail"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditPayeeBankDetailDto
{
    public int? Id { get; set; }

    public int? BankId { get; set; }

    public int? PayeeBankId { get; set; }

    [StringLength(256)]
    public string BankCode { get; set; }

    public int? BankFileId { get; set; }
}
