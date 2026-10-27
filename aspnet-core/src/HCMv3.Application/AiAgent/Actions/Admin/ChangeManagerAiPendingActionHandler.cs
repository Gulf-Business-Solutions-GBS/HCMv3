using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.AiAgent.Tools;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Actions;

/// <summary>
/// Applies a manager change an administrator confirmed, through the Employee Managers screen's service: a new
/// record from the effective date (the service ends the previous one the day before), or -- when a record
/// already starts that very day -- that record's manager changed in place.
///
/// <para>
/// Refuses if the employee's manager records changed between drafting and confirming, rather than apply a
/// change worked out against data that is no longer there.
/// </para>
/// </summary>
public class ChangeManagerAiPendingActionHandler : AiPendingActionHandlerBase
{
    public const string Type = "ChangeManager";

    private readonly IEmployeeManagersAppService _managers;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ChangeManagerAiPendingActionHandler(IEmployeeManagersAppService managers, IUnitOfWorkManager unitOfWorkManager)
    {
        _managers = managers;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string ActionType => Type;

    public override AiPendingActionPreview Describe(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        var preview = new AiPendingActionPreview { Title = L("AiAgentActionChangeManager") }
            .Add(L("AiAgentEmployee"), $"{payload.EmployeeCode} · {PickName(payload.EmployeeNameEn, payload.EmployeeNameAr)}")
            .Add(L("AiAgentEffectiveFrom"), FormatDate(payload.EffectiveDate))
            .Add(L("Manager"), $"{(payload.SourceManagerCode == null ? "—" : payload.SourceManagerCode + " · " + payload.SourceManagerNameEn)} → " +
                                      $"{payload.ManagerCode} · {PickName(payload.ManagerNameEn, payload.ManagerNameAr)}");

        return payload.UpdatesRecordStartingSameDay
            ? preview.Add(L("AiAgentUpdatesExistingRecord"), FormatDate(payload.EffectiveDate))
            : payload.SourceRecordId.HasValue
                ? preview.Add(L("AiAgentCurrentRecordEnds"), FormatDate(payload.EffectiveDate.AddDays(-1)))
                : preview;
    }

    public override async Task<string> ExecuteAsync(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        using (var uow = _unitOfWorkManager.Begin())
        {
            var records = (await _managers.GetAll(new GetAllEmployeeManagersInput { EmployeeId = payload.EmployeeId, MaxResultCount = 1000 })).Items;
            var inEffect = PrepareManagerChangeAiAgentTool.InEffect(records, payload.EffectiveDate);

            if (records.Any(r => r.StartDate.Date > payload.EffectiveDate)
                || inEffect?.Id != payload.SourceRecordId
                || inEffect?.ManagerId != payload.SourceManagerId)
            {
                throw ChangedSinceDraft();
            }

            if (payload.UpdatesRecordStartingSameDay)
            {
                var edit = (await _managers.GetEmployeeManagerForEdit(new EntityDto(inEffect!.Id))).EmployeeManager;
                edit.ManagerId = payload.ManagerId;
                await _managers.CreateOrEdit(edit);
            }
            else
            {
                await _managers.CreateOrEdit(new CreateOrEditEmployeeManagerDto
                {
                    EmployeeId = payload.EmployeeId,
                    ManagerId = payload.ManagerId,
                    StartDate = payload.EffectiveDate
                });
            }

            await uow.CompleteAsync();
        }

        return L("AiAgentManagerChanged", payload.EmployeeCode, payload.ManagerCode, FormatDate(payload.EffectiveDate));
    }

    private UserFriendlyException ChangedSinceDraft() => new(L("AiAgentRecordChangedSinceDraft"));

    public class Payload
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeNameEn { get; set; }

        public string EmployeeNameAr { get; set; }

        public int ManagerId { get; set; }

        public string ManagerCode { get; set; }

        public string ManagerNameEn { get; set; }

        public string ManagerNameAr { get; set; }

        public DateTime EffectiveDate { get; set; }

        /// <summary>The record in effect on the effective date when drafted; null when there was none.</summary>
        public int? SourceRecordId { get; set; }

        public int? SourceManagerId { get; set; }

        public string SourceManagerCode { get; set; }

        public string SourceManagerNameEn { get; set; }

        public bool UpdatesRecordStartingSameDay { get; set; }
    }
}
