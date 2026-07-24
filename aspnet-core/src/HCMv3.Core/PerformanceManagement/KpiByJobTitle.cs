using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Effective-dated KPI weighting header for a <see cref="JobTitle"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.KpiByJobTitle</c>). Aggregated child is
/// <see cref="KpiByJobTitleDetail"/>, flat parent+child pattern (own <c>KpiByJobTitleId</c> FK). The legacy
/// computed <c>TotalWeight</c> getter is DTO-only (see <c>KpiByJobTitleDto</c>), not a stored column. The
/// legacy EndDate auto-chaining (<c>SetEndDate</c>) is ported in <c>KpiByJobTitlesAppService</c>, same
/// pattern as <c>PersonnelAdministration.EmployeeOrgAssignmentsAppService</c>.
/// </summary>
public class KpiByJobTitle : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int JobTitleId { get; set; }

    public virtual JobTitle JobTitle { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
