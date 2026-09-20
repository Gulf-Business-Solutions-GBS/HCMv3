using System;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.PayrollLock;

/// <summary>
/// Lowers the employee's <c>EarliestMasterDataChange</c> when a payroll-sensitive record changes, so the next
/// payroll run recalculates from that date and pays the difference as arrears.
///
/// <para>Ported from legacy <c>MyBaseObject.OnSaving</c>/<c>OnDeleting</c> and <c>MyBaseObject.SetRetroDate</c>.
/// The rule, exactly as legacy has it:</para>
/// <list type="number">
/// <item><description>the record's type is flagged payroll sensitive for the company;</description></item>
/// <item><description>the record belongs to an employee and carries a date -- <c>StartDate</c>, else
/// <c>EffectiveDate</c>, else <c>PaymentDate</c>, else <c>CompensationDate</c>, in that order (legacy's
/// <c>transDate</c>);</description></item>
/// <item><description><c>EarliestMasterDataChange</c> moves DOWN to that date, never up: the oldest pending
/// change is the one the payroll run starts from;</description></item>
/// <item><description>for a time-sensitive type, <c>NextTimeEvaluationFrom</c> moves down the same way;</description></item>
/// <item><description>every move writes a <c>RetroReasonLog</c> row saying which record and which date.</description></item>
/// </list>
///
/// <para>Refused outright when the date is before the employee's <c>EarliestRetroAccountingDate</c>, or when
/// retro calculation is switched off for the period (<c>PayrollDataService.ValidateRetroCalculation</c>).</para>
/// </summary>
public interface IPayrollRetroTrigger : IDomainService
{
    /// <summary>
    /// Records the change of <paramref name="entity"/>. <paramref name="deleted"/> only changes the wording of the
    /// logged reason, as in legacy.
    /// </summary>
    void RecordChange(object entity, bool deleted);

    /// <summary>
    /// Legacy <c>IsEndDateAutoChange</c>: this row is only being re-closed because a neighbour was added, moved or
    /// removed, so it must not trigger a recalculation of its own. Marked per row, and only for this save.
    /// </summary>
    void MarkEndDateAutoChange(object entity);

    /// <summary>
    /// Legacy <c>StopRetroEffect</c>: the record changed, but nothing that affects pay did, so no recalculation.
    /// Applies to a save, not to a delete -- legacy ignores it when deleting.
    /// </summary>
    void MarkStopRetroEffect(object entity);

    /// <summary>
    /// Legacy <c>MyBaseObject.SetRetroDate</c>: lowers the date from code, for a type that has no record of its own
    /// to hang the change on. Creates the employee's payroll status row when there is none.
    /// </summary>
    void SetRetroDate(Type entityType, int employeeId, DateTime retroFromDate, int entityObjectId = 0);
}
