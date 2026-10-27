using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="Bank"/>.
/// </summary>
public class BankDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string ExternalCode { get; set; }

    public string SWIFTCode { get; set; }

    public string CompanyCode { get; set; }

    public int? BankFileId { get; set; }

    public string BankFileName { get; set; }
}
