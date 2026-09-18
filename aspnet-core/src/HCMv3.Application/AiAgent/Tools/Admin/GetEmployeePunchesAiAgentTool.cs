using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Timing;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// "Show 1002's punches for the last 7 days": the clock-in and clock-out events on the Time Events screen, for one
/// employee over a date range, grouped by day with the first in and last out worked out here -- a small model
/// reading forty raw punches gets the first and last wrong far more often than it copies them.
///
/// <para>
/// Read through the Time Events screen's service, so only a user with that screen can use it, and it shows what
/// the screen shows: approved punches (a missing-punch request still awaiting approval is not listed).
/// </para>
/// </summary>
public class GetEmployeePunchesAiAgentTool : IAiAgentTool
{
    /// <summary>A range longer than this is almost always a mistyped date, and would flood the model.</summary>
    public const int MaxDays = 31;

    private const int MaxPunches = 500;

    private readonly AiPayLookup _lookup;
    private readonly ITimeEventsAppService _timeEvents;
    private readonly ITransTypesAppService _transTypes;

    public GetEmployeePunchesAiAgentTool(AiPayLookup lookup, ITimeEventsAppService timeEvents, ITransTypesAppService transTypes)
    {
        _lookup = lookup;
        _timeEvents = timeEvents;
        _transTypes = transTypes;
    }

    public string Name => "get_employee_punches";

    public string Description =>
        "HR administration: an employee's clock-in/clock-out punches (time events) per day for a date range, with " +
        "first in and last out. Work out fromDate and toDate from the details (today, yesterday, last 7 days).";

    public string RequiredPermission => AppPermissions.Pages_TimeManagement_TimeEvents;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["employeeCode"] = new JsonObject { ["type"] = "string" },
            ["fromDate"] = new JsonObject { ["type"] = "string", ["description"] = "yyyy-MM-dd" },
            ["toDate"] = new JsonObject { ["type"] = "string", ["description"] = "yyyy-MM-dd; same as fromDate for one day." }
        }
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var code = arguments.GetString("employeeCode");
        if (code == null)
        {
            return AiToolReplies.Ask("Ask the user which employee.");
        }

        var today = Clock.Now.Date;
        var from = (arguments.GetDate("fromDate") ?? today).Date;
        var to = (arguments.GetDate("toDate") ?? from).Date;

        if (to < from)
        {
            (from, to) = (to, from);
        }

        if ((to - from).TotalDays + 1 > MaxDays)
        {
            return AiToolReplies.Ask($"That is more than {MaxDays} days. Ask the user for a shorter period, or suggest the Time Events screen or attendance reports.");
        }

        var (employee, similar) = await _lookup.ResolveEmployeeAsync(code);
        if (employee == null)
        {
            return AiToolReplies.EmployeeNotFound(code, similar);
        }

        var punches = (await _timeEvents.GetAll(new GetAllTimeEventsInput
            {
                EmployeeId = employee.Id,
                TransDateFrom = from,
                TransDateTo = to,
                Sorting = "TransTime ASC",
                MaxResultCount = MaxPunches
            }))
            .Items;

        var clockInTypeId = await ClockInTypeIdAsync();

        var days = punches
            .GroupBy(p => p.TransDate.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var ordered = g.OrderBy(p => p.TransTime).ToList();
                var ins = ordered.Where(p => p.TransTypeId == clockInTypeId).ToList();
                var outs = ordered.Where(p => p.TransTypeId != clockInTypeId).ToList();

                return new
                {
                    date = g.Key.ToString("yyyy-MM-dd ddd", CultureInfo.InvariantCulture),
                    firstIn = clockInTypeId.HasValue && ins.Count > 0 ? Time(ins.First()) : null,
                    lastOut = clockInTypeId.HasValue && outs.Count > 0 ? Time(outs.Last()) : null,
                    punches = ordered.Select(p => $"{Time(p)} {p.TransTypeName}{(p.AccessUnitName == null ? "" : " @ " + p.AccessUnitName)}")
                };
            })
            .ToList();

        var withPunches = days.Select(d => d.date.Substring(0, 10)).ToHashSet();
        var withoutPunches = Enumerable.Range(0, (int)(to - from).TotalDays + 1)
            .Select(i => from.AddDays(i))
            .Where(d => !withPunches.Contains(d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .Select(d => d.ToString("yyyy-MM-dd ddd", CultureInfo.InvariantCulture))
            .ToList();

        return new
        {
            employee = AiPayLookup.Summarise(employee),
            from = from.ToString("yyyy-MM-dd"),
            to = to.ToString("yyyy-MM-dd"),
            days,
            daysWithoutPunches = withoutPunches,
            note = punches.Count >= MaxPunches ? $"Only the first {MaxPunches} punches are shown." : null
        };
    }

    /// <summary>
    /// The punch type with code "1" (<see cref="TransTypeCodes.ClockIn"/>) is an arrival; every other type counts
    /// as a departure -- the rule the Time Engine and the ESS dashboard use. Tenants name punch types freely, so
    /// the name cannot be trusted for this. Null when the user may not read the punch types: the punches are then
    /// listed without a first-in/last-out summary rather than one guessed from names.
    /// </summary>
    private async Task<int?> ClockInTypeIdAsync()
    {
        try
        {
            var types = await _transTypes.GetAll(new GetAllSimpleLookupsInput { MaxResultCount = 1000 });
            return types.Items.FirstOrDefault(t => string.Equals(t.Code?.Trim(), TransTypeCodes.ClockIn, StringComparison.OrdinalIgnoreCase))?.Id;
        }
        catch (AbpAuthorizationException)
        {
            return null;
        }
    }

    private static string Time(TimeEventDto punch) => punch.TransTime.ToString("HH:mm", CultureInfo.InvariantCulture);
}
