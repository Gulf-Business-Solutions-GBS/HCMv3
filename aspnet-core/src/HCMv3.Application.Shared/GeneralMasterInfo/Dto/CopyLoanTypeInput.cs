using System.ComponentModel.DataAnnotations;
using HCMv3.PayrollManagement;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Input for Copy Loan Type (legacy <c>LoanTypeCopy</c>): which loan type to copy, the copy's names, and what to
/// do with its deduction wage type.
/// </summary>
public class CopyLoanTypeInput
{
    /// <summary>Room left in a wage type name for the " Arrear" suffix the arrear copy gets.</summary>
    public const int MaxWageTypeNameLength = SimpleLookupConsts.MaxNameLength - 7;

    [Range(1, int.MaxValue)]
    public int LoanTypeId { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    /// <summary>Optional; defaults to <see cref="NameEn"/> (legacy parity).</summary>
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    public WageTypeCopyOption WageTypeOption { get; set; }

    /// <summary>Name of the new wage type. Required when <see cref="WageTypeOption"/> is CopyAndCreate.</summary>
    [StringLength(MaxWageTypeNameLength)]
    public string WageTypeName { get; set; }
}
