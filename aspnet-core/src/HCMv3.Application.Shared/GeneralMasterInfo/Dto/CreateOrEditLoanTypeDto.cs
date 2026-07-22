using System.ComponentModel.DataAnnotations;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="LoanType"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditLoanTypeDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code (legacy <c>GetMaxCode</c> parity).
    /// </summary>
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    public LoanDeductionType LoanDeductionType { get; set; }

    public RecoveryOption RecoveryOption { get; set; }

    public int EligiblePeriod { get; set; }

    public int EligibleCountPerYear { get; set; }

    public int MaxInstallments { get; set; }

    public EligibleAmount EligibleAmount { get; set; }

    public double EligibleAmountNumber { get; set; }
}
