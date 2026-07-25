using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Insurance/compensation claim raised against an <see cref="IncidentRegister"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.HealthAndSafetyManagement.IncidentClaim</c>). Legacy
/// <c>PerformValidation</c> required ClaimDate, Incident, PaymentDate and Currency -- ported as-is in
/// <c>IncidentClaimsAppService</c>. Legacy property is named <c>Amount</c> with a <c>[DisplayName("Number")]</c>
/// UI override; kept as <c>Amount</c> here since the display name is an XAF UI concept.
/// </summary>
public class IncidentClaim : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual DateTime ClaimDate { get; set; }

    public virtual int IncidentId { get; set; }

    public virtual IncidentRegister Incident { get; set; }

    public virtual double Amount { get; set; }

    public virtual int CurrencyId { get; set; }

    public virtual Currency Currency { get; set; }

    public virtual DateTime PaymentDate { get; set; }

    public virtual int? ClaimStatusId { get; set; }

    public virtual IncidentClaimStatus ClaimStatus { get; set; }

    public virtual string Comments { get; set; }
}
