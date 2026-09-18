using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using HCMv3.AiAgent.Actions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// Drafts a change to an employee's contact and personal details -- mobile, email, address, emergency contact,
/// marital status. Never saves; the administrator confirms the card.
///
/// <para>
/// Only the fields in <see cref="AiEmployeeFields"/> can be changed. Names, Civil ID, nationality, dates of birth
/// and joining, and status are deliberately out of reach: a mistake in those follows the employee into payroll,
/// residency and every report, and they are rarely changed from a sentence.
/// </para>
/// </summary>
public class PreparePersonalDataChangeAiAgentTool : IAiAgentTool
{
    private static readonly EmailAddressAttribute EmailCheck = new();

    private readonly AiPayLookup _lookup;
    private readonly AiMasterDataLookup _masterData;
    private readonly IEmployeesAppService _employees;
    private readonly IPermissionChecker _permissionChecker;
    private readonly AiPendingActionStore _pendingActionStore;

    public PreparePersonalDataChangeAiAgentTool(
        AiPayLookup lookup,
        AiMasterDataLookup masterData,
        IEmployeesAppService employees,
        IPermissionChecker permissionChecker,
        AiPendingActionStore pendingActionStore)
    {
        _lookup = lookup;
        _masterData = masterData;
        _employees = employees;
        _permissionChecker = permissionChecker;
        _pendingActionStore = pendingActionStore;
    }

    public string Name => "prepare_personal_data_change";

    public string Description =>
        "HR administration: prepare a change to an employee's contact or personal details, for the user to confirm. " +
        "Does NOT save. Give only the fields that change. Names, Civil ID, nationality and dates cannot be changed here.";

    public string RequiredPermission => AppPermissions.Pages_PersonnelAdministration_Employees_Edit;

    public JsonObject BuildParameters()
    {
        var properties = new JsonObject { ["employeeCode"] = new JsonObject { ["type"] = "string" } };

        foreach (var field in AiEmployeeFields.All)
        {
            properties[field.Key] = new JsonObject { ["type"] = "string" };
        }

        return new JsonObject { ["type"] = "object", ["properties"] = properties };
    }

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var employeeCode = arguments.GetString("employeeCode");
        var requested = AiEmployeeFields.All
            .Select(f => (Field: f, Text: arguments.GetString(f.Key)))
            .Where(r => r.Text != null)
            .ToList();

        var missing = new List<string>();
        if (employeeCode == null) missing.Add("the employee");
        if (requested.Count == 0) missing.Add("which detail to change and its new value");

        if (missing.Count > 0)
        {
            return AiToolReplies.Ask("Nothing was prepared. Ask the user for: " + string.Join("; ", missing) + ".");
        }

        if (!await _permissionChecker.IsGrantedAsync(AppPermissions.Pages_PersonnelAdministration_Employees_Edit))
        {
            return AiToolReplies.NotPossible($"The user does not have the '{AppPermissions.Pages_PersonnelAdministration_Employees_Edit}' permission needed to change employee details. Nothing was prepared.");
        }

        var (employee, similar) = await _lookup.ResolveEmployeeAsync(employeeCode);
        if (employee == null)
        {
            return AiToolReplies.EmployeeNotFound(employeeCode, similar);
        }

        var current = (await _employees.GetEmployeeForEdit(new EntityDto(employee.Id))).Employee;
        var changes = new List<ChangePersonalDataAiPendingActionHandler.Change>();

        foreach (var (field, text) in requested)
        {
            var value = text.Trim();
            string display = value;

            if (field.IsPhoneNumber)
            {
                value = AiEmployeeFields.NormalisePhone(text);
                if (value == null)
                {
                    return AiToolReplies.Ask($"'{text}' is not a phone number (5 to 15 digits). Nothing was prepared. Ask the user for the {field.Key} number.");
                }

                display = value;
            }
            else if ((field.Key is "email" or "alternateEmail") && !EmailCheck.IsValid(value))
            {
                return AiToolReplies.Ask($"'{text}' is not an email address. Nothing was prepared. Ask the user for the correct address.");
            }
            else if (field.Lookup.HasValue)
            {
                var match = await _masterData.ResolveAsync(field.Lookup.Value, text);
                if (match.Match == null)
                {
                    return AiToolReplies.NotResolved(field.Lookup.Value, text, match);
                }

                value = match.Match.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                display = match.Match.NameEn;
            }

            var before = field.Get(current);
            if (before == value || (string.IsNullOrEmpty(before) && string.IsNullOrEmpty(value)))
            {
                continue;
            }

            changes.Add(new ChangePersonalDataAiPendingActionHandler.Change
            {
                Key = field.Key,
                Before = before,
                BeforeDisplay = field.Lookup.HasValue ? null : before,
                After = value,
                AfterDisplay = display
            });
        }

        if (changes.Count == 0)
        {
            return AiToolReplies.NotPossible($"Employee {employee.Code} already has those details. Nothing was prepared. Tell the user.");
        }

        await _pendingActionStore.CreateAsync(context, ChangePersonalDataAiPendingActionHandler.Type, new ChangePersonalDataAiPendingActionHandler.Payload
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeNameEn = employee.FullNameEn,
            EmployeeNameAr = employee.FullNameAr,
            Changes = changes
        });

        return new
        {
            status = "draft_ready",
            employee = AiPayLookup.Summarise(employee),
            changes = changes.Select(c => new { field = c.Key, before = c.BeforeDisplay, after = c.AfterDisplay }),
            instruction = "Summarise the change in one sentence and tell the user to press Confirm on the card below. It has NOT been saved yet."
        };
    }
}
