using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HCMv3.Authorization;

namespace HCMv3.AiAgent.Tools;

/// <summary>Lists the wage types an administrator can use for basic pay or for a one-time payment.</summary>
public class FindWageTypesAiAgentTool : IAiAgentTool
{
    private readonly AiPayLookup _lookup;

    public FindWageTypesAiAgentTool(AiPayLookup lookup)
    {
        _lookup = lookup;
    }

    public string Name => "find_wage_types";

    public string Description =>
        "HR administration: list wage types (allowances, earnings, deductions) with code, names and whether they " +
        "are an Earning or a Deduction. Set usage to basic_pay for what can go on basic pay, or one_time for bonuses " +
        "and one-off deductions. Optional query filters by name or code. Use it when the user asks what exists or " +
        "when a wage type they named was not found.";

    public string RequiredPermission => AppPermissions.Pages_PayrollManagement_WageTypes;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["usage"] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray("basic_pay", "one_time"),
                ["description"] = "basic_pay or one_time. Default one_time."
            },
            ["query"] = new JsonObject { ["type"] = "string", ["description"] = "Optional part of the name or code." }
        }
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var usage = ParseUsage(arguments.GetString("usage"));
        var query = arguments.GetString("query");

        var wageTypes = await _lookup.GetAllowedWageTypesAsync(usage);

        if (query != null)
        {
            wageTypes = wageTypes.Where(w =>
                    (w.Code ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (w.NameEn ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (w.NameAr ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return new
        {
            usage = usage == AiWageTypeUsage.BasicPay ? "basic_pay" : "one_time",
            count = wageTypes.Count,
            wageTypes = wageTypes.Take(50).Select(AiPayLookup.Summarise)
        };
    }

    public static AiWageTypeUsage ParseUsage(string value) =>
        value != null && value.Replace(" ", "_").StartsWith("basic", StringComparison.OrdinalIgnoreCase)
            ? AiWageTypeUsage.BasicPay
            : AiWageTypeUsage.OneTimePayment;
}
