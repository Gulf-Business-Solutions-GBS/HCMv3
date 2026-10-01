using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HCMv3.SetupChecks;

/// <summary>
/// One piece of setup a release feature needs, and how to tell whether a tenant has done it.
///
/// <para><b>Read only, one tenant.</b> A check never chooses the tenant: <see cref="SetupCheckRunner"/> has already
/// narrowed every repository to the tenant being checked before <see cref="RunAsync"/> is called, and it throws away
/// anything a check might write. A check counts and names by CODE only -- never names, salaries or other personal
/// details.</para>
///
/// <para><b>Adding one.</b> Implement this, add the type to <see cref="SetupCheckCatalog"/>, list its code on the
/// release feature that needs it, set <see cref="IntroducedIn"/> to that release, and add the keys
/// <c>SetupCheck_{Code without dash}_Title</c> and <c>..._Why</c> in English and Arabic. The guard tests check all of
/// that.</para>
/// </summary>
public interface ISetupCheck
{
    /// <summary>Stable id quoted by support, e.g. <c>WFL-001</c>. Never reused or renumbered.</summary>
    string Code { get; }

    /// <summary>The <see cref="Releases.HCMv3Releases"/> version this check came in with.</summary>
    string IntroducedIn { get; }

    SetupCheckSeverity Severity { get; }

    /// <summary>Screen Code (see <c>ScreenCatalog</c>) where the missing setup is maintained.</summary>
    string FixScreenCode { get; }

    Task<SetupCheckFinding> RunAsync(SetupCheckContext context);
}

/// <summary>What a check is given: the day to judge "current" records by.</summary>
public class SetupCheckContext
{
    public SetupCheckContext(DateTime today)
    {
        Today = today.Date;
    }

    public DateTime Today { get; }
}

/// <summary>What one check found.</summary>
public class SetupCheckFinding
{
    /// <summary>Most examples a finding carries; the count still says how many there are in all.</summary>
    public const int MaxExamples = 20;

    private SetupCheckFinding(SetupCheckOutcome outcome, int checkedCount, int missingCount, IReadOnlyList<string> examples, string message)
    {
        Outcome = outcome;
        CheckedCount = checkedCount;
        MissingCount = missingCount;
        Examples = examples;
        Message = message;
    }

    public SetupCheckOutcome Outcome { get; }

    /// <summary>How many things were looked at (employees, payroll areas, settings rows...).</summary>
    public int CheckedCount { get; }

    /// <summary>How many of them lack the setup.</summary>
    public int MissingCount { get; }

    /// <summary>Codes of the first few that lack it, sorted, at most <see cref="MaxExamples"/>.</summary>
    public IReadOnlyList<string> Examples { get; }

    /// <summary>Only for <see cref="SetupCheckOutcome.Error"/>.</summary>
    public string Message { get; }

    public static SetupCheckFinding NotApplicable() =>
        new(SetupCheckOutcome.NotApplicable, 0, 0, Array.Empty<string>(), null);

    public static SetupCheckFinding Error(string message) =>
        new(SetupCheckOutcome.Error, 0, 0, Array.Empty<string>(), message);

    /// <summary>For a check on one thing that either exists or not, with nothing to name.</summary>
    public static SetupCheckFinding Exists(bool exists) =>
        new(exists ? SetupCheckOutcome.Ok : SetupCheckOutcome.Missing, 1, exists ? 0 : 1, Array.Empty<string>(), null);

    /// <summary>Ok when nothing is missing, Missing otherwise; not applicable when there was nothing to check.</summary>
    public static SetupCheckFinding From(int checkedCount, IReadOnlyCollection<string> missingCodes)
    {
        if (checkedCount == 0)
        {
            return NotApplicable();
        }

        var missing = missingCodes ?? Array.Empty<string>();
        var examples = missing.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).Take(MaxExamples).ToList();
        return new SetupCheckFinding(
            missing.Count == 0 ? SetupCheckOutcome.Ok : SetupCheckOutcome.Missing,
            checkedCount,
            missing.Count,
            examples,
            null);
    }
}
