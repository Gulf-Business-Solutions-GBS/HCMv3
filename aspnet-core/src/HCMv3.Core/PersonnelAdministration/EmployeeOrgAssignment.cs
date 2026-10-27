using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.Workflow;

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
/// ALSO NO LONGER DEFERRED: <see cref="WorkflowGroupId"/>, for the same reason -- it stopped being a gap
/// and became a blocker the moment approval routing was implemented. See that property's own remarks.
///
/// NO LONGER DEFERRED: <see cref="PositionId"/> and position occupancy (legacy <c>UpdatedPositionRelation</c>
/// family), kept in step by <c>IPositionOccupancyService</c> when <c>ManageOrgSettings.EnablePosition</c> is on.
///
/// STILL DEFERRED (see Wave B scope notes):
/// <c>AddBasicSalary</c>/<c>AddLocationInWageAccount</c> side effects; the legacy "Check OrganizationUnitType" validation
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

    /// <summary>
    /// Legacy <c>EmployeeOrgAssignment.WorkflowGroup</c>, previously listed as deferred in this class's
    /// remarks. No longer deferrable: it is the first step of approval routing. Legacy
    /// <c>WorkflowGeneralService.GetRequestMaster</c> reads the employee's current-dated org assignment to
    /// find their workflow group, and only then looks up which approval chain applies -- so without this
    /// column, group-based routing cannot work at all and every employee falls through to the same default
    /// chain.
    ///
    /// <para>
    /// Nullable, and null is normal: an employee with no group resolves to the
    /// <c>WorkflowGroup.IsDefault</c> row, exactly as legacy did.
    /// </para>
    /// </summary>
    public virtual int? WorkflowGroupId { get; set; }

    public virtual WorkflowGroup WorkflowGroup { get; set; }

    /// <summary>
    /// Legacy <c>EmployeeOrgAssignment.Position</c>. Required when <c>ManageOrgSettings.EnablePosition</c> is on;
    /// each assignment with a position keeps one holder row in <see cref="HCMv3.ManpowerPlanningManagement.PositionRelationship"/>.
    /// </summary>
    public virtual int? PositionId { get; set; }

    public virtual HCMv3.ManpowerPlanningManagement.Position Position { get; set; }
}
