using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Actions;

/// <summary>
/// Applies a contract change an administrator confirmed, through the Employee Contracts screen's service, so its
/// overlap and date checks apply.
///
/// <para>
/// <b>A new contract is two saves in one unit of work:</b> end the current contract the day before, then add the
/// new one. The contracts service checks overlaps against the DATABASE and does not save by itself, so the
/// shortened contract is flushed before the new one is added -- otherwise the check still sees the old end date
/// and refuses. If the second save is refused, the first is rolled back with it.
/// </para>
/// </summary>
public class ChangeContractAiPendingActionHandler : AiPendingActionHandlerBase
{
    public const string Type = "ChangeContract";

    public static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly IEmployeeContractsAppService _contracts;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ChangeContractAiPendingActionHandler(IEmployeeContractsAppService contracts, IUnitOfWorkManager unitOfWorkManager)
    {
        _contracts = contracts;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string ActionType => Type;

    public override AiPendingActionPreview Describe(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        var preview = new AiPendingActionPreview
            {
                Title = L(payload.Mode == ChangeMode.NewContract ? "AiAgentActionNewContract" : "AiAgentActionChangeContractEnd")
            }
            .Add(L("AiAgentEmployee"), $"{payload.EmployeeCode} · {PickName(payload.EmployeeNameEn, payload.EmployeeNameAr)}")
            .Add(L("ContractType"), PickName(payload.ContractTypeName, payload.ContractTypeNameAr))
            .Add(L("AiAgentFrom"), FormatDate(payload.NewStartDate));

        if (payload.Mode == ChangeMode.ChangeEndDate)
        {
            preview.Add(L("AiAgentTo"), $"{FormatEnd(payload.SourceEndDate!.Value)} → {FormatEnd(payload.NewEndDate)}");
        }
        else
        {
            preview.Add(L("AiAgentTo"), FormatEnd(payload.NewEndDate));

            if (payload.EndSourceRecord)
            {
                preview.Add(L("AiAgentCurrentRecordEnds"), $"{payload.SourceContractTypeName} · {FormatDate(payload.NewStartDate.AddDays(-1))}");
            }
        }

        return preview;
    }

    public override async Task<string> ExecuteAsync(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        using (var uow = _unitOfWorkManager.Begin())
        {
            var records = (await _contracts.GetAll(new GetAllEmployeeContractsInput { EmployeeId = payload.EmployeeId, MaxResultCount = 1000 })).Items;
            var source = payload.SourceRecordId.HasValue ? records.FirstOrDefault(r => r.Id == payload.SourceRecordId.Value) : null;

            if (Snapshot(records) != payload.Snapshot || (payload.SourceRecordId.HasValue && source == null))
            {
                throw new UserFriendlyException(L("AiAgentRecordChangedSinceDraft"));
            }

            if (payload.Mode == ChangeMode.ChangeEndDate)
            {
                await SetEndDateAsync(source!.Id, payload.NewEndDate);
            }
            else
            {
                if (payload.EndSourceRecord)
                {
                    await SetEndDateAsync(source!.Id, payload.NewStartDate.AddDays(-1));
                    await _unitOfWorkManager.Current.SaveChangesAsync();
                }

                await _contracts.CreateOrEdit(new CreateOrEditEmployeeContractDto
                {
                    EmployeeId = payload.EmployeeId,
                    ContractTypeId = payload.ContractTypeId,
                    StartDate = payload.NewStartDate,
                    EndDate = payload.NewEndDate
                });
            }

            await uow.CompleteAsync();
        }

        return payload.Mode == ChangeMode.NewContract
            ? L("AiAgentContractCreated", payload.EmployeeCode, FormatDate(payload.NewStartDate))
            : L("AiAgentContractEndChanged", payload.EmployeeCode, FormatEnd(payload.NewEndDate));
    }

    /// <summary>
    /// Every contract of the employee, as drafted against. A change to any of them -- not only the current one --
    /// means the draft's overlap reasoning may no longer hold.
    /// </summary>
    public static string Snapshot(IEnumerable<EmployeeContractDto> records) =>
        string.Join("|", records.OrderBy(r => r.Id).Select(r => $"{r.Id}:{r.ContractTypeId}:{r.StartDate:yyyyMMdd}:{r.EndDate:yyyyMMdd}"));

    private async Task SetEndDateAsync(int contractId, DateTime endDate)
    {
        var edit = (await _contracts.GetEmployeeContractForEdit(new EntityDto(contractId))).EmployeeContract;
        edit.EndDate = endDate;
        await _contracts.CreateOrEdit(edit);
    }

    private string FormatEnd(DateTime date) => date.Date >= OpenEnded.Date ? L("AiAgentOpenEnded") : FormatDate(date);

    public enum ChangeMode
    {
        /// <summary>Move the current contract's end date.</summary>
        ChangeEndDate = 0,

        /// <summary>End the current contract the day before and add a new one.</summary>
        NewContract = 1
    }

    public class Payload
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeNameEn { get; set; }

        public string EmployeeNameAr { get; set; }

        public ChangeMode Mode { get; set; }

        public int ContractTypeId { get; set; }

        public string ContractTypeName { get; set; }

        public string ContractTypeNameAr { get; set; }

        /// <summary>The new contract's first day; for an end-date change, the current contract's start.</summary>
        public DateTime NewStartDate { get; set; }

        public DateTime NewEndDate { get; set; }

        /// <summary>The contract that was current when drafted; null when the employee had none.</summary>
        public int? SourceRecordId { get; set; }

        public DateTime? SourceStartDate { get; set; }

        public DateTime? SourceEndDate { get; set; }

        public string SourceContractTypeName { get; set; }

        /// <summary><see cref="Snapshot"/> when drafted.</summary>
        public string Snapshot { get; set; }

        /// <summary>A new contract starts while the current one is still running: end it the day before.</summary>
        public bool EndSourceRecord { get; set; }
    }
}
