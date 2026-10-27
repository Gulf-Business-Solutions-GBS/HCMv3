using Abp.Application.Services.Dto;

namespace HCMv3.OrganizationManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="OrganizationBankInfo"/>.
/// </summary>
public class OrganizationBankInfoDto : EntityDto<int>
{
    public long? OrganizationUnitId { get; set; }

    public string OrganizationUnitName { get; set; }

    public int? BankId { get; set; }

    public string BankName { get; set; }

    public string AccountNo { get; set; }

    public string SC_Code { get; set; }
}
