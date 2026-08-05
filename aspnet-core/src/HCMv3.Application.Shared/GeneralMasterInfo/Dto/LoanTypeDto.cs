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

    /// <summary>
    /// Legacy <c>LoanType.WageType</c> FK (restored wave P9). The Payroll Engine dispatches wage code 2001
    /// "Loan Recovery Deduction" through it, so it must be settable from this master.
    /// </summary>
    public int? WageTypeId { get; set; }

    public string WageTypeCode { get; set; }

    public string WageTypeNameEn { get; set; }
}
