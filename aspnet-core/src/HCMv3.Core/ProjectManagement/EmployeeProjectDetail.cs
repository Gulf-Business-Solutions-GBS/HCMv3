using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Aggregated child of <see cref="EmployeeProject"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ProjectManagement.EmployeeProjectDetail</c>), linked back via
/// <see cref="EmployeeProjectId"/>. Legacy <c>PerformValidation</c> parity (date range within parent's
/// range; no overlap with sibling ProjectDetail rows for the same Project; percentage rules mirroring
/// parent's CostingMethod) is enforced in <c>EmployeeProjectDetailsAppService</c>, not here.
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>CalendarDays</c>/<c>WorkDays</c> properties were non-persistent
/// (<c>[NonPersistent]</c>) computed getters, not stored fields, so they are intentionally NOT ported as
/// entity properties here. <c>CalendarDays</c> is a trivial date-math one-liner callers can compute
/// client-side; <c>WorkDays</c> is exposed instead as an on-demand app-service method
/// (<c>EmployeeProjectDetailsAppService.CalculateWorkDaysAsync</c>) that queries <c>TimeManagement</c>'s
/// <c>TimeRegister</c> entity -- see that method's XML doc for the simplification made there.
/// </summary>
public class EmployeeProjectDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeProjectId { get; set; }

    public virtual EmployeeProject EmployeeProject { get; set; }

    public virtual int ProjectId { get; set; }

    public virtual Project Project { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual double Percentage { get; set; }
}
