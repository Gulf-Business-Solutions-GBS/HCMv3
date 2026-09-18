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
/// Drafts "move 1002 to Finance as Accountant from 1 October": a new organisational assignment from a date,
/// changing only what the administrator named and keeping everything else as it is. Never saves; the
/// administrator confirms the card.
/// </summary>
public class PrepareOrgAssignmentChangeAiAgentTool : IAiAgentTool
{
    private readonly AiPayLookup _lookup;
    private readonly AiMasterDataLookup _masterData;
    private readonly IEmployeeOrgAssignmentsAppService _assignments;
    private readonly IPermissionChecker _permissionChecker;
    private readonly AiPendingActionStore _pendingActionStore;

    public PrepareOrgAssignmentChangeAiAgentTool(
        AiPayLookup lookup,
        AiMasterDataLookup masterData,
        IEmployeeOrgAssignmentsAppService assignments,
        IPermissionChecker permissionChecker,
        AiPendingActionStore pendingActionStore)
    {
        _lookup = lookup;
        _masterData = masterData;
        _assignments = assignments;
        _permissionChecker = permissionChecker;
        _pendingActionStore = pendingActionStore;
    }

    public string Name => "prepare_org_assignment_change";

    public string Description =>
        "HR administration: prepare a transfer or job change from a date (organisation unit/department, job title, " +
        "cost center, work location), for the user to confirm. Does NOT save. Give only what changes, by name.";

    public string RequiredPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments_Create;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["employeeCode"] = new JsonObject { ["type"] = "string" },
            ["effectiveDate"] = new JsonObject { ["type"] = "string", ["description"] = "yyyy-MM-dd" },
            ["orgUnit"] = new JsonObject { ["type"] = "string" },
            ["jobTitle"] = new JsonObject { ["type"] = "string" },
            ["costCenter"] = new JsonObject { ["type"] = "string" },
            ["workLocation"] = new JsonObject { ["type"] = "string" }
        }
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var employeeCode = arguments.GetString("employeeCode");
        var effectiveDate = arguments.GetDate("effectiveDate");
        var requested = new List<(AiMasterDataKind Kind, string Text)>
            {
                (AiMasterDataKind.OrgUnit, arguments.GetString("orgUnit")),
                (AiMasterDataKind.JobTitle, arguments.GetString("jobTitle")),
                (AiMasterDataKind.CostCenter, arguments.GetString("costCenter")),
                (AiMasterDataKind.WorkLocation, arguments.GetString("workLocation"))
            }
            .Where(r => !string.IsNullOrWhiteSpace(r.Text))
            .ToList();

        var missing = new List<string>();
        if (employeeCode == null) missing.Add("the employee");
        if (!effectiveDate.HasValue) missing.Add("the date the change starts");
        if (requested.Count == 0) missing.Add("what changes (organisation unit, job title, cost center or work location)");

        if (missing.Count > 0)
        {
            return AiToolReplies.Ask("Nothing was prepared. Ask the user for: " + string.Join("; ", missing) + ".");
        }

        var lacking = await AiToolReplies.FirstMissingAsync(_permissionChecker,
            AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments_Create,
            AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments_Edit);
        if (lacking != null)
        {
            return AiToolReplies.NotPossible($"The user does not have the '{lacking}' permission needed to change organisational assignments. Nothing was prepared.");
        }

        var (employee, similar) = await _lookup.ResolveEmployeeAsync(employeeCode);
        if (employee == null)
        {
            return AiToolReplies.EmployeeNotFound(employeeCode, similar);
        }

        var effective = effectiveDate!.Value.Date;
        var records = (await _assignments.GetAll(new GetAllEmployeeOrgAssignmentsInput { EmployeeId = employee.Id, MaxResultCount = 1000 })).Items;

        var later = records.Where(r => r.StartDate.Date > effective).OrderBy(r => r.StartDate).FirstOrDefault();
        if (later != null)
        {
            return AiToolReplies.NotPossible($"Employee {employee.Code} already has an organisational assignment starting {later.StartDate:yyyy-MM-dd}, after " +
                                             $"{effective:yyyy-MM-dd}. That must be changed on the Org Assignments screen. Nothing was prepared.");
        }

        var source = records.Where(r => r.StartDate.Date <= effective).OrderByDescending(r => r.StartDate).FirstOrDefault();
        var before = source == null ? new ChangeOrgAssignmentAiPendingActionHandler.Assignment() : ChangeOrgAssignmentAiPendingActionHandler.Assignment.From(source);
        var after = before.Copy();

        foreach (var (kind, text) in requested)
        {
            var match = await _masterData.ResolveAsync(kind, text);
            if (match.Match == null)
            {
                return AiToolReplies.NotResolved(kind, text, match);
            }

            after.Set(kind, match.Match);
        }

        var changes = after.DifferencesFrom(before);
        if (changes.Count == 0)
        {
            return AiToolReplies.NotPossible($"Employee {employee.Code} already has exactly that assignment on {effective:yyyy-MM-dd}. Nothing was prepared. Tell the user.");
        }

        var payload = new ChangeOrgAssignmentAiPendingActionHandler.Payload
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeNameEn = employee.FullNameEn,
            EmployeeNameAr = employee.FullNameAr,
            EffectiveDate = effective,
            SourceRecordId = source?.Id,
            UpdatesRecordStartingSameDay = source != null && source.StartDate.Date == effective,
            Before = before,
            After = after,
            Snapshot = ChangeOrgAssignmentAiPendingActionHandler.Snapshot(records)
        };

        await _pendingActionStore.CreateAsync(context, ChangeOrgAssignmentAiPendingActionHandler.Type, payload);

        return new
        {
            status = "draft_ready",
            employee = AiPayLookup.Summarise(employee),
            effectiveDate = effective.ToString("yyyy-MM-dd"),
            changes = changes.Select(c => new { field = AiMasterDataLookup.Describe(c.Kind), before = c.Before, after = c.After }),
            instruction = "Summarise the change in one sentence and tell the user to press Confirm on the card below. It has NOT been saved yet."
        };
    }
}
