using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Investigation record for an <see cref="IncidentRegister"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.HealthAndSafetyManagement.IncidentInvestigation</c>). Legacy
/// <c>PerformValidation</c> required InvestigationDate, Incident and RootCause -- ported as-is in
/// <c>IncidentInvestigationsAppService</c>.
/// </summary>
public class IncidentInvestigation : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual DateTime InvestigationDate { get; set; }

    public virtual DateTime InvestigationTime { get; set; }

    public virtual int IncidentId { get; set; }

    public virtual IncidentRegister Incident { get; set; }

    public virtual int RootCauseId { get; set; }

    public virtual InvestigationRootCause RootCause { get; set; }

    public virtual string RootCauseAnalysis { get; set; }

    public virtual string Comments { get; set; }
}
