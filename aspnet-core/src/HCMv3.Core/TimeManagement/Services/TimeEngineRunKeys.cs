namespace HCMv3.TimeManagement.Services;

/// <summary>
/// The <see cref="EngineRuns.EngineRun.RunKey"/> a Time Engine run is filed under. Two jobs:
/// <list type="number">
/// <item><description><b>One run at a time per payroll area.</b> A run for an area is keyed
/// <see cref="ForPayrollArea"/>, the same shape as <c>PayrollEngineService.BuildRunKey</c> (the engine types differ,
/// so the two never collide), and a second run of the same area is refused while the first is going. NOT a port:
/// legacy had no guard -- its Run button truncated the whole <c>TimeLog</c> table and queued another thread, so a
/// second run wiped the first one's log and their counters merged.</description></item>
/// <item><description><b>Keeping the payroll engine's runs off the Time Engine screen.</b> With
/// <c>EnableTimeEngineAutorun</c> on, the payroll engine evaluates time for every employee it pays, one employee per
/// time-engine call (<c>PayrollEngineService.TimeEngineAutorun</c>, legacy <c>HCM10.PayrollEngine/Program.cs</c>
/// lines 554-753). Each of those calls is a run of its own; filed under the area key they would bury the operator's
/// own run under hundreds of one-employee runs. Legacy's screen never showed them (they wrote only to the
/// <c>TimeLog</c> the payroll run truncated first), so they are filed under <see cref="FromPayrollEnginePrefix"/>
/// and the screen leaves them out.</description></item>
/// </list>
/// </summary>
public static class TimeEngineRunKeys
{
    /// <summary>
    /// The user name the payroll engine passes on its per-employee time-engine calls
    /// (<c>PayrollEngineService.PayrollEngineOriginatorUserName</c>, which is private to that class -- a test pins
    /// the two together). Also what its <c>ExecutionLog</c> rows carry, which is how the screen's run history leaves
    /// them out.
    /// </summary>
    public const string PayrollEngineOriginatorUserName = "PayrollEngine";

    /// <summary>Prefix of the run key of a time-engine run made by a payroll run.</summary>
    public const string FromPayrollEnginePrefix = "FromPayrollEngine:";

    private const string PayrollAreaPrefix = "PayrollArea:";

    /// <summary>
    /// One key per payroll area per tenant. Normalised so trailing spaces or a different casing cannot smuggle a
    /// second run of the same area past the guard. Null for a blank area -- such a run gets a key of its own.
    /// </summary>
    public static string ForPayrollArea(string payrollAreaName)
    {
        return string.IsNullOrWhiteSpace(payrollAreaName)
            ? null
            : PayrollAreaPrefix + payrollAreaName.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// True for the payroll engine's per-employee time-engine call. Go-Live Setup also sets
    /// <see cref="TimeEngineRunInput.IsRanFromPayrollEngine"/> (it runs the time engine "as the payroll engine
    /// would"), but under the operator's own name and in batches, so its runs stay on the screen as before.
    /// </summary>
    public static bool IsPayrollEngineAutorun(TimeEngineRunInput input)
    {
        return input != null
               && input.IsRanFromPayrollEngine
               && input.CurrentUserName == PayrollEngineOriginatorUserName;
    }

    /// <summary>
    /// The key for a run. <paramref name="payrollAreaName"/> is the area the run belongs to: the one selected, or,
    /// for a run of named employees only, the first employee's (resolved by the caller).
    /// </summary>
    public static string ForRun(TimeEngineRunInput input, string payrollAreaName)
    {
        if (IsPayrollEngineAutorun(input))
        {
            return FromPayrollEnginePrefix + (ForPayrollArea(payrollAreaName) ?? string.Empty);
        }

        return ForPayrollArea(payrollAreaName);
    }

    /// <summary>True for a run key the Time Engine screen does not show.</summary>
    public static bool IsHiddenFromTimeEngineScreen(string runKey)
    {
        return runKey != null && runKey.StartsWith(FromPayrollEnginePrefix, System.StringComparison.Ordinal);
    }
}
