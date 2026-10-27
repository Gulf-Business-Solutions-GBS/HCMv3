using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.AiAgent.Tools;
using HCMv3.PersonnelAdministration;

namespace HCMv3.AiAgent.Actions;

/// <summary>
/// Applies a personal data change an administrator confirmed, through the Employees screen's service.
///
/// <para>
/// <b>Load, change, save the whole record.</b> That service's save overwrites EVERY field with what it is sent,
/// nulls included. So the record is read fresh on Confirm, only the drafted fields are changed on it, and all of
/// it is sent back -- the same as a person editing one field on the screen. The screen's checks (unique Civil ID,
/// email format, business rules) run as they would there.
/// </para>
///
/// <para>
/// Refuses if any drafted field was edited by someone else between drafting and confirming.
/// </para>
/// </summary>
public class ChangePersonalDataAiPendingActionHandler : AiPendingActionHandlerBase
{
    public const string Type = "ChangePersonalData";

    private readonly IEmployeesAppService _employees;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ChangePersonalDataAiPendingActionHandler(IEmployeesAppService employees, IUnitOfWorkManager unitOfWorkManager)
    {
        _employees = employees;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string ActionType => Type;

    public override AiPendingActionPreview Describe(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        var preview = new AiPendingActionPreview { Title = L("AiAgentActionChangePersonalData") }
            .Add(L("AiAgentEmployee"), $"{payload.EmployeeCode} · {PickName(payload.EmployeeNameEn, payload.EmployeeNameAr)}");

        foreach (var change in payload.Changes)
        {
            var field = AiEmployeeFields.Find(change.Key);
            var value = field?.Lookup != null || change.BeforeDisplay == null && change.Before != null
                ? change.AfterDisplay
                : $"{change.BeforeDisplay ?? "—"} → {change.AfterDisplay}";

            preview.Add(field == null ? change.Key : L(field.LabelKey), value);
        }

        return preview;
    }

    public override async Task<string> ExecuteAsync(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        using (var uow = _unitOfWorkManager.Begin())
        {
            var edit = (await _employees.GetEmployeeForEdit(new EntityDto(payload.EmployeeId))).Employee;

            foreach (var change in payload.Changes)
            {
                var field = AiEmployeeFields.Find(change.Key) ?? throw new UserFriendlyException(L("AiAgentRecordChangedSinceDraft"));
                var now = field.Get(edit);

                if (now != change.Before && !(string.IsNullOrEmpty(now) && string.IsNullOrEmpty(change.Before)))
                {
                    throw new UserFriendlyException(L("AiAgentRecordChangedSinceDraft"));
                }

                field.Set(edit, change.After);
            }

            await _employees.CreateOrEdit(edit);
            await uow.CompleteAsync();
        }

        return L("AiAgentPersonalDataChanged", payload.EmployeeCode);
    }

    public class Payload
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeNameEn { get; set; }

        public string EmployeeNameAr { get; set; }

        public List<Change> Changes { get; set; } = new();
    }

    public class Change
    {
        /// <summary>An <see cref="AiEmployeeFields.Field.Key"/>.</summary>
        public string Key { get; set; }

        /// <summary>The stored value when drafted, as <see cref="AiEmployeeFields.Field.Get"/> gives it.</summary>
        public string Before { get; set; }

        public string BeforeDisplay { get; set; }

        public string After { get; set; }

        public string AfterDisplay { get; set; }
    }
}
