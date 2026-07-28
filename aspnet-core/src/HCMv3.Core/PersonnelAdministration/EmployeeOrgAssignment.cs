using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;

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
/// NO LONGER DEFERRED: <see cref="EmployeeGroupId"/>, <see cref="EmployeeSubGroupId"/> and
/// <see cref="PayrollAreaId"/> were deferred in Wave B because PayrollManagement was not yet ported.
/// It is now, and their absence had become a live defect rather than a gap: the Time Engine screen
/// failed for every employee with "Employee Sub Group not found for Employee", the sick-leave-unpaid
/// (2003) slab correction never applied, and wage codes 23/27/29/35 threw at runtime. Several services
/// had grown caller-supplied-parameter workarounds for exactly this (<c>IAccrualService</c>,
/// <c>IIndemnityCalculationService</c>, <c>TimeEngineRunInput.EmployeeSubGroupIds</c>) and
/// <c>IEmployeePayrollAreaResolver</c> existed solely to reach PayrollArea via
/// <c>OrgUnitProfile</c>. All three are nullable so existing rows migrate cleanly; resolve them
/// date-effectively as of the assignment, exactly as legacy did.
///
/// STILL DEFERRED (see Wave B scope notes): <c>Position</c>/<c>WorkflowGroup</c> fields;
/// <c>AddBasicSalary</c>/<c>AddLocationInWageAccount</c> side effects; Position-vacancy-tracking
/// (<c>UpdatedPositionRelation</c> family); the legacy "Check OrganizationUnitType" validation
/// (not applicable -- collapsed to one FK); the "system generated flag to stop
/// EarliestMasterDataChange" block.
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

    /// <summary>Legacy <c>EmployeeOrgAssignment.EmployeeGroup</c>. Nullable: existing rows predate this column.</summary>
    public virtual int? EmployeeGroupId { get; set; }

    public virtual EmployeeGroup EmployeeGroup { get; set; }

    /// <summary>
    /// Legacy <c>EmployeeOrgAssignment.EmployeeSubGroup</c>. Drives leave/ticket/gratuity slab selection,
    /// so a null here degrades those calculations rather than failing loudly -- resolve it date-effectively.
    /// </summary>
    public virtual int? EmployeeSubGroupId { get; set; }

    public virtual EmployeeSubGroup EmployeeSubGroup { get; set; }

    /// <summary>
    /// Legacy <c>EmployeeOrgAssignment.PayrollArea</c>. <c>IEmployeePayrollAreaResolver</c> reaches this
    /// via <c>OrgUnitProfile</c> as a workaround; prefer this FK when set and fall back to that chain.
    /// </summary>
    public virtual int? PayrollAreaId { get; set; }

    public virtual PayrollArea PayrollArea { get; set; }
}
