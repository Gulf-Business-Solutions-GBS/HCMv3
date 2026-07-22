using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Currency master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.Currency</c>).
/// Extends the Code/NameEn/NameAr shape with decimal-count and short/denomination form fields, so it
/// can't derive from <see cref="SimpleLookupEntity"/> (see <see cref="AccessUnit"/> for the same bespoke pattern).
/// </summary>
public class Currency : FullAuditedEntity, IMustHaveTenant
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

    public virtual int DecimalCount { get; set; }

    [StringLength(CurrencyConsts.MaxShortFormLength)]
    public virtual string CurrencyShortFormEn { get; set; }

    [StringLength(CurrencyConsts.MaxDenominationFormLength)]
    public virtual string CurrencyDenominationFormEn { get; set; }

    [StringLength(CurrencyConsts.MaxShortFormLength)]
    public virtual string CurrencyShortFormAr { get; set; }

    [StringLength(CurrencyConsts.MaxDenominationFormLength)]
    public virtual string CurrencyDenominationFormAr { get; set; }
}
