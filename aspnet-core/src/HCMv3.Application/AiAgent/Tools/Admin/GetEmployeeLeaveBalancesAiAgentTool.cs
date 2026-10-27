using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.Authorization;
using HCMv3.Ess;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// Another employee's leave balances, for HR -- "what is the leave balance of 1002". The self-service leave
/// tools only ever show the signed-in user's OWN data, so without this an administrator got "I don't have
/// access" (seen on the Webtropia server).
///
/// <para>
/// Stands in for the Employee Absence Quotas screen and needs its permission. The balance is the one that
/// screen shows (accrued - used - adjusted - forfeited). Note that the employee's own My Leave balance counts
/// adjustments the other way (accrued + adjusted - used - forfeited); the parts are returned too so the two can
/// be told apart. Which is right is an open business question, not something to settle here.
/// </para>
/// </summary>
public class GetEmployeeLeaveBalancesAiAgentTool : IAiAgentTool
{
    private readonly AiPayLookup _lookup;
    private readonly IEmployeeAbsenceQuotasAppService _quotas;
    private readonly IEssLookupAppService _essLookup;

    public GetEmployeeLeaveBalancesAiAgentTool(AiPayLookup lookup, IEmployeeAbsenceQuotasAppService quotas, IEssLookupAppService essLookup)
    {
        _lookup = lookup;
        _quotas = quotas;
        _essLookup = essLookup;
    }

    public string Name => "get_employee_leave_balances";

    public string Description =>
        "HR administration: another employee's leave balance for each leave type (accrued, used, adjusted, forfeited, " +
        "balance) for the entitlement period covering a date (default today). Needs the employee code, e.g. 1002. " +
        "Use when the user asks about a specific employee's leave balance. For the user's OWN balance use get_my_leave_balances.";

    public string RequiredPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["employeeCode"] = new JsonObject { ["type"] = "string", ["description"] = "The employee code, e.g. 1002." },
            ["asOfDate"] = new JsonObject { ["type"] = "string", ["description"] = "Optional date, yyyy-MM-dd. Default today." }
        },
        ["required"] = new JsonArray("employeeCode")
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var code = arguments.GetString("employeeCode");
        if (code == null)
        {
            return new { status = "needs_more_information", instruction = "Ask the user for the employee code." };
        }

        var (employee, similar) = await _lookup.ResolveEmployeeAsync(code);
        if (employee == null)
        {
            return new
            {
                status = "employee_not_found",
                instruction = $"No employee has the code '{code}'. Ask the user which employee they mean.",
                similar = similar.Select(AiPayLookup.Summarise)
            };
        }

        var asOf = arguments.GetDate("asOfDate") ?? Clock.Now.Date;
        var quotas = (await _quotas.GetAll(new GetAllEmployeeAbsenceQuotasInput { EmployeeId = employee.Id, MaxResultCount = 1000 })).Items;
        var current = quotas.Where(q => q.EntitlementFrom.Date <= asOf && q.EntitlementTo.Date >= asOf).ToList();
        var arabicNames = await ArabicLeaveTypeNamesAsync();

        if (current.Count == 0)
        {
            return new
            {
                employee = AiPayLookup.Summarise(employee),
                asOfDate = asOf.ToString("yyyy-MM-dd"),
                note = quotas.Count == 0
                    ? "This employee has no leave entitlement set up. Tell the user."
                    : "No entitlement period covers that date. The most recent periods are listed.",
                balances = quotas.OrderByDescending(q => q.EntitlementTo).Take(5).Select(q => Row(q, arabicNames))
            };
        }

        return new
        {
            employee = AiPayLookup.Summarise(employee),
            asOfDate = asOf.ToString("yyyy-MM-dd"),
            balances = current.OrderBy(q => q.AbsenceTypeName).Select(q => Row(q, arabicNames))
        };
    }

    private static object Row(EmployeeAbsenceQuotaDto q, IReadOnlyDictionary<int, string> arabicNames) => new
    {
        leaveTypeEn = q.AbsenceTypeName,
        leaveTypeAr = arabicNames.TryGetValue(q.AbsenceTypeId, out var ar) ? ar : null,
        entitlementFrom = q.EntitlementFrom.ToString("yyyy-MM-dd"),
        entitlementTo = q.EntitlementTo.ToString("yyyy-MM-dd"),
        accrued = Math.Round(q.QuotaAccrued, 2),
        used = Math.Round(q.QuotaUsed, 2),
        adjusted = Math.Round(q.QuotaAdjusted, 2),
        forfeited = Math.Round(q.QuotaForfeited, 2),
        balance = q.QuotaBalance
    };

    /// <summary>
    /// Arabic names come from the self-service lookup, which an HR user without self-service access cannot read.
    /// That is not a reason to fail the whole answer -- the English names are enough.
    /// </summary>
    private async Task<IReadOnlyDictionary<int, string>> ArabicLeaveTypeNamesAsync()
    {
        try
        {
            return (await _essLookup.GetAbsenceTypes()).ToDictionary(t => t.Id, t => t.NameAr);
        }
        catch (Abp.Authorization.AbpAuthorizationException)
        {
            return new Dictionary<int, string>();
        }
    }
}
