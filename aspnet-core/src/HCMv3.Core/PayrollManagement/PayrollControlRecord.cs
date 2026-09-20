using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// PayrollControlRecord (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.PayrollControlRecord</c>). Exactly one control record
/// is allowed per <see cref="PayrollArea"/> (enforced in <c>PayrollControlRecordsAppService</c>, legacy
/// "Control record is unique for a PayrollArea" parity).
///
/// DELIBERATE SIMPLIFICATIONS:
/// <list type="bullet">
/// <item><description>The legacy <c>PeriodLocked</c>/<c>PeriodClosed</c> enums (No=0/Yes=1) carry no extra
/// semantics beyond a boolean and are simplified to plain <see cref="bool"/> fields, matching this
/// codebase's convention elsewhere (e.g. <c>WageType.Status</c>).</description></item>
/// <item><description>The legacy SalaryRegister bulk-lock cascade (on PeriodLocked/PeriodClosed
/// transitions) is NOT ported — <c>SalaryRegister</c> doesn't exist yet in this port (a later
/// wave).</description></item>
/// <item><description>The legacy commented-out/dead retro-log block (<c>EarliestMasterDataChange</c> reset)
/// is NOT ported (already dead code in the legacy source).</description></item>
/// <item><description>The legacy computed, non-persistent <c>PayrollStatus</c> display string is a pure UI
/// convenience and is NOT ported — it can be derived client-side from
/// <see cref="PeriodLocked"/>/<see cref="PeriodClosed"/>/<see cref="CalendarMonthStart"/> if
/// needed.</description></item>
/// </list>
///
/// The 12-branch fiscal-period-shift calculator (legacy <c>GetStartEndDateFromPayrollPeriod</c>) is ported
/// faithfully as the self-contained, pure static <see cref="PayrollPeriodShiftCalculator"/> helper (no
/// cross-module dependency), consumed by <c>PayrollControlRecordsAppService</c> when computing
/// <see cref="CalendarMonthStart"/>/<see cref="CalendarMonthEnd"/> on create.
/// </summary>
public class PayrollControlRecord : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int PayrollAreaId { get; set; }

    public virtual PayrollArea PayrollArea { get; set; }

    public virtual int PayrollMonth { get; set; }

    public virtual int PayrollYear { get; set; }

    public virtual DateTime CalendarMonthStart { get; set; }

    public virtual DateTime CalendarMonthEnd { get; set; }

    /// <summary>Simplified from the legacy PeriodLocked enum (No/Yes) — see class remarks.</summary>
    public virtual bool PeriodLocked { get; set; }

    /// <summary>Simplified from the legacy PeriodClosed enum (No/Yes) — see class remarks.</summary>
    public virtual bool PeriodClosed { get; set; }

    /// <summary>
    /// Which of the three states the period is in. <see cref="PeriodLocked"/>/<see cref="PeriodClosed"/> stay the
    /// stored truth the engine and the payroll lock read; this names the state and distinguishes a period merely not
    /// started (<see cref="PayrollPeriodState.Open"/>) from one deliberately reopened to correct data
    /// (<see cref="PayrollPeriodState.ReleasedForCorrection"/>). Always set through <see cref="ApplyState"/>.
    /// </summary>
    public virtual PayrollPeriodState PeriodState { get; set; }

    /// <summary>When the state last changed, and by whom. Null until somebody moves it.</summary>
    public virtual DateTime? StateChangedTime { get; set; }

    public virtual long? StateChangedUserId { get; set; }

    /// <summary>
    /// Moves the period to <paramref name="state"/> and keeps the two legacy flags in step, so a state change and
    /// what the engine sees can never disagree.
    /// </summary>
    public virtual void ApplyState(PayrollPeriodState state, long? userId, DateTime now)
    {
        PeriodState = state;
        PeriodLocked = state == PayrollPeriodState.ReleasedForPayroll;
        PeriodClosed = state == PayrollPeriodState.Closed;
        StateChangedTime = now;
        StateChangedUserId = userId;
    }

    /// <summary>
    /// The state the stored flags describe. Used when reading rows written before states existed, and to keep a
    /// direct change of the flags (the old screen, an import) from leaving the state behind.
    /// </summary>
    public virtual PayrollPeriodState StateFromFlags()
    {
        if (PeriodClosed)
        {
            return PayrollPeriodState.Closed;
        }

        if (PeriodLocked)
        {
            return PayrollPeriodState.ReleasedForPayroll;
        }

        // An unlocked, unclosed period is Open unless somebody deliberately released it for correction.
        return PeriodState == PayrollPeriodState.ReleasedForCorrection
            ? PayrollPeriodState.ReleasedForCorrection
            : PayrollPeriodState.Open;
    }
}
