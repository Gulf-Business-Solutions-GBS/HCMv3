using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="PayeeBankDetail"/>.
/// </summary>
public class PayeeBankDetailDto : EntityDto<int>
{
    public int? BankId { get; set; }

    public string BankName { get; set; }

    public int? PayeeBankId { get; set; }

    public string PayeeBankName { get; set; }

    public string BankCode { get; set; }

    public int? BankFileId { get; set; }

    public string BankFileName { get; set; }
}
