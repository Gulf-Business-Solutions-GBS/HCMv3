using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AccountInfo"/>.
/// </summary>
public class AccountInfoDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int AccountTypeId { get; set; }

    public string AccountTypeName { get; set; }
}
