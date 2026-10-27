using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Abp.Authorization;
using HCMv3.AiAgent.Actions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// Drafts "change 1002's manager to 1005 from 1 October". Never saves; the administrator confirms the card, and the
/// Employee Managers screen's own service does the save -- it ends the previous manager's record the day before.
///
/// <para>
/// Two checks the screen does not make, because a reporting line built from a sentence is easier to get wrong
/// than one picked from a list: an employee cannot manage themselves, and a manager cannot end up reporting,
/// however indirectly, to the person they manage. Either would send approvals round in a circle.
/// </para>
/// </summary>
public class PrepareManagerChangeAiAgentTool : IAiAgentTool
{
    /// <summary>Deeper than any real organisation; only here so bad data cannot loop forever.</summary>
    private const int MaxChainDepth = 50;

    private readonly AiPayLookup _lookup;
    private readonly IEmployeeManagersAppService _managers;
    private readonly IPermissionChecker _permissionChecker;
    private readonly AiPendingActionStore _pendingActionStore;

    public PrepareManagerChangeAiAgentTool(
        AiPayLookup lookup,
        IEmployeeManagersAppService managers,
        IPermissionChecker permissionChecker,
        AiPendingActionStore pendingActionStore)
    {
        _lookup = lookup;
        _managers = managers;
        _permissionChecker = permissionChecker;
        _pendingActionStore = pendingActionStore;
    }

    public string Name => "prepare_manager_change";

    public string Description =>
        "HR administration: prepare a change of an employee's line manager from a date, for the user to confirm. " +
        "Does NOT save. The previous manager's record ends the day before.";

    public string RequiredPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeManagers_Create;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["employeeCode"] = new JsonObject { ["type"] = "string", ["description"] = "Code of the employee whose manager changes." },
            ["managerCode"] = new JsonObject { ["type"] = "string", ["description"] = "Employee code of the new manager." },
            ["effectiveDate"] = new JsonObject { ["type"] = "string", ["description"] = "First day under the new manager, yyyy-MM-dd." }
        }
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var employeeCode = arguments.GetString("employeeCode");
        var managerCode = arguments.GetString("managerCode");
        var effectiveDate = arguments.GetDate("effectiveDate");

        var missing = new List<string>();
        if (employeeCode == null) missing.Add("the employee");
        if (managerCode == null) missing.Add("the new manager");
        if (!effectiveDate.HasValue) missing.Add("the date the change starts");

        if (missing.Count > 0)
        {
            return AiToolReplies.Ask("Nothing was prepared. Ask the user for: " + string.Join("; ", missing) + ".");
        }

        var lacking = await AiToolReplies.FirstMissingAsync(_permissionChecker,
            AppPermissions.Pages_PersonnelAdministration_EmployeeManagers_Create,
            AppPermissions.Pages_PersonnelAdministration_EmployeeManagers_Edit);
        if (lacking != null)
        {
            return AiToolReplies.NotPossible($"The user does not have the '{lacking}' permission needed to change managers. Nothing was prepared.");
        }

        var (employee, similarEmployees) = await _lookup.ResolveEmployeeAsync(employeeCode);
        if (employee == null)
        {
            return AiToolReplies.EmployeeNotFound(employeeCode, similarEmployees);
        }

        var (manager, similarManagers) = await _lookup.ResolveEmployeeAsync(managerCode);
        if (manager == null)
        {
            return AiToolReplies.EmployeeNotFound(managerCode, similarManagers);
        }

        if (manager.Id == employee.Id)
        {
            return AiToolReplies.NotPossible($"An employee cannot be their own manager ({employee.Code}). Nothing was prepared. Ask the user who the manager should be.");
        }

        if (manager.Status != EmployeeStatus.Active)
        {
            return AiToolReplies.NotPossible($"{manager.Code} {manager.FullNameEn} is not an active employee, so cannot be a manager. Nothing was prepared.");
        }

        var effective = effectiveDate!.Value.Date;
        var records = await LoadAsync(employee.Id);

        var later = records.Where(r => r.StartDate.Date > effective).OrderBy(r => r.StartDate).FirstOrDefault();
        if (later != null)
        {
            return AiToolReplies.NotPossible($"Employee {employee.Code} already has a manager record starting {later.StartDate:yyyy-MM-dd}, after " +
                                             $"{effective:yyyy-MM-dd}. That must be changed on the Employee Managers screen. Nothing was prepared.");
        }

        var inEffect = InEffect(records, effective);
        if (inEffect?.ManagerId == manager.Id)
        {
            return AiToolReplies.NotPossible($"{manager.Code} {manager.FullNameEn} is already {employee.Code}'s manager on {effective:yyyy-MM-dd}. Nothing was prepared. Tell the user.");
        }

        if (await ReportsToAsync(manager.Id, employee.Id, effective))
        {
            return AiToolReplies.NotPossible($"{manager.Code} {manager.FullNameEn} reports, directly or through others, to {employee.Code} {employee.FullNameEn}. " +
                                             "Making them the manager would create a loop. Nothing was prepared.");
        }

        var payload = new ChangeManagerAiPendingActionHandler.Payload
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeNameEn = employee.FullNameEn,
            EmployeeNameAr = employee.FullNameAr,
            ManagerId = manager.Id,
            ManagerCode = manager.Code,
            ManagerNameEn = manager.FullNameEn,
            ManagerNameAr = manager.FullNameAr,
            EffectiveDate = effective,
            SourceRecordId = inEffect?.Id,
            SourceManagerId = inEffect?.ManagerId,
            SourceManagerCode = inEffect?.ManagerCode,
            SourceManagerNameEn = inEffect?.ManagerFullNameEn,
            UpdatesRecordStartingSameDay = inEffect != null && inEffect.StartDate.Date == effective
        };

        await _pendingActionStore.CreateAsync(context, ChangeManagerAiPendingActionHandler.Type, payload);

        return new
        {
            status = "draft_ready",
            employee = AiPayLookup.Summarise(employee),
            currentManager = inEffect == null ? null : new { code = inEffect.ManagerCode, nameEn = inEffect.ManagerFullNameEn },
            newManager = AiPayLookup.Summarise(manager),
            effectiveDate = effective.ToString("yyyy-MM-dd"),
            instruction = "Summarise the change in one sentence and tell the user to press Confirm on the card below. It has NOT been saved yet."
        };
    }

    private async Task<IReadOnlyList<EmployeeManagerDto>> LoadAsync(int employeeId) =>
        (await _managers.GetAll(new GetAllEmployeeManagersInput { EmployeeId = employeeId, MaxResultCount = 1000 })).Items;

    public static EmployeeManagerDto InEffect(IEnumerable<EmployeeManagerDto> records, DateTime date) =>
        records.Where(r => r.StartDate.Date <= date && r.EndDate.Date >= date).OrderByDescending(r => r.StartDate).FirstOrDefault();

    /// <summary>Whether <paramref name="employeeId"/> is anywhere above <paramref name="managerId"/> on that date.</summary>
    private async Task<bool> ReportsToAsync(int managerId, int employeeId, DateTime date)
    {
        var seen = new HashSet<int> { managerId };
        var current = managerId;

        for (var depth = 0; depth < MaxChainDepth; depth++)
        {
            var above = InEffect(await LoadAsync(current), date)?.ManagerId;
            if (above == null)
            {
                return false;
            }

            if (above.Value == employeeId)
            {
                return true;
            }

            if (!seen.Add(above.Value))
            {
                return false; // an existing loop elsewhere; not this change's to report
            }

            current = above.Value;
        }

        return false;
    }
}
