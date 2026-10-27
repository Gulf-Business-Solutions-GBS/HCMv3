using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Cost-distribution line of a <see cref="CostRegister"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.CostRegisterDetail</c>), linked back via
/// <see cref="CostRegisterId"/>. No business validation in the legacy source (plain CRUD).
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>AfterConstruction</c> auto-defaults <c>Currency</c> via
/// <c>CurrencyService.GetDefaultCurrency</c>. This codebase's <see cref="Currency"/> entity has no
/// "default currency" flag/concept anywhere (checked <c>CurrencyConvertorsAppService</c> and every other
/// Currency usage in the solution — no established default-currency lookup pattern exists yet), so
/// <see cref="CurrencyId"/> is ported as a plain nullable FK with NO auto-default; callers must supply it
/// explicitly if desired.
/// </summary>
public class CostRegisterDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int CostRegisterId { get; set; }

    public virtual CostRegister CostRegister { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }

    public virtual int AccountInfoId { get; set; }

    public virtual AccountInfo AccountInfo { get; set; }

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

    public virtual double Amount { get; set; }

    public virtual int? CurrencyId { get; set; }

    public virtual Currency Currency { get; set; }
}
