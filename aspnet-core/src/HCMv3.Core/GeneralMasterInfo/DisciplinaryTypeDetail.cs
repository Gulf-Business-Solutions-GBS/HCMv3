using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// DisciplinaryTypeDetail (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.DisciplinaryTypeDetail</c>).
/// Pure detail record: no Code/NameEn/NameAr. Owned by a <see cref="DisciplinaryType"/>, with a
/// <see cref="DisciplinaryDeductionType"/> FK (reused from Wave B).
/// </summary>
public class DisciplinaryTypeDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? DisciplinaryTypeId { get; set; }

    public virtual DisciplinaryType DisciplinaryType { get; set; }

    public virtual int DisciplinaryCount { get; set; }

    public virtual int? DisciplinaryDeductionTypeId { get; set; }

    public virtual DisciplinaryDeductionType DisciplinaryDeductionType { get; set; }
}
