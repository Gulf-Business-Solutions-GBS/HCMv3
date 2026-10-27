using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HCMv3.AiAgent.Actions;
using HCMv3.Authorization;
using HCMv3.Ess;
using HCMv3.Ess.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>Drafts the withdrawal of one of the user's own leave requests. Never cancels anything itself.</summary>
public class PrepareLeaveCancellationAiAgentTool : IAiAgentTool
{
    private readonly IMyLeaveAppService _myLeaveAppService;
    private readonly AiPendingActionStore _pendingActionStore;

    public PrepareLeaveCancellationAiAgentTool(IMyLeaveAppService myLeaveAppService, AiPendingActionStore pendingActionStore)
    {
        _myLeaveAppService = myLeaveAppService;
        _pendingActionStore = pendingActionStore;
    }

    public string Name => "prepare_leave_cancellation";

    public string Description =>
        "Prepare the withdrawal of one of the user's own leave requests, for the user to confirm. This does NOT " +
        "cancel anything: the user sees a card with a Confirm button under your reply. Only requests that are not " +
        "yet approved can be withdrawn (canCancel true in get_my_leave_requests). Get the id from get_my_leave_requests; " +
        "if the user has several, ask which one.";

    public string RequiredPermission => AppPermissions.Ess_MyLeave;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["leaveRequestId"] = new JsonObject { ["type"] = "integer", ["description"] = "The id from get_my_leave_requests." }
        },
        ["required"] = new JsonArray("leaveRequestId")
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var id = arguments.GetInt("leaveRequestId");
        if (!id.HasValue)
        {
            return new
            {
                status = "needs_more_information",
                instruction = "Nothing was prepared. Call get_my_leave_requests and ask the user which request to withdraw."
            };
        }

        // Found through the caller's own list, so an id belonging to someone else is simply not there.
        var leaves = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput { IncludeHistory = true, MaxResultCount = 1000 });
        var leave = leaves.Items.FirstOrDefault(l => l.Id == id.Value);

        if (leave == null)
        {
            return new
            {
                status = "not_possible",
                instruction = "That is not one of the user's leave requests. Call get_my_leave_requests to find the right one."
            };
        }

        if (!leave.CanCancel)
        {
            return new
            {
                status = "not_possible",
                instruction = $"This request ({GetMyLeaveRequestsAiAgentTool.DescribeStatus(leave.WorkflowStatus)}) can no longer be withdrawn. " +
                              "Approved leave can only be changed by requesting an amendment on the My Leave screen, or by contacting HR. Nothing was prepared."
            };
        }

        await _pendingActionStore.CreateAsync(context, CancelLeaveAiPendingActionHandler.Type, new CancelLeaveAiPendingActionHandler.Payload
        {
            EmployeeAbsenceId = leave.Id,
            LeaveTypeNameEn = leave.AbsenceTypeNameEn,
            LeaveTypeNameAr = leave.AbsenceTypeNameAr,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            Days = leave.TotalLeaveDays
        });

        return new
        {
            status = "draft_ready",
            leaveTypeEn = leave.AbsenceTypeNameEn,
            leaveTypeAr = leave.AbsenceTypeNameAr,
            startDate = leave.StartDate.ToString("yyyy-MM-dd"),
            endDate = leave.EndDate.ToString("yyyy-MM-dd"),
            instruction = "Tell the user to press Confirm on the card below to withdraw it. It has NOT been withdrawn yet."
        };
    }
}
