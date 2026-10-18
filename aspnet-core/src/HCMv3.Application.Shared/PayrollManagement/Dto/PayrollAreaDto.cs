using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="PayrollArea"/>.
/// </summary>
public class PayrollAreaDto : EntityDto<int>
{
    public string Code { get; set; }

    public string Name { get; set; }

    public int PayrollPeriodId { get; set; }

    public string PayrollPeriodName { get; set; }

    public int CurrencyId { get; set; }

    public string CurrencyName { get; set; }

    public int CutOffDay { get; set; }

    public int? FICompanyId { get; set; }

    public string FICompanyName { get; set; }

    public int? FIBankId { get; set; }

    public string FIBankName { get; set; }

    public int? CountryId { get; set; }

    public string CountryName { get; set; }

    public int? PAReleaseNotificationId { get; set; }

    public string PAReleaseNotificationName { get; set; }

    public int? PAExitNotificationId { get; set; }

    public string PAExitNotificationName { get; set; }

    public int? PACorrectionNotificationId { get; set; }

    public string PACorrectionNotificationName { get; set; }
}
