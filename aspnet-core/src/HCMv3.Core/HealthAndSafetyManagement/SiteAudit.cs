using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Site safety audit header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.HealthAndSafetyManagement.SiteAudit</c>). Legacy
/// <c>PerformValidation</c> required AuditDate, Location, ReassessmentDate and ConductedBy -- ported
/// as-is in <c>SiteAuditsAppService</c>. Its <c>SiteAuditDetail</c> aggregated child collection is
/// exposed separately via <see cref="SiteAuditDetail"/>/<c>SiteAuditDetailsAppService</c> (same pattern as
/// <c>ProjectManagement.ProjectRegister</c>/<c>ProjectRegisterDetail</c>).
/// </summary>
public class SiteAudit : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual DateTime AuditDate { get; set; }

    public virtual DateTime AuditTime { get; set; }

    public virtual int LocationId { get; set; }

    public virtual WorkLocation Location { get; set; }

    public virtual int ConductedById { get; set; }

    public virtual Employee ConductedBy { get; set; }

    public virtual string Comments { get; set; }

    public virtual DateTime ReassessmentDate { get; set; }
}
