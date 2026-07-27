using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.TimeManagement.Services;

/// <inheritdoc cref="IPunchResolutionStrategyResolver"/>
/// <remarks>
/// Reproduces legacy's ternary chain (legacy <c>Program.cs</c> lines ~1263-1316):
/// <c>ActualTime ? ActualTimeService : PlannedTime ? PlannedTimeService : FlexiTime ? FlexiTimeService :
/// TimesheetService</c>. Only the first three statuses are matched by name; EVERYTHING else --
/// <see cref="TimeMgmtStatus.NA"/>, <see cref="TimeMgmtStatus.TimeSheet"/> itself, and any value added to the
/// enum later -- lands on the TimeSheet strategy, exactly like legacy's <c>else</c> arm.
///
/// <para><see cref="TimeMgmtStatus.VariableTime"/> is intentionally NOT special-cased: legacy
/// <c>CreateTimeRegister</c> throws and skips the day long before punch resolution, so this resolver is never
/// reached with it. Should it ever be, it falls through to TimeSheet like every other unhandled value --
/// which is precisely what legacy's ternary would have done too.</para>
///
/// <para>Strategies are injected as <c>IEnumerable&lt;IPunchResolutionStrategy&gt;</c> (ABP/Castle resolves
/// all registered implementations) and matched on
/// <see cref="IPunchResolutionStrategy.TimeMgmtStatus"/>, so adding a strategy requires no change here.</para>
/// </remarks>
public class PunchResolutionStrategyResolver : HCMv3DomainServiceBase, IPunchResolutionStrategyResolver
{
    private readonly List<IPunchResolutionStrategy> _strategies;

    public PunchResolutionStrategyResolver(IEnumerable<IPunchResolutionStrategy> strategies)
    {
        _strategies = strategies?.ToList() ?? new List<IPunchResolutionStrategy>();
    }

    /// <inheritdoc/>
    public IPunchResolutionStrategy Resolve(TimeMgmtStatus timeMgmtStatus)
    {
        if (timeMgmtStatus == TimeMgmtStatus.ActualTime
            || timeMgmtStatus == TimeMgmtStatus.PlannedTime
            || timeMgmtStatus == TimeMgmtStatus.FlexiTime)
        {
            var matched = Find(timeMgmtStatus);
            if (matched != null)
            {
                return matched;
            }
        }

        var fallThrough = Find(TimeMgmtStatus.TimeSheet);
        if (fallThrough == null)
        {
            throw new InvalidOperationException(
                "No IPunchResolutionStrategy registered for TimeMgmtStatus.TimeSheet. It is the mandatory "
                + "fall-through strategy for every unhandled TimeMgmtStatus (legacy Program.cs's final "
                + "': TimesheetService.X' ternary arm), so the Time Engine cannot run without it.");
        }

        return fallThrough;
    }

    private IPunchResolutionStrategy Find(TimeMgmtStatus timeMgmtStatus)
    {
        return _strategies.FirstOrDefault(s => s.TimeMgmtStatus == timeMgmtStatus);
    }
}
