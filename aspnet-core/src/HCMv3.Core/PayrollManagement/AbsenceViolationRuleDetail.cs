using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Escalation step of an <see cref="AbsenceViolationRule"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AbsenceViolationRuleDetail</c>), listed separately and
/// linked back via <see cref="AbsenceViolationRuleId"/>.
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>ActionType</c> is a polymorphic picklist that resolves to either
/// an <c>AbsenceType</c> row (when <see cref="Action"/> is <see cref="PayrollManagement.AbsenceViolationAction.Leave"/>)
/// or a <c>GeneralMasterInfo.DisciplinaryType</c> row (when <see cref="Action"/> is
/// <see cref="PayrollManagement.AbsenceViolationAction.Violation"/>), via a value-converter + filtered
/// DataSourceProperty. Both target entities exist in this codebase, but a single scalar FK cannot cleanly
/// target two different tables in EF Core, so <see cref="ActionTypeId"/> is ported as a plain nullable
/// <c>int</c> column with NO foreign key / navigation property — callers must resolve it against
/// <c>AbsenceType</c> or <c>DisciplinaryType</c> themselves based on <see cref="Action"/>. The legacy
/// <c>HoldTillPunchFound</c> bool and <c>NotificationConfig</c> FK were left out of the first pass (no
/// <c>NotificationConfig</c> existed then) and were restored on 2026-09-23 together with the job that reads
/// them (<c>AbsenceViolationService</c>).
/// </summary>
public class AbsenceViolationRuleDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int AbsenceViolationRuleId { get; set; }

    public virtual AbsenceViolationRule AbsenceViolationRule { get; set; }

    public virtual int Step { get; set; }

    public virtual int DayInterval { get; set; }

    public virtual AbsenceViolationAction Action { get; set; }

    /// <summary>
    /// Deferred polymorphic reference: resolves to <c>AbsenceType.Id</c> when <see cref="Action"/> is
    /// <see cref="AbsenceViolationAction.Leave"/>, or to <c>HCMv3.GeneralMasterInfo.DisciplinaryType.Id</c>
    /// when <see cref="Action"/> is <see cref="AbsenceViolationAction.Violation"/>. See class remarks.
    /// </summary>
    public virtual int? ActionTypeId { get; set; }

    /// <summary>
    /// Legacy <c>HoldTillPunchFound</c>: the step waits until the employee punches again. For a missing-punch
    /// day the step fires only on a following working day that has a punch; for a leave not returned from, only
    /// when there is a punch today.
    /// </summary>
    public virtual bool HoldTillPunchFound { get; set; }

    /// <summary>
    /// Legacy <c>NotificationConfig</c>: who is told, and how, when this step fires. Optional -- a step with no
    /// configuration acts silently.
    /// </summary>
    public virtual int? NotificationConfigId { get; set; }

    /// <inheritdoc cref="NotificationConfigId"/>
    public virtual NotificationConfig NotificationConfig { get; set; }
}
