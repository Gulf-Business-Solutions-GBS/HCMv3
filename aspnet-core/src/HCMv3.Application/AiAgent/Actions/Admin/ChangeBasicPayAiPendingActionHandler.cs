using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.AiAgent.Tools;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Actions;

/// <summary>
/// Applies a basic pay change an administrator confirmed: closes the record in effect the day before the change,
/// opens a new one from the effective date carrying every existing line, with the change applied.
///
/// <para>
/// <b>Why the old record is closed here.</b> The Basic Pay screen does not do it, and the payroll engine picks
/// the record for a pay date with an unordered <c>FirstOrDefault</c> over overlapping records -- so leaving the old
/// record open would make which salary gets paid a matter of chance. Chosen explicitly by the business owner.
/// </para>
///
/// <para>
/// <b>All or nothing.</b> Closing, creating and every line run in ONE unit of work through the admin services
/// (so their permissions and duplicate checks apply). If any step is refused, none of it happens -- there is no
/// state where the old record was closed and the new one does not exist.
/// </para>
///
/// <para>
/// <b>Nothing is applied against changed data.</b> The draft recorded the lines it was based on. If the
/// employee's basic pay was edited between drafting and confirming, this refuses rather than overwrite that
/// edit with a copy of the old lines.
/// </para>
/// </summary>
public class ChangeBasicPayAiPendingActionHandler : AiPendingActionHandlerBase
{
    public const string Type = "ChangeBasicPay";

    public static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly IEmployeeBasicPaysAppService _basicPays;
    private readonly IEmployeeBasicPayDetailsAppService _basicPayDetails;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ChangeBasicPayAiPendingActionHandler(
        IEmployeeBasicPaysAppService basicPays,
        IEmployeeBasicPayDetailsAppService basicPayDetails,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _basicPays = basicPays;
        _basicPayDetails = basicPayDetails;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string ActionType => Type;

    public override AiPendingActionPreview Describe(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        var preview = new AiPendingActionPreview { Title = L("AiAgentActionChangeBasicPay") }
            .Add(L("AiAgentEmployee"), $"{payload.EmployeeCode} · {PickName(payload.EmployeeNameEn, payload.EmployeeNameAr)}")
            .Add(L("AiAgentEffectiveFrom"), FormatDate(payload.EffectiveDate));

        if (payload.Mode == ChangeMode.UpdateRecord)
        {
            preview.Add(L("AiAgentUpdatesExistingRecord"), FormatDate(payload.EffectiveDate));
        }
        else if (payload.CloseSourceRecord && payload.SourceStartDate.HasValue)
        {
            preview.Add(L("AiAgentCurrentRecordEnds"), FormatDate(payload.EffectiveDate.AddDays(-1)));
        }

        foreach (var line in UnionOfLines(payload))
        {
            preview.Add(PickName(line.NameEn, line.NameAr), DescribeLine(line.Before, line.After));
        }

        return preview.Add(L("AiAgentTotal"), $"{AiPayLookup.FormatAmount(payload.Before.Sum(l => l.Amount))} → {AiPayLookup.FormatAmount(payload.After.Sum(l => l.Amount))}");
    }

    public override async Task<string> ExecuteAsync(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        using (var uow = _unitOfWorkManager.Begin())
        {
            var records = (await _basicPays.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = payload.EmployeeId, MaxResultCount = 1000 })).Items;

            if (records.Any(r => r.StartDate.Date > payload.EffectiveDate))
            {
                throw ChangedSinceDraft();
            }

            switch (payload.Mode)
            {
                case ChangeMode.UpdateRecord:
                    await UpdateRecordAsync(payload, records);
                    break;

                case ChangeMode.NewRecord:
                    await OpenNewRecordAsync(payload, records);
                    break;

                default:
                    throw new InvalidOperationException("Unknown basic pay change mode " + payload.Mode);
            }

            await uow.CompleteAsync();
        }

        return L("AiAgentBasicPayChanged", payload.EmployeeCode, FormatDate(payload.EffectiveDate));
    }

    private async Task UpdateRecordAsync(Payload payload, IReadOnlyList<EmployeeBasicPayDto> records)
    {
        var record = records.FirstOrDefault(r => r.Id == payload.SourceBasicPayId && r.StartDate.Date == payload.EffectiveDate)
                     ?? throw ChangedSinceDraft();

        var current = await LoadLinesAsync(record.Id);
        EnsureUnchanged(payload, current);

        foreach (var line in payload.After)
        {
            var existing = current.FirstOrDefault(c => c.WageTypeId == line.WageTypeId);

            if (existing == null)
            {
                await _basicPayDetails.CreateOrEdit(ToDetail(line, record.Id));
            }
            else if (Math.Round(existing.Amount, 3) != Math.Round(line.Amount, 3))
            {
                var edit = ToDetail(line, record.Id);
                edit.Id = existing.Id;
                // Keep what the draft did not touch exactly as it is on the row.
                edit.CurrencyId = existing.CurrencyId;
                edit.DependantNumber = existing.DependantNumber;
                edit.DependantUnit = existing.DependantUnit;
                edit.DependantWageTypeId = existing.DependantWageTypeId;
                await _basicPayDetails.CreateOrEdit(edit);
            }
        }

        foreach (var removed in current.Where(c => payload.After.All(a => a.WageTypeId != c.WageTypeId)))
        {
            await _basicPayDetails.Delete(new EntityDto(removed.Id));
        }
    }

    private async Task OpenNewRecordAsync(Payload payload, IReadOnlyList<EmployeeBasicPayDto> records)
    {
        if (records.Any(r => r.StartDate.Date == payload.EffectiveDate))
        {
            throw ChangedSinceDraft();
        }

        var newEndDate = OpenEnded;

        if (payload.SourceBasicPayId.HasValue)
        {
            var source = records.FirstOrDefault(r => r.Id == payload.SourceBasicPayId.Value
                                                     && r.StartDate.Date == payload.SourceStartDate
                                                     && r.EndDate.Date == payload.SourceEndDate)
                         ?? throw ChangedSinceDraft();

            EnsureUnchanged(payload, await LoadLinesAsync(source.Id));

            if (payload.CloseSourceRecord)
            {
                var edit = (await _basicPays.GetEmployeeBasicPayForEdit(new EntityDto(source.Id))).EmployeeBasicPay;
                edit.EndDate = payload.EffectiveDate.AddDays(-1);
                await _basicPays.CreateOrEdit(edit);

                // The new record takes over the old one's end, so a record that was itself due to end keeps doing so.
                newEndDate = source.EndDate.Date;
            }
        }

        await _basicPays.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
        {
            EmployeeId = payload.EmployeeId,
            StartDate = payload.EffectiveDate,
            EndDate = newEndDate,
            PayType = payload.PayType,
            PayInterval = payload.PayInterval
        });

        // CreateOrEdit returns no id; StartDate is unique per employee, so read the new record back by it.
        await _unitOfWorkManager.Current.SaveChangesAsync();
        var created = (await _basicPays.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = payload.EmployeeId, MaxResultCount = 1000 }))
            .Items.Single(r => r.StartDate.Date == payload.EffectiveDate);

        foreach (var line in payload.After)
        {
            await _basicPayDetails.CreateOrEdit(ToDetail(line, created.Id));
        }
    }

    private async Task<List<EmployeeBasicPayDetailDto>> LoadLinesAsync(int basicPayId) =>
        (await _basicPayDetails.GetAll(new GetAllEmployeeBasicPayDetailsInput { EmployeeBasicPayId = basicPayId, MaxResultCount = 1000 }))
        .Items.ToList();

    private void EnsureUnchanged(Payload payload, List<EmployeeBasicPayDetailDto> current)
    {
        static string Signature(IEnumerable<(int WageTypeId, double Amount)> lines) =>
            string.Join("|", lines.OrderBy(l => l.WageTypeId).Select(l => l.WageTypeId + "=" + Math.Round(l.Amount, 3).ToString(CultureInfo.InvariantCulture)));

        if (Signature(current.Select(c => (c.WageTypeId, c.Amount))) != Signature(payload.Before.Select(b => (b.WageTypeId, b.Amount))))
        {
            throw ChangedSinceDraft();
        }
    }

    private UserFriendlyException ChangedSinceDraft() => new(L("AiAgentBasicPayChangedSinceDraft"));

    private static CreateOrEditEmployeeBasicPayDetailDto ToDetail(Line line, int basicPayId) => new()
    {
        EmployeeBasicPayId = basicPayId,
        WageTypeId = line.WageTypeId,
        Amount = line.Amount,
        CurrencyId = line.CurrencyId,
        DependantNumber = line.DependantNumber,
        DependantUnit = line.DependantUnit,
        DependantWageTypeId = line.DependantWageTypeId
    };

    private static IEnumerable<(string NameEn, string NameAr, double? Before, double? After)> UnionOfLines(Payload payload)
    {
        var ids = payload.Before.Select(b => b.WageTypeId).Concat(payload.After.Select(a => a.WageTypeId)).Distinct();

        foreach (var id in ids)
        {
            var before = payload.Before.FirstOrDefault(b => b.WageTypeId == id);
            var after = payload.After.FirstOrDefault(a => a.WageTypeId == id);
            var names = after ?? before;
            yield return (names.NameEn, names.NameAr, before?.Amount, after?.Amount);
        }
    }

    /// <summary>Language-neutral on purpose, so the card reads the same in both languages: "30.000 → 80.000".</summary>
    public static string DescribeLine(double? before, double? after)
    {
        if (before.HasValue && after.HasValue && Math.Round(before.Value, 3) == Math.Round(after.Value, 3))
        {
            return AiPayLookup.FormatAmount(after.Value);
        }

        return $"{(before.HasValue ? AiPayLookup.FormatAmount(before.Value) : "—")} → {(after.HasValue ? AiPayLookup.FormatAmount(after.Value) : "—")}";
    }

    public enum ChangeMode
    {
        /// <summary>Close the record in effect (if any) and open a new one from the effective date.</summary>
        NewRecord = 0,

        /// <summary>A record already starts on the effective date: change its lines in place.</summary>
        UpdateRecord = 1
    }

    public class Payload
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeNameEn { get; set; }

        public string EmployeeNameAr { get; set; }

        public DateTime EffectiveDate { get; set; }

        public ChangeMode Mode { get; set; }

        /// <summary>The record the lines were copied from; null when the employee had none.</summary>
        public int? SourceBasicPayId { get; set; }

        public DateTime? SourceStartDate { get; set; }

        public DateTime? SourceEndDate { get; set; }

        /// <summary>True when the source record is still in effect on the effective date and must be ended.</summary>
        public bool CloseSourceRecord { get; set; }

        public PayType PayType { get; set; }

        public PayInterval PayInterval { get; set; }

        public List<Line> Before { get; set; } = new();

        public List<Line> After { get; set; } = new();
    }

    public class Line
    {
        public int WageTypeId { get; set; }

        public string Code { get; set; }

        public string NameEn { get; set; }

        public string NameAr { get; set; }

        public double Amount { get; set; }

        public int CurrencyId { get; set; }

        public double DependantNumber { get; set; }

        public DependantUnit DependantUnit { get; set; }

        public int? DependantWageTypeId { get; set; }
    }
}
