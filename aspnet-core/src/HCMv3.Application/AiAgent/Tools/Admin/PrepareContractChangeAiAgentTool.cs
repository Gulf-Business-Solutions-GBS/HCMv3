using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Timing;
using HCMv3.AiAgent.Actions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// Drafts a contract change for the administrator to confirm. Two kinds:
/// <list type="bullet">
/// <item><description><b>Change the end date</b> of the current contract -- "extend 1002's contract to
/// 31/12/2027", "make it open-ended".</description></item>
/// <item><description><b>A new contract</b> from a date -- "renew 1002 as Permanent from 1 January". The current
/// contract is ended the day before.</description></item>
/// </list>
///
/// <para>
/// <b>Why ending the old contract is done here.</b> Unlike managers and org assignments, the Employee Contracts
/// service does not end the previous record for you -- it refuses overlapping contracts instead. So a renewal is
/// two saves, done together on Confirm or not at all.
/// </para>
/// </summary>
public class PrepareContractChangeAiAgentTool : IAiAgentTool
{
    private readonly AiPayLookup _lookup;
    private readonly AiMasterDataLookup _masterData;
    private readonly IEmployeeContractsAppService _contracts;
    private readonly IPermissionChecker _permissionChecker;
    private readonly AiPendingActionStore _pendingActionStore;

    public PrepareContractChangeAiAgentTool(
        AiPayLookup lookup,
        AiMasterDataLookup masterData,
        IEmployeeContractsAppService contracts,
        IPermissionChecker permissionChecker,
        AiPendingActionStore pendingActionStore)
    {
        _lookup = lookup;
        _masterData = masterData;
        _contracts = contracts;
        _permissionChecker = permissionChecker;
        _pendingActionStore = pendingActionStore;
    }

    public string Name => "prepare_contract_change";

    public string Description =>
        "HR administration: prepare a contract change for the user to confirm. Does NOT save. Without startDate it " +
        "changes the current contract's end date (extend). With startDate it starts a new contract then, ending the " +
        "current one the day before (renew or change contract type).";

    public string RequiredPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeContracts_Edit;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["employeeCode"] = new JsonObject { ["type"] = "string" },
            ["endDate"] = new JsonObject { ["type"] = "string", ["description"] = "New last day of the contract, yyyy-MM-dd." },
            ["openEnded"] = new JsonObject { ["type"] = "boolean", ["description"] = "True for a contract with no end date." },
            ["startDate"] = new JsonObject { ["type"] = "string", ["description"] = "Only for a NEW contract: its first day, yyyy-MM-dd." },
            ["contractType"] = new JsonObject { ["type"] = "string", ["description"] = "Only for a new contract: type name, e.g. Permanent." }
        }
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var employeeCode = arguments.GetString("employeeCode");
        var endDate = arguments.GetDate("endDate");
        var openEnded = arguments.GetBool("openEnded") == true;
        var startDate = arguments.GetDate("startDate");
        var contractTypeText = arguments.GetString("contractType");

        var missing = new List<string>();
        if (employeeCode == null) missing.Add("the employee");
        if (!endDate.HasValue && !openEnded) missing.Add("the new end date (or whether the contract is open-ended)");

        if (missing.Count > 0)
        {
            return AiToolReplies.Ask("Nothing was prepared. Ask the user for: " + string.Join("; ", missing) + ".");
        }

        var lacking = await AiToolReplies.FirstMissingAsync(_permissionChecker,
            AppPermissions.Pages_PersonnelAdministration_EmployeeContracts_Edit,
            AppPermissions.Pages_PersonnelAdministration_EmployeeContracts_Create);
        if (lacking != null)
        {
            return AiToolReplies.NotPossible($"The user does not have the '{lacking}' permission needed to change contracts. Nothing was prepared.");
        }

        var (employee, similar) = await _lookup.ResolveEmployeeAsync(employeeCode);
        if (employee == null)
        {
            return AiToolReplies.EmployeeNotFound(employeeCode, similar);
        }

        var records = (await _contracts.GetAll(new GetAllEmployeeContractsInput { EmployeeId = employee.Id, MaxResultCount = 1000 })).Items
            .OrderBy(r => r.StartDate).ToList();
        var newEnd = openEnded ? ChangeContractAiPendingActionHandler.OpenEnded : endDate!.Value.Date;

        AiNamedRecord contractType = null;
        if (contractTypeText != null)
        {
            var match = await _masterData.ResolveAsync(AiMasterDataKind.ContractType, contractTypeText);
            if (match.Match == null)
            {
                return AiToolReplies.NotResolved(AiMasterDataKind.ContractType, contractTypeText, match);
            }

            contractType = match.Match;
        }

        var today = Clock.Now.Date;
        var current = InEffect(records, startDate?.Date ?? today) ?? records.LastOrDefault(r => r.StartDate.Date <= (startDate?.Date ?? today));

        var payload = new ChangeContractAiPendingActionHandler.Payload
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeNameEn = employee.FullNameEn,
            EmployeeNameAr = employee.FullNameAr,
            NewEndDate = newEnd,
            SourceRecordId = current?.Id,
            SourceStartDate = current?.StartDate.Date,
            SourceEndDate = current?.EndDate.Date,
            SourceContractTypeName = current?.ContractTypeName,
            Snapshot = ChangeContractAiPendingActionHandler.Snapshot(records)
        };

        if (!startDate.HasValue)
        {
            if (contractType != null && contractType.Id != current?.ContractTypeId)
            {
                return AiToolReplies.Ask($"Changing the contract type to {contractType.NameEn} starts a new contract. Nothing was prepared. Ask the user from which date.");
            }

            if (current == null)
            {
                return AiToolReplies.Ask($"Employee {employee.Code} has no contract to extend. Nothing was prepared. Ask the user for the contract type and start date of a new contract.");
            }

            if (current.EndDate.Date == newEnd)
            {
                return AiToolReplies.NotPossible($"The contract already ends on {newEnd:yyyy-MM-dd}. Nothing was prepared. Tell the user.");
            }

            if (newEnd < current.StartDate.Date)
            {
                return AiToolReplies.NotPossible($"The contract starts {current.StartDate:yyyy-MM-dd}; it cannot end before that. Nothing was prepared.");
            }

            var next = records.FirstOrDefault(r => r.StartDate.Date > current.StartDate.Date);
            if (next != null && next.StartDate.Date <= newEnd)
            {
                return AiToolReplies.NotPossible($"Employee {employee.Code} has a later contract starting {next.StartDate:yyyy-MM-dd}, so this one cannot run to {newEnd:yyyy-MM-dd}. " +
                                                 "Nothing was prepared. That must be sorted out on the Employee Contracts screen.");
            }

            payload.Mode = ChangeContractAiPendingActionHandler.ChangeMode.ChangeEndDate;
            payload.ContractTypeId = current.ContractTypeId;
            payload.ContractTypeName = current.ContractTypeName;
            payload.NewStartDate = current.StartDate.Date;
        }
        else
        {
            var start = startDate.Value.Date;

            if (newEnd < start)
            {
                return AiToolReplies.NotPossible($"The new contract cannot end ({newEnd:yyyy-MM-dd}) before it starts ({start:yyyy-MM-dd}). Nothing was prepared.");
            }

            var later = records.FirstOrDefault(r => r.StartDate.Date >= start);
            if (later != null)
            {
                return AiToolReplies.NotPossible($"Employee {employee.Code} already has a contract starting {later.StartDate:yyyy-MM-dd}, on or after {start:yyyy-MM-dd}. " +
                                                 "Nothing was prepared. That must be changed on the Employee Contracts screen.");
            }

            if (contractType == null && current == null)
            {
                return AiToolReplies.Ask($"Employee {employee.Code} has no earlier contract to take the type from. Nothing was prepared. Ask the user for the contract type.");
            }

            payload.Mode = ChangeContractAiPendingActionHandler.ChangeMode.NewContract;
            payload.ContractTypeId = contractType != null ? (int)contractType.Id : current!.ContractTypeId;
            payload.ContractTypeName = contractType?.NameEn ?? current!.ContractTypeName;
            payload.ContractTypeNameAr = contractType?.NameAr;
            payload.NewStartDate = start;
            payload.EndSourceRecord = current != null && current.EndDate.Date >= start;
        }

        await _pendingActionStore.CreateAsync(context, ChangeContractAiPendingActionHandler.Type, payload);

        return new
        {
            status = "draft_ready",
            employee = AiPayLookup.Summarise(employee),
            change = payload.Mode == ChangeContractAiPendingActionHandler.ChangeMode.ChangeEndDate ? "end_date_changed" : "new_contract",
            contractType = payload.ContractTypeName,
            from = payload.NewStartDate.ToString("yyyy-MM-dd"),
            to = openEnded ? "open-ended" : newEnd.ToString("yyyy-MM-dd"),
            currentContractEnds = payload.EndSourceRecord ? payload.NewStartDate.AddDays(-1).ToString("yyyy-MM-dd") : null,
            instruction = "Summarise the change in one sentence and tell the user to press Confirm on the card below. It has NOT been saved yet."
        };
    }

    public static EmployeeContractDto InEffect(IEnumerable<EmployeeContractDto> records, DateTime date) =>
        records.Where(r => r.StartDate.Date <= date && r.EndDate.Date >= date).OrderByDescending(r => r.StartDate).FirstOrDefault();
}
