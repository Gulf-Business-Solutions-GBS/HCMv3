using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// Effective-dated competency profile assigned to a <see cref="JobTitle"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.CompetencyDevelopment.CompetencyByJobTitle</c>). Its aggregated
/// <c>CompetencyByJobTitleDetail</c> child collection is ported as a flat satellite entity
/// (<see cref="CompetencyByJobTitleDetail"/>) carrying its own <c>CompetencyByJobTitleId</c> FK, same flat
/// parent+child pattern used throughout this port.
/// </summary>
public class CompetencyByJobTitle : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int JobTitleId { get; set; }

    public virtual JobTitle JobTitle { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
