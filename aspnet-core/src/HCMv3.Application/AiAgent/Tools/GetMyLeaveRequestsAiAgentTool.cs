using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HCMv3.Authorization;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.Workflow;

namespace HCMv3.AiAgent.Tools;

/// <summary>The employee's own leave requests and where each one stands.</summary>
public class GetMyLeaveRequestsAiAgentTool : IAiAgentTool
{
    public const int MaxRequests = 25;

    private readonly IMyLeaveAppService _myLeaveAppService;

    public GetMyLeaveRequestsAiAgentTool(IMyLeaveAppService myLeaveAppService)
    {
        _myLeaveAppService = myLeaveAppService;
    }

    public string Name => "get_my_leave_requests";

    public string Description =>
        "List the logged-in employee's own leave with dates, number of days and status " +
        "(Pending approval, Approved, Rejected, Draft). Newest first. By default returns current and future " +
        "leave plus anything still pending; set includeHistory to true to include past leave. " +
        "Use for questions about the status of a leave request or upcoming leave.";

    public string RequiredPermission => AppPermissions.Ess_MyLeave;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["includeHistory"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "true to include leave that has already ended. Default false."
            }
        }
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var page = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput
        {
            IncludeHistory = arguments.GetBool("includeHistory") ?? false,
            MaxResultCount = MaxRequests
        });

        return new
        {
            totalCount = page.TotalCount,
            shown = page.Items.Count,
            leaveRequests = page.Items.Select(l => new
            {
                id = l.Id,
                leaveTypeEn = l.AbsenceTypeNameEn,
                leaveTypeAr = l.AbsenceTypeNameAr,
                startDate = l.StartDate.ToString("yyyy-MM-dd"),
                endDate = l.EndDate.ToString("yyyy-MM-dd"),
                days = l.TotalLeaveDays,
                status = DescribeStatus(l.WorkflowStatus),
                canCancel = l.CanCancel,
                hasPendingChange = l.HasPendingAmendment,
                comments = l.Comments
            })
        };
    }

    /// <summary>
    /// Words rather than the enum, because the enum's names are the engine's vocabulary: "Open" means
    /// waiting for an approver, which is not what a model -- or an employee -- would take it to mean.
    /// </summary>
    public static string DescribeStatus(RequestStatus? status) => status switch
    {
        // Null: never went through approval (entered by HR, or approval is off), so it is in effect.
        null => "Approved",
        RequestStatus.Open => "Pending approval",
        RequestStatus.Approved => "Approved",
        RequestStatus.Rejected => "Rejected",
        RequestStatus.Closed => "Closed",
        RequestStatus.Draft => "Draft (not submitted)",
        RequestStatus.Transferred => "Transferred to another approver",
        _ => status.ToString()
    };
}
