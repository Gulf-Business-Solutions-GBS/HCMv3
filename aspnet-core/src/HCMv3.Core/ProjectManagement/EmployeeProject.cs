using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Header row assigning an Employee to one or more Projects for a period (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ProjectManagement.EmployeeProject</c>). Legacy <c>PerformValidation</c>
/// parity (StartDate/EndDate must be calendar-month boundaries; no overlapping date range with other
/// EmployeeProject rows for the same Employee; must have at least one <see cref="EmployeeProjectDetail"/>
/// child; if <see cref="CostingMethod"/> == Percentage, child percentages must not exceed 100 total) is
/// enforced in <c>EmployeeProjectsAppService</c>, not here. Its aggregated child is
/// <see cref="EmployeeProjectDetail"/>, linked back via <see cref="EmployeeProjectDetail.EmployeeProjectId"/>.
/// </summary>
public class EmployeeProject : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual ProjectCostingMethod CostingMethod { get; set; }
}
