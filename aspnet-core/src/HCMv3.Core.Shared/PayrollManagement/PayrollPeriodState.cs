namespace HCMv3.PayrollManagement;

/// <summary>
/// Where a payroll period stands, as the payroll control record shows it.
///
/// <para>Legacy had the same three states, but only as two flags (<c>PeriodLocked</c>, <c>PeriodClosed</c>) that the
/// user ticked by hand. They are still stored, and still what the engine and the payroll lock read, so nothing about
/// a run changes; this says which of the three a period is in, in one field, and gives
/// <see cref="ReleasedForCorrection"/> a name of its own.</para>
/// </summary>
public enum PayrollPeriodState
{
    /// <summary>Not started. Master data can be changed; a regular payroll run is not allowed yet.</summary>
    Open = 0,

    /// <summary>
    /// The payroll window. Master data for these employees is frozen, and a regular run requires this state.
    /// Legacy: PeriodLocked = Yes.
    /// </summary>
    ReleasedForPayroll = 1,

    /// <summary>
    /// Deliberately reopened so data can be corrected, with the intention of locking again and running payroll once
    /// more. Master data can be changed, exactly as when Open -- the difference is that it is recorded as a decision,
    /// with who made it and when. Go-Live Setup puts a period here whenever it corrects data.
    /// </summary>
    ReleasedForCorrection = 2,

    /// <summary>
    /// Payroll for the period is finished ("exit payroll"). Off-cycle runs are allowed.
    /// Legacy: PeriodClosed = Yes.
    /// </summary>
    Closed = 3,
}
