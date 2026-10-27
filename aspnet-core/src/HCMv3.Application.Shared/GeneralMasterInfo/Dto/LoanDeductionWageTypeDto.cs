using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// A wage type the Loan Types screen offers as the loan recovery deduction (legacy <c>LoanType.LoanDedWageType</c>).
/// </summary>
public class LoanDeductionWageTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }
}
