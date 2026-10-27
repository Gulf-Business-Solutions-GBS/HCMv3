using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Manpower planning period master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.ManpowerPlanningPeriod</c>).
/// A single <c>Name</c> field (no NameEn/NameAr split), matching the legacy source exactly.
/// </summary>
public class ManpowerPlanningPeriod : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    [StringLength(200)]
    public virtual string Name { get; set; }

    public virtual bool Active { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
