using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.AiAgent.Actions;

/// <summary>
/// Applies a duty change an administrator confirmed: one substitute duty per day, through the Employee Substitute
/// Duties screen's service -- new days go through its approval routing, a day that already had an override has
/// that override's duty changed. All days in one unit of work: all of them or none.
/// </summary>
public class ChangeDutyAiPendingActionHandler : AiPendingActionHandlerBase
{
    public const string Type = "ChangeDuty";

    private readonly IEmployeeSubstituteDutiesAppService _substituteDuties;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ChangeDutyAiPendingActionHandler(IEmployeeSubstituteDutiesAppService substituteDuties, IUnitOfWorkManager unitOfWorkManager)
    {
        _substituteDuties = substituteDuties;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string ActionType => Type;

    public override AiPendingActionPreview Describe(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        var preview = new AiPendingActionPreview { Title = L("AiAgentActionChangeDuty") }
            .Add(L("AiAgentEmployee"), $"{payload.EmployeeCode} · {PickName(payload.EmployeeNameEn, payload.EmployeeNameAr)}")
            .Add(L("Duty"), $"{PickName(payload.DutyNameEn, payload.DutyNameAr)}{(payload.DutyNote == null ? "" : " (" + payload.DutyNote + ")")}")
            .Add(L("AiAgentFrom"), FormatDate(payload.From))
            .Add(L("AiAgentTo"), FormatDate(payload.To));

        foreach (var day in payload.Days.Where(d => d.ExistingDutyName != null))
        {
            preview.Add(FormatDate(day.Date), $"{day.ExistingDutyName} → {PickName(payload.DutyNameEn, payload.DutyNameAr)}");
        }

        return preview.Add(L("AiAgentDays"), payload.Days.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public override async Task<string> ExecuteAsync(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        using (var uow = _unitOfWorkManager.Begin())
        {
            var existing = await LoadOverridesAsync(_substituteDuties, payload.EmployeeId, payload.From, payload.To);
            if (Snapshot(existing) != payload.Snapshot)
            {
                throw new UserFriendlyException(L("AiAgentRecordChangedSinceDraft"));
            }

            foreach (var day in payload.Days)
            {
                await _substituteDuties.CreateOrEdit(new CreateOrEditEmployeeSubstituteDutyDto
                {
                    Id = day.ExistingOverrideId,
                    EmployeeId = payload.EmployeeId,
                    DutyId = payload.DutyId,
                    SubstituteDate = day.Date
                });
            }

            await uow.CompleteAsync();
        }

        return L("AiAgentDutyChanged", payload.EmployeeCode, payload.Days.Count);
    }

    /// <summary>
    /// The employee's overrides in the range. The screen's list has no date filter, so it is read newest first
    /// and stops once past the range.
    /// </summary>
    public static async Task<List<EmployeeSubstituteDutyDto>> LoadOverridesAsync(
        IEmployeeSubstituteDutiesAppService service, int employeeId, DateTime from, DateTime to)
    {
        const int pageSize = 200;
        var found = new List<EmployeeSubstituteDutyDto>();

        for (var skip = 0; ; skip += pageSize)
        {
            var page = (await service.GetAll(new GetAllEmployeeSubstituteDutiesInput
            {
                EmployeeId = employeeId,
                Sorting = "SubstituteDate DESC",
                SkipCount = skip,
                MaxResultCount = pageSize
            })).Items;

            found.AddRange(page.Where(p => p.SubstituteDate.Date >= from && p.SubstituteDate.Date <= to));

            if (page.Count < pageSize || page[^1].SubstituteDate.Date < from)
            {
                return found;
            }
        }
    }

    public static string Snapshot(IEnumerable<EmployeeSubstituteDutyDto> overrides) =>
        string.Join("|", overrides.OrderBy(o => o.Id).Select(o => $"{o.Id}:{o.SubstituteDate:yyyyMMdd}:{o.DutyId}"));

    public class Payload
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeNameEn { get; set; }

        public string EmployeeNameAr { get; set; }

        public int DutyId { get; set; }

        public string DutyNameEn { get; set; }

        public string DutyNameAr { get; set; }

        /// <summary>Hours or "weekly off", so the card shows what the duty means, not just its name.</summary>
        public string DutyNote { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        /// <summary>Only the days that change; a day already on this duty is left out.</summary>
        public List<Day> Days { get; set; } = new();

        public string Snapshot { get; set; }
    }

    public class Day
    {
        public DateTime Date { get; set; }

        public int? ExistingOverrideId { get; set; }

        public string ExistingDutyName { get; set; }
    }
}
