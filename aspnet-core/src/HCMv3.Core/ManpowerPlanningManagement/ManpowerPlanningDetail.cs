using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.ManpowerPlanningManagement;

/// <summary>
/// Line item of a <see cref="ManpowerPlanning"/> request: one requested job title with a budgeted monthly
/// amount per month (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement.ManpowerPlanningDetail</c>).
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>January</c>..<c>December</c> property setters cascade-fill all
/// subsequent months when an earlier month is set (a data-entry convenience for the XAF UI wizard --
/// e.g. setting <c>January</c> also sets Feb..Dec to the same value, then setting <c>March</c> re-cascades
/// Apr..Dec, etc.). That UI cascade behavior is NOT replicated here (this is a follow-up Angular-side UX
/// concern per the dispatch's UI-follow-up plan) -- all 12 month fields are plain, independently-settable
/// values; <c>ManpowerPlanningDetailsAppService.CreateOrEdit</c> stores exactly what the caller sends.
/// <see cref="TotalAmount"/> (sum of the 12 months) and the parent's <see cref="ManpowerPlanning.GrandTotalAmount"/>
/// (sum of all details' <see cref="TotalAmount"/>) are recomputed server-side on every Create/Update/Delete,
/// same as the legacy <c>UpdateTotalAmount</c>.
/// </summary>
public class ManpowerPlanningDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int ManpowerPlanningId { get; set; }

    public virtual ManpowerPlanning ManpowerPlanning { get; set; }

    public virtual int JobTitleId { get; set; }

    public virtual JobTitle JobTitle { get; set; }

    /// <summary>
    /// Set once the plan is approved -- by the workflow engine, in
    /// <c>ManpowerPlanningWorkflowTransactionHandler.OnApprovedAsync</c>, which is the only code path that
    /// mints these positions since the bespoke Approve method was deleted. Matches the legacy
    /// final-approval <c>OnSaving</c> block that created a new <see cref="Position"/> for every detail line
    /// without one.
    /// </summary>
    public virtual int? PositionId { get; set; }

    public virtual Position Position { get; set; }

    /// <summary>
    /// Current position holder for display purposes (legacy read-only <c>Holder</c> field -- not
    /// programmatically populated anywhere in the audited legacy source).
    /// </summary>
    public virtual int? HolderId { get; set; }

    public virtual Employee Holder { get; set; }

    public virtual double January { get; set; }

    public virtual double February { get; set; }

    public virtual double March { get; set; }

    public virtual double April { get; set; }

    public virtual double May { get; set; }

    public virtual double June { get; set; }

    public virtual double July { get; set; }

    public virtual double August { get; set; }

    public virtual double September { get; set; }

    public virtual double October { get; set; }

    public virtual double November { get; set; }

    public virtual double December { get; set; }

    public virtual double TotalAmount { get; set; }
}
