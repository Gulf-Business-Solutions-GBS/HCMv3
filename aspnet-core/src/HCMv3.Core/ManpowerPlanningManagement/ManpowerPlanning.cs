using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Manpower planning request header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.ManpowerPlanning</c>).
///
/// DELIBERATE SIMPLIFICATION: the legacy class inherited from <c>Workflow.WorkflowRequest</c> (multi-step
/// approval engine: <c>RequestNumber</c> global auto-numbering shared across all workflow request types,
/// <c>WorkflowStep</c>, <c>Approver</c>, <c>DocAttachment</c>, <c>WorkFlowApprover</c>/<c>RequestLog</c>/
/// <c>WorkflowRemark</c>/<c>WFReadLog</c> aggregated collections, employee-visibility filtering
/// (<c>AllowedEmployeeList</c>/<c>DelegatedEmployee</c>), <c>CurrentWorkFlowRole</c>/<c>Approvers</c> UI
/// helpers). The Workflow module is not ported anywhere in this codebase yet, so none of that is
/// available -- this entity is ported standalone with just the fields the module's own business logic
/// (validation + approval-time Position creation, see <c>ManpowerPlanningsAppService</c>) actually uses,
/// and <see cref="ManpowerPlanningStatus"/> stands in for the legacy generic <c>RequestStatus</c>.
///
/// <c>OrganizationUnitId</c> FKs Zero's own built-in <c>Abp.Organizations.OrganizationUnit</c> tree (the
/// legacy "Department" field), matching the same simplification already used by
/// <c>HCMv3.PersonnelAdministration.EmployeeOrgAssignment</c>.
/// </summary>
public class ManpowerPlanning : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    [Required]
    public virtual long OrganizationUnitId { get; set; }

    public virtual Abp.Organizations.OrganizationUnit OrganizationUnit { get; set; }

    [Required]
    public virtual int PeriodId { get; set; }

    public virtual ManpowerPlanningPeriod Period { get; set; }

    public virtual double GrandTotalAmount { get; set; }

    [StringLength(1000)]
    public virtual string RequestComments { get; set; }

    public virtual ManpowerPlanningStatus Status { get; set; }

    public virtual ICollection<ManpowerPlanningDetail> ManpowerPlanningDetails { get; set; }
}
