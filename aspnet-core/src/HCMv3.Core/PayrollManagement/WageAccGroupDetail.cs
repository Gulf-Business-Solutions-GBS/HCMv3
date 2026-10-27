using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.PayrollManagement;

/// <summary>
/// FI account mapping row for a <see cref="PayrollManagement.WageAccGroup"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.WageAccGroupDetail</c>), listed separately and linked
/// back via <see cref="WageAccGroupId"/>.
///
/// DELIBERATE SIMPLIFICATION: the legacy source cross-checks each populated
/// <c>CostElement1</c>..<c>CostElement7</c> slot's <c>CostElementType.Code</c> against a fixed
/// <c>CostService.CostElementN</c> constant. No equivalent slot-type-validation pattern exists anywhere else
/// in this port (<c>CostElement</c>/<c>CostElementType</c> carry no such constants or established check), so
/// this cross-check is NOT ported — only basic required-field and duplicate-combination validation is
/// enforced in <c>WageAccGroupDetailsAppService</c>.
/// </summary>
public class WageAccGroupDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WageAccGroupId { get; set; }

    public virtual WageAccGroup WageAccGroup { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual int AccountInfoId { get; set; }

    public virtual AccountInfo AccountInfo { get; set; }

    public virtual OffsetAccountInfoType OffsetAccountInfoType { get; set; }

    public virtual int? OffsetAccountInfoId { get; set; }

    public virtual AccountInfo OffsetAccountInfo { get; set; }

    public virtual int? CostElement1Id { get; set; }

    public virtual CostElement CostElement1 { get; set; }

    public virtual int? CostElement2Id { get; set; }

    public virtual CostElement CostElement2 { get; set; }

    public virtual int? CostElement3Id { get; set; }

    public virtual CostElement CostElement3 { get; set; }

    public virtual int? CostElement4Id { get; set; }

    public virtual CostElement CostElement4 { get; set; }

    public virtual int? CostElement5Id { get; set; }

    public virtual CostElement CostElement5 { get; set; }

    public virtual int? CostElement6Id { get; set; }

    public virtual CostElement CostElement6 { get; set; }

    public virtual int? CostElement7Id { get; set; }

    public virtual CostElement CostElement7 { get; set; }

    public virtual double CostPercentage { get; set; }

    public virtual bool MergeOffsetAccount { get; set; }
}
