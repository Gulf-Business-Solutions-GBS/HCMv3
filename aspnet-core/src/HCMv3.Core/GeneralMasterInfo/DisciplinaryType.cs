using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// DisciplinaryType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.DisciplinaryType</c>).
/// Extends the Code/NameEn/NameAr shape with <see cref="RepeatDurationInDays"/> and <see cref="WageTypeId"/>,
/// so it can't derive from <see cref="SimpleLookupEntity"/> (see <see cref="City"/> for the same bespoke pattern).
/// The legacy <c>ViolationWageType</c> picker filter is <c>DisciplinaryTypesAppService.GetViolationWageTypes</c>.
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

    /// <summary>
    /// Legacy <c>ViolationType.WageType</c>: the deduction wage type the violation is charged through. Legacy
    /// required it; nullable here because rows created before it was ported have none.
    /// </summary>
    public virtual int? WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }
}
