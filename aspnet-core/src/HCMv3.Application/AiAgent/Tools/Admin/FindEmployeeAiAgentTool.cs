using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HCMv3.Authorization;

namespace HCMv3.AiAgent.Tools;

/// <summary>Finds employees by code or name, for an administrator. Same data and permission as the Employees screen.</summary>
public class FindEmployeeAiAgentTool : IAiAgentTool
{
    private readonly AiPayLookup _lookup;

    public FindEmployeeAiAgentTool(AiPayLookup lookup)
    {
        _lookup = lookup;
    }

    public string Name => "find_employee";

    public string Description =>
        "HR administration: search employees by employee code, name, email or civil ID. Returns code, English and " +
        "Arabic name and status. Use it when the user names an employee instead of giving the code, or to check a code.";

    public string RequiredPermission => AppPermissions.Pages_PersonnelAdministration_Employees;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["query"] = new JsonObject { ["type"] = "string", ["description"] = "Employee code, name, email or civil ID." }
        },
        ["required"] = new JsonArray("query")
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var query = arguments.GetString("query") ?? throw new AiToolArgumentException("'query' is required.");

        var employees = await _lookup.SearchEmployeesAsync(query);

        return new
        {
            count = employees.Count,
            employees = employees
                .OrderByDescending(e => string.Equals(e.Code, query, System.StringComparison.OrdinalIgnoreCase))
                .Select(AiPayLookup.Summarise),
            note = employees.Count == 0 ? "No employee matches. Ask the user to check the code or name." : null
        };
    }
}
