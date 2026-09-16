using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HCMv3.Authorization;
using HCMv3.Ess;

namespace HCMv3.AiAgent.Tools;

/// <summary>The employee's own leave balances, through the same service as the My Leave screen.</summary>
public class GetMyLeaveBalancesAiAgentTool : IAiAgentTool
{
    private readonly IMyLeaveAppService _myLeaveAppService;

    public GetMyLeaveBalancesAiAgentTool(IMyLeaveAppService myLeaveAppService)
    {
        _myLeaveAppService = myLeaveAppService;
    }

    public string Name => "get_my_leave_balances";

    public string Description =>
        "Get the logged-in employee's own leave balance for each leave type they hold: accrued, used, " +
        "pending (applied for, awaiting approval), remaining, and bookable (what they can still apply for). " +
        "Use for any question about how much leave the user has left.";

    public string RequiredPermission => AppPermissions.Ess_MyLeave;

    public JsonObject BuildParameters() => AiToolArguments.NoParameters();

    public async Task<object> ExecuteAsync(AiToolArguments arguments)
    {
        var balances = await _myLeaveAppService.GetMyBalances();

        if (balances.Count == 0)
        {
            return new
            {
                balances = Array.Empty<object>(),
                note = "No leave entitlement is set up for this employee for the current period. Tell the user to contact HR."
            };
        }

        return new
        {
            balances = balances.Select(b => new
            {
                leaveTypeId = b.AbsenceTypeId,
                leaveTypeEn = b.AbsenceTypeNameEn,
                leaveTypeAr = b.AbsenceTypeNameAr,
                accrued = Round(b.Accrued),
                used = Round(b.Used),
                pending = Round(b.Pending),
                remaining = Round(b.Remaining),
                bookable = Round(b.Bookable)
            })
        };
    }

    private static double Round(double value) => Math.Round(value, 2);
}
