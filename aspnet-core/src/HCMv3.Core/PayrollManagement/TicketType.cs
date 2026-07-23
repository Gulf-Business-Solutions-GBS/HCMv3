using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// TicketType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.TicketType</c>).
///
/// <see cref="TicketBasisPeriod"/> reuses the already-ported <see cref="LeaveBasisPeriod"/> enum: the
/// legacy declares a distinct <c>TicketBasisPeriod</c> enum (in <c>AccrualPeriod.cs</c>) with the exact
/// same CalendarDays=0/FixedDays=1 shape as <c>LeaveBasisPeriod</c> -- semantically identical, so no new
/// enum is introduced (see <c>TicketTypesAppService</c> for a Wave 4 report note).
///
/// The legacy's dynamic <c>TicketTypeWageType</c> DataSourceProperty filter (excludes leave/overtime wage
/// types, technical Code "9000", etc.) is a pure XAF UI/combo-box filtering concern and is not ported --
/// <see cref="WageTypeId"/> here is a plain FK with no candidate-list restriction.
/// </summary>
public class TicketType : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual LeaveBasisPeriod TicketBasisPeriod { get; set; }

    public virtual double TicketBasisFixedDays { get; set; }

    public virtual bool Lapse { get; set; }

    public virtual int LapseAfter { get; set; }

    public virtual int StopDependantAccrualAfter { get; set; }

    public virtual double MinCompensation { get; set; }

    public virtual DeductionPeriod DeductionPeriod { get; set; }

    public virtual QuotaEntitlement QuotaEntitlement { get; set; }

    public virtual int? WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }
}
