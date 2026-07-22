using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// CurrencyConvertor (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.CurrencyConvertor</c>).
/// A rate record (not a Code/NameEn/NameAr lookup): a calendar date plus a From/To <see cref="Currency"/>
/// pair and the conversion rate. Both FKs reference the same <see cref="Currency"/> entity, so distinct FK
/// id properties (<see cref="FromCurrId"/>/<see cref="ToCurrId"/>) with two separate navigation properties
/// are used, following the same established dual-FK-to-same-table pattern as
/// <see cref="PayeeBankDetail"/> (Bank/PayeeBank both -> <see cref="Bank"/>).
/// </summary>
public class CurrencyConvertor : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual DateTime CalendarDate { get; set; }

    public virtual int? FromCurrId { get; set; }

    public virtual Currency FromCurr { get; set; }

    public virtual int? ToCurrId { get; set; }

    public virtual Currency ToCurr { get; set; }

    public virtual double Rate { get; set; }
}
