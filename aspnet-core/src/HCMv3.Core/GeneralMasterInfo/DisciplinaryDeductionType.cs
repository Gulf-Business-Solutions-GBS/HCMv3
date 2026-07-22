using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// DisciplinaryDeductionType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.DisciplinaryDeductionType</c>).
/// Extends the Code/NameEn/NameAr shape with a <see cref="GeneralMasterInfo.DisciplinaryDeductionUnit"/>
/// enum and a deduction value, so it can't derive from <see cref="SimpleLookupEntity"/>
/// (see <see cref="AccessUnit"/> for the same bespoke pattern).
/// </summary>
public class DisciplinaryDeductionType : FullAuditedEntity, IMustHaveTenant
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

    public virtual DisciplinaryDeductionUnit DisciplinaryDeductionUnit { get; set; }

    /// <summary>
    /// Legacy parity: must be between 0 and 100 (exclusive of 0) when <see cref="DisciplinaryDeductionUnit"/>
    /// is <see cref="GeneralMasterInfo.DisciplinaryDeductionUnit.Percentage"/>.
    /// </summary>
    public virtual double DeductionValue { get; set; }
}
