using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TicketType"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditTicketTypeDto
{
    public int? Id { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameEn { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string NameAr { get; set; }

    public LeaveBasisPeriod TicketBasisPeriod { get; set; }

    public double TicketBasisFixedDays { get; set; }

    public bool Lapse { get; set; }

    public int LapseAfter { get; set; }

    public int StopDependantAccrualAfter { get; set; }

    public double MinCompensation { get; set; }

    public DeductionPeriod DeductionPeriod { get; set; }

    public QuotaEntitlement QuotaEntitlement { get; set; }

    public int? WageTypeId { get; set; }
}
