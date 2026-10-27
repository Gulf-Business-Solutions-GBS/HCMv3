using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Abp.Authorization;
using HCMv3.AiAgent.Actions;
using HCMv3.Authorization;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// Drafts "put 1002 on the night shift on 20 and 21 September": a substitute duty -- a one-day override of the
/// employee's normal roster -- for each day in a short range. Never saves; the administrator confirms the card.
///
/// <para>
/// Saved through the Employee Substitute Duties screen's service, so where the company routes duty changes
/// through approval, a new override waits for approval exactly as one entered on the screen would.
/// </para>
/// </summary>
public class PrepareDutyChangeAiAgentTool : IAiAgentTool
{
    /// <summary>Overrides are for days, not for a new pattern; longer is a roster change and belongs on the screen.</summary>
    public const int MaxDays = 14;

    private readonly AiPayLookup _lookup;
    private readonly AiMasterDataLookup _masterData;
    private readonly IEmployeeSubstituteDutiesAppService _substituteDuties;
    private readonly IPermissionChecker _permissionChecker;
    private readonly AiPendingActionStore _pendingActionStore;

    public PrepareDutyChangeAiAgentTool(
        AiPayLookup lookup,
        AiMasterDataLookup masterData,
        IEmployeeSubstituteDutiesAppService substituteDuties,
        IPermissionChecker permissionChecker,
        AiPendingActionStore pendingActionStore)
    {
        _lookup = lookup;
        _masterData = masterData;
        _substituteDuties = substituteDuties;
        _permissionChecker = permissionChecker;
        _pendingActionStore = pendingActionStore;
    }

    public string Name => "prepare_duty_change";

    public string Description =>
        $"HR administration: prepare a duty (shift) change for an employee on one day or up to {MaxDays} days, for the " +
        "user to confirm. Does NOT save. Overrides the normal roster on those days only.";

    public string RequiredPermission => AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Create;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["employeeCode"] = new JsonObject { ["type"] = "string" },
            ["duty"] = new JsonObject { ["type"] = "string", ["description"] = "Duty/shift name, e.g. Night Shift." },
            ["fromDate"] = new JsonObject { ["type"] = "string", ["description"] = "yyyy-MM-dd" },
            ["toDate"] = new JsonObject { ["type"] = "string", ["description"] = "yyyy-MM-dd; leave out for one day." }
        }
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var employeeCode = arguments.GetString("employeeCode");
        var dutyText = arguments.GetString("duty");
        var fromDate = arguments.GetDate("fromDate");

        var missing = new List<string>();
        if (employeeCode == null) missing.Add("the employee");
        if (dutyText == null) missing.Add("the duty (shift)");
        if (!fromDate.HasValue) missing.Add("the day or days");

        if (missing.Count > 0)
        {
            return AiToolReplies.Ask("Nothing was prepared. Ask the user for: " + string.Join("; ", missing) + ".");
        }

        var from = fromDate!.Value.Date;
        var to = (arguments.GetDate("toDate") ?? from).Date;
        if (to < from)
        {
            (from, to) = (to, from);
        }

        if ((to - from).TotalDays + 1 > MaxDays)
        {
            return AiToolReplies.NotPossible($"That is more than {MaxDays} days. A longer change is a roster or duty period change, made on the Employee Duty Periods screen. Nothing was prepared.");
        }

        var lacking = await AiToolReplies.FirstMissingAsync(_permissionChecker,
            AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Create,
            AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Edit);
        if (lacking != null)
        {
            return AiToolReplies.NotPossible($"The user does not have the '{lacking}' permission needed to change duties. Nothing was prepared.");
        }

        var (employee, similar) = await _lookup.ResolveEmployeeAsync(employeeCode);
        if (employee == null)
        {
            return AiToolReplies.EmployeeNotFound(employeeCode, similar);
        }

        var match = await _masterData.ResolveAsync(AiMasterDataKind.Duty, dutyText);
        if (match.Match == null)
        {
            return AiToolReplies.NotResolved(AiMasterDataKind.Duty, dutyText, match);
        }

        var duty = match.Match;
        var existing = await ChangeDutyAiPendingActionHandler.LoadOverridesAsync(_substituteDuties, employee.Id, from, to);

        var days = new List<ChangeDutyAiPendingActionHandler.Day>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var current = existing.FirstOrDefault(e => e.SubstituteDate.Date == date);
            if (current?.DutyId == duty.Id)
            {
                continue;
            }

            days.Add(new ChangeDutyAiPendingActionHandler.Day
            {
                Date = date,
                ExistingOverrideId = current?.Id,
                ExistingDutyName = current?.DutyName
            });
        }

        if (days.Count == 0)
        {
            return AiToolReplies.NotPossible($"Employee {employee.Code} is already on {duty.NameEn} on those days. Nothing was prepared. Tell the user.");
        }

        await _pendingActionStore.CreateAsync(context, ChangeDutyAiPendingActionHandler.Type, new ChangeDutyAiPendingActionHandler.Payload
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeNameEn = employee.FullNameEn,
            EmployeeNameAr = employee.FullNameAr,
            DutyId = (int)duty.Id,
            DutyNameEn = duty.NameEn,
            DutyNameAr = duty.NameAr,
            DutyNote = duty.Note,
            From = from,
            To = to,
            Days = days,
            Snapshot = ChangeDutyAiPendingActionHandler.Snapshot(existing)
        });

        return new
        {
            status = "draft_ready",
            employee = AiPayLookup.Summarise(employee),
            duty = AiMasterDataLookup.Summarise(duty),
            days = days.Select(d => d.Date.ToString("yyyy-MM-dd ddd", CultureInfo.InvariantCulture)),
            replacesEarlierOverride = days.Where(d => d.ExistingDutyName != null).Select(d => new { date = d.Date.ToString("yyyy-MM-dd"), was = d.ExistingDutyName }),
            instruction = "Summarise the change in one sentence and tell the user to press Confirm on the card below. It has NOT been saved yet. " +
                          "If the company routes duty changes for approval, it will wait for approval after Confirm."
        };
    }
}
