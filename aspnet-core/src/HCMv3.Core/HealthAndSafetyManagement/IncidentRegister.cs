using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Health and safety incident register (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.HealthAndSafetyManagement.IncidentRegister</c>). Legacy
/// <c>PerformValidation</c> required IncidentDate, IncidentType, Nature and Severity -- ported as-is in
/// <c>IncidentRegistersAppService</c>. Referenced by <see cref="ActionRegister"/>,
/// <see cref="IncidentClaim"/> and <see cref="IncidentInvestigation"/>.
/// </summary>
public class IncidentRegister : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual DateTime IncidentDate { get; set; }

    public virtual DateTime IncidentTime { get; set; }

    public virtual int IncidentTypeId { get; set; }

    public virtual IncidentType IncidentType { get; set; }

    public virtual string Description { get; set; }

    public virtual int NatureId { get; set; }

    public virtual IncidentNature Nature { get; set; }

    public virtual int SeverityId { get; set; }

    public virtual IncidentSeverity Severity { get; set; }

    public virtual int? ReportedById { get; set; }

    public virtual Employee ReportedBy { get; set; }

    public virtual int? PersonInvolvedId { get; set; }

    public virtual Employee PersonInvolved { get; set; }

    public virtual string OtherPeopleInvolved { get; set; }

    public virtual int? LocationId { get; set; }

    public virtual WorkLocation Location { get; set; }

    public virtual int? AssignedToId { get; set; }

    public virtual Employee AssignedTo { get; set; }

    public virtual string DisabilityPercentage { get; set; }

    public virtual int? StatusId { get; set; }

    public virtual IncidentStatus Status { get; set; }

    public virtual string Comments { get; set; }
}
