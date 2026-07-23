using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Date-bound Employee organizational assignment (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeOrgAssignment</c>, Wave B).
///
/// The legacy entity's 7 discrete cascading-dropdown <c>OrgLevel1</c>..<c>OrgLevel7</c> fields are
/// collapsed into a single <see cref="OrganizationUnitId"/> FK to Zero's own built-in, already
/// hierarchical <see cref="OrganizationUnit"/> tree (parent chain via <c>OrganizationUnit.ParentId</c>)
/// -- one FK replaces all 7 legacy levels.
///
/// DEFERRED (not ported, see Wave B scope notes): <c>Position</c>/<c>EmployeeGroup</c>/
/// <c>EmployeeSubGroup</c>/<c>PayrollArea</c>/<c>WorkflowGroup</c> fields (ManpowerPlanning/
/// PayrollManagement/Workflow modules not ported); <c>AddBasicSalary</c>/<c>AddLocationInWageAccount</c>
/// side effects; Position-vacancy-tracking (<c>UpdatedPositionRelation</c> family); the legacy
/// "Check OrganizationUnitType" validation (not applicable -- collapsed to one FK); the "system
/// generated flag to stop EarliestMasterDataChange" block (references unported Payroll fields).
///
/// The EndDate auto-chaining (legacy <c>SetEndDate</c>), no-duplicate-StartDate validation, and
/// <c>PrefillFromPrevious</c> are ported in <c>EmployeeOrgAssignmentsAppService</c>.
/// </summary>
public class EmployeeOrgAssignment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int? JobTitleId { get; set; }

    public virtual JobTitle JobTitle { get; set; }

    public virtual int? CostCenterId { get; set; }

    public virtual CostCenter CostCenter { get; set; }

    public virtual int? WorkLocationId { get; set; }

    public virtual WorkLocation WorkLocation { get; set; }

    public virtual int? PostingGroupId { get; set; }

    public virtual PostingGroup PostingGroup { get; set; }

    public virtual int? BudgetingGroupId { get; set; }

    public virtual BudgetingGroup BudgetingGroup { get; set; }

    /// <summary>
    /// FK to Zero's own <c>Abp.Organizations.OrganizationUnit</c> tree -- replaces the legacy's 7
    /// discrete OrgLevel1..7 fields (see class remarks).
    /// </summary>
    public virtual long? OrganizationUnitId { get; set; }

    public virtual OrganizationUnit OrganizationUnit { get; set; }

    /// <summary>
    /// Optional link to the <see cref="EmployeeAction"/> that produced this record, so
    /// <c>EmployeeActionsAppService</c> can cascade-delete it when the action is deleted.
    /// </summary>
    public virtual int? EmployeeActionId { get; set; }

    public virtual EmployeeAction EmployeeAction { get; set; }
}
