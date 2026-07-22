using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// DisciplinaryType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.DisciplinaryType</c>).
/// Extends the Code/NameEn/NameAr shape with <see cref="RepeatDurationInDays"/>, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="City"/> for the same bespoke pattern).
/// The legacy <c>WageType</c> FK and <c>ViolationWageType</c> collection are not ported (Payroll module out of scope).
/// </summary>
public class DisciplinaryType : FullAuditedEntity, IMustHaveTenant
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

    public virtual int RepeatDurationInDays { get; set; }
}
