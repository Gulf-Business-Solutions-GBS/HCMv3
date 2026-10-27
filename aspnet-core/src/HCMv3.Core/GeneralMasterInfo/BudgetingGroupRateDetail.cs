using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// BudgetingGroupRateDetail (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.BudgetingGroupRateDetail</c>).
/// Pure detail record: no Code/NameEn/NameAr. Owned by a <see cref="BudgetingGroup"/>.
/// </summary>
public class BudgetingGroupRateDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int? BudgetingGroupId { get; set; }

    public virtual BudgetingGroup BudgetingGroup { get; set; }

    public virtual System.DateTime StartDate { get; set; }

    public virtual System.DateTime EndDate { get; set; }

    public virtual double BonusRate { get; set; }
}
