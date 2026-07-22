using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="LoanType"/>.
/// </summary>
public class LoanTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public LoanDeductionType LoanDeductionType { get; set; }

    public RecoveryOption RecoveryOption { get; set; }

    public int EligiblePeriod { get; set; }

    public int EligibleCountPerYear { get; set; }

    public int MaxInstallments { get; set; }

    public EligibleAmount EligibleAmount { get; set; }

    public double EligibleAmountNumber { get; set; }
}
