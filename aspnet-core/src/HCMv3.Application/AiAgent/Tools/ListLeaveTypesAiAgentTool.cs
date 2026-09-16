using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HCMv3.Authorization;
using HCMv3.Ess;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// The leave types an employee can pick from -- the same list as the apply-for-leave dropdown. This is how
/// the model turns "sick leave" into an id, and what it offers when the user did not say which type.
/// </summary>
public class ListLeaveTypesAiAgentTool : IAiAgentTool
{
    private readonly IEssLookupAppService _essLookupAppService;

    public ListLeaveTypesAiAgentTool(IEssLookupAppService essLookupAppService)
    {
        _essLookupAppService = essLookupAppService;
    }

    public string Name => "list_leave_types";

    public string Description =>
        "List the leave types an employee can apply for, with their ids and English and Arabic names. " +
        "Use it to find the id of a leave type the user mentions, or to offer the choices when the user " +
        "has not said which type of leave they want.";

    public string RequiredPermission => AppPermissions.Ess_MyLeave;

    public JsonObject BuildParameters() => AiToolArguments.NoParameters();

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var types = await _essLookupAppService.GetAbsenceTypes();

        return new
        {
            leaveTypes = types.Select(t => new
            {
                id = t.Id,
                code = t.Code,
                nameEn = t.NameEn,
                nameAr = t.NameAr
            })
        };
    }
}
