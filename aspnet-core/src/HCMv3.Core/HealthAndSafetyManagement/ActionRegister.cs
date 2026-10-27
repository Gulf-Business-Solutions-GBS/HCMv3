using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Corrective/preventive action tracker, linkable to either an <see cref="IncidentRegister"/> or a
/// <see cref="RiskRegister"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.HealthAndSafetyManagement.ActionRegister</c>). Legacy
/// <c>PerformValidation</c> required Incident, Risk, AssignedTo and ActionDate; ported as-is in
/// <c>ActionRegistersAppService</c>.
/// </summary>
public class ActionRegister : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual DateTime ActionDate { get; set; }

    public virtual string Description { get; set; }

    public virtual int IncidentId { get; set; }

    public virtual IncidentRegister Incident { get; set; }

    public virtual int RiskId { get; set; }

    public virtual RiskRegister Risk { get; set; }

    public virtual int AssignedToId { get; set; }

    public virtual Employee AssignedTo { get; set; }

    public virtual int? StatusId { get; set; }

    public virtual ActionStatus Status { get; set; }
}
