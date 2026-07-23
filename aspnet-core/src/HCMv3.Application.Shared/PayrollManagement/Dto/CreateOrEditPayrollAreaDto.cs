using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="PayrollArea"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditPayrollAreaDto
{
    public int? Id { get; set; }

    /// <summary>
    /// Optional. When left blank the server auto-generates the next numeric code.
    /// </summary>
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string Name { get; set; }

    [Required]
    public int PayrollPeriodId { get; set; }

    [Required]
    public int CurrencyId { get; set; }

    public int CutOffDay { get; set; }

    public int? FICompanyId { get; set; }

    public int? FIBankId { get; set; }

    public int? CountryId { get; set; }
}
