using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Health and safety risk register (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.HealthAndSafetyManagement.RiskRegister</c>). Legacy
/// <c>PerformValidation</c> required RiskType, Impact, Possibility, ReportedBy and Location; ReportingDate
/// was also validated non-default -- ported as-is in <c>RiskRegistersAppService</c>. Referenced by
/// <see cref="ActionRegister"/>.
/// </summary>
public class RiskRegister : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual DateTime ReportingDate { get; set; }

    public virtual DateTime ReportingTime { get; set; }

    public virtual int RiskTypeId { get; set; }

    public virtual RiskType RiskType { get; set; }

    public virtual string Description { get; set; }

    public virtual int ImpactId { get; set; }

    public virtual RiskImpact Impact { get; set; }

    public virtual int PossibilityId { get; set; }

    public virtual RiskPossibility Possibility { get; set; }

    public virtual int ReportedById { get; set; }

    public virtual Employee ReportedBy { get; set; }

    public virtual int LocationId { get; set; }

    public virtual WorkLocation Location { get; set; }

    public virtual int? AssignedToId { get; set; }

    public virtual Employee AssignedTo { get; set; }

    public virtual string RiskMitigation { get; set; }

    public virtual int? ActionId { get; set; }

    public virtual RiskAction Action { get; set; }

    public virtual int? StatusId { get; set; }

    public virtual RiskStatus Status { get; set; }

    public virtual DateTime ReassessmentDate { get; set; }
}
