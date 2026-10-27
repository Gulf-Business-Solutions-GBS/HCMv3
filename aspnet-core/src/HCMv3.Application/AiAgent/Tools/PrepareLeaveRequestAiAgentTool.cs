using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.AiAgent.Actions;
using HCMv3.Authorization;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.Workflow;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// Drafts a leave application for the user to confirm. Never submits.
///
/// <para>
/// This tool is where "I want 10 days of leave" becomes a question back to the user. Every argument is
/// optional in the schema on purpose: a model that must supply all of them invents the ones it does not
/// have, whereas one that may leave them out gets told exactly what to ask for.
/// </para>
///
/// <para>
/// The balance and overlap checks here are for a helpful answer NOW, not for safety -- the leave service
/// repeats its own checks when the user confirms, against the data as it is then.
/// </para>
/// </summary>
public class PrepareLeaveRequestAiAgentTool : IAiAgentTool
{
    public const int MaxDays = 366;

    private readonly IMyLeaveAppService _myLeaveAppService;
    private readonly IEssLookupAppService _essLookupAppService;
    private readonly AiPendingActionStore _pendingActionStore;

    public PrepareLeaveRequestAiAgentTool(
        IMyLeaveAppService myLeaveAppService,
        IEssLookupAppService essLookupAppService,
        AiPendingActionStore pendingActionStore)
    {
        _myLeaveAppService = myLeaveAppService;
        _essLookupAppService = essLookupAppService;
        _pendingActionStore = pendingActionStore;
    }

    public string Name => "prepare_leave_request";

    public string Description =>
        "Prepare a leave application for the user to confirm. This does NOT submit anything: the user sees a " +
        "card with a Confirm button under your reply. Call it when the user wants to apply for leave. You need " +
        "the leave type id (from list_leave_types), the first day, and either the last day or the number of " +
        "days. Leave out anything the user has not told you -- the result says what to ask for. " +
        "Days are calendar days counted from the first day to the last day inclusive.";

    public string RequiredPermission => AppPermissions.Ess_MyLeave;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["leaveTypeId"] = new JsonObject { ["type"] = "integer", ["description"] = "Id of the leave type, from list_leave_types." },
            ["startDate"] = new JsonObject { ["type"] = "string", ["description"] = "First day of leave, yyyy-MM-dd." },
            ["endDate"] = new JsonObject { ["type"] = "string", ["description"] = "Last day of leave, yyyy-MM-dd. Give this or days." },
            ["days"] = new JsonObject { ["type"] = "integer", ["description"] = "Number of calendar days including the first day. Give this or endDate." },
            ["comments"] = new JsonObject { ["type"] = "string", ["description"] = "Optional reason or note from the user." }
        }
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var leaveTypeId = arguments.GetInt("leaveTypeId");
        var startDate = arguments.GetDate("startDate");
        var endDate = arguments.GetDate("endDate");
        var days = arguments.GetInt("days");
        var comments = arguments.GetString("comments");

        var leaveTypes = await _essLookupAppService.GetAbsenceTypes();
        var leaveType = leaveTypeId.HasValue ? leaveTypes.FirstOrDefault(t => t.Id == leaveTypeId.Value) : null;

        var missing = new List<string>();
        if (leaveType == null)
        {
            missing.Add("which type of leave");
        }

        if (!startDate.HasValue)
        {
            missing.Add("the first day of leave");
        }

        if (!endDate.HasValue && !days.HasValue)
        {
            missing.Add("the last day of leave or how many days");
        }

        if (missing.Count > 0)
        {
            return new
            {
                status = "needs_more_information",
                missing,
                instruction = "Nothing was prepared. Ask the user, in one short question, for: " + string.Join("; ", missing) +
                              (leaveType == null ? ". List the leave type names for them to choose from." : "."),
                leaveTypes = leaveType == null
                    ? leaveTypes.Select(t => new { id = t.Id, nameEn = t.NameEn, nameAr = t.NameAr })
                    : null
            };
        }

        if (days is < 1)
        {
            throw new AiToolArgumentException("days must be at least 1.");
        }

        var start = startDate!.Value;
        var end = endDate ?? start.AddDays(days!.Value - 1);
        var totalDays = (end - start).Days + 1;

        if (endDate.HasValue && days.HasValue && days.Value != totalDays)
        {
            return NotPrepared("needs_more_information",
                $"The last day {end:yyyy-MM-dd} gives {totalDays} days, but {days} days were given. Ask the user which is right.");
        }

        if (end < start)
        {
            return NotPrepared("needs_more_information", "The last day is before the first day. Ask the user for the correct dates.");
        }

        if (totalDays > MaxDays)
        {
            return NotPrepared("needs_more_information", $"{totalDays} days is too long for one application. Ask the user to check the dates.");
        }

        var balance = (await _myLeaveAppService.GetMyBalances()).FirstOrDefault(b => b.AbsenceTypeId == leaveType!.Id);

        // No quota row means the type is not quota-managed (the leave service skips the check too), so only a
        // type the employee HOLDS a balance for is checked here.
        if (balance != null && totalDays > balance.Bookable)
        {
            return NotPrepared("not_possible",
                $"The user can book at most {Math.Round(balance.Bookable, 2)} day(s) of {leaveType!.NameEn} ({leaveType.NameAr}) " +
                $"but asked for {totalDays}. Tell them; nothing was prepared.");
        }

        var clash = await FindOverlapAsync(start, end);
        if (clash != null)
        {
            return NotPrepared("not_possible",
                $"The user already has {clash.AbsenceTypeNameEn} ({clash.AbsenceTypeNameAr}) from {clash.StartDate:yyyy-MM-dd} to " +
                $"{clash.EndDate:yyyy-MM-dd} ({GetMyLeaveRequestsAiAgentTool.DescribeStatus(clash.WorkflowStatus)}), which overlaps. " +
                "Tell them; nothing was prepared.");
        }

        await _pendingActionStore.CreateAsync(context, ApplyForLeaveAiPendingActionHandler.Type, new ApplyForLeaveAiPendingActionHandler.Payload
        {
            AbsenceTypeId = leaveType!.Id,
            LeaveTypeNameEn = leaveType.NameEn,
            LeaveTypeNameAr = leaveType.NameAr,
            StartDate = start,
            EndDate = end,
            Days = totalDays,
            Comments = comments
        });

        return new
        {
            status = "draft_ready",
            leaveTypeEn = leaveType.NameEn,
            leaveTypeAr = leaveType.NameAr,
            startDate = start.ToString("yyyy-MM-dd"),
            endDate = end.ToString("yyyy-MM-dd"),
            days = totalDays,
            bookableAfterThis = balance == null ? (double?)null : Math.Round(balance.Bookable - totalDays, 2),
            startsInThePast = start < Clock.Now.Date ? true : (bool?)null,
            instruction = "Summarise it in one or two sentences and tell the user to press Confirm on the card below " +
                          "to submit it. It has NOT been submitted yet -- do not say it has."
        };
    }

    /// <summary>
    /// Pending requests block as well as approved ones -- the same rule the leave workflow handler applies at
    /// submit -- and only a rejected one does not.
    /// </summary>
    private async Task<MyLeaveDto> FindOverlapAsync(DateTime start, DateTime end)
    {
        var leaves = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput
        {
            // Only reach into history when the new leave itself starts in the past; otherwise current and
            // future leave is everything that can overlap.
            IncludeHistory = start < Clock.Now.Date,
            MaxResultCount = 1000
        });

        return leaves.Items.FirstOrDefault(l =>
            l.WorkflowStatus != RequestStatus.Rejected
            && l.StartDate.Date <= end
            && l.EndDate.Date >= start);
    }

    private static object NotPrepared(string status, string instruction) => new { status, instruction };
}
