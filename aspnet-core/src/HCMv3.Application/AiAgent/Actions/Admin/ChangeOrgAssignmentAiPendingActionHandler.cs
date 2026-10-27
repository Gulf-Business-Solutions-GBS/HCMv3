using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.AiAgent.Tools;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.AiAgent.Actions;

/// <summary>
/// Applies an organisational assignment change an administrator confirmed, through the Org Assignments screen's
/// service -- which ends the previous assignment the day before.
///
/// <para>
/// <b>The four fields the screen's service drops.</b> A new assignment row saved through that service gets no
/// employee group, sub group, payroll area or workflow group: its DTO does not carry them and it does not copy
/// them from the previous row. A row without them breaks the Time Engine, leave rules, wage types and approval
/// routing from the transfer date on. So after the service has saved the row, the four are copied onto it from
/// the assignment it replaces -- in the same unit of work, so it is all or nothing. Chosen by the business owner
/// over waiting for the screen fix on another branch; once that lands, this copy finds the values already set
/// and changes nothing.
/// </para>
/// </summary>
public class ChangeOrgAssignmentAiPendingActionHandler : AiPendingActionHandlerBase
{
    public const string Type = "ChangeOrgAssignment";

    private readonly IEmployeeOrgAssignmentsAppService _assignments;
    private readonly IRepository<EmployeeOrgAssignment> _assignmentRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ChangeOrgAssignmentAiPendingActionHandler(
        IEmployeeOrgAssignmentsAppService assignments,
        IRepository<EmployeeOrgAssignment> assignmentRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _assignments = assignments;
        _assignmentRepository = assignmentRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string ActionType => Type;

    public override AiPendingActionPreview Describe(string payloadJson)
    {
        var payload = Read<Payload>(payloadJson);

        var preview = new AiPendingActionPreview { Title = L("AiAgentActionChangeOrgAssignment") }
            .Add(L("AiAgentEmployee"), $"{payload.EmployeeCode} · {PickName(payload.EmployeeNameEn, payload.EmployeeNameAr)}")
            .Add(L("AiAgentEffectiveFrom"), FormatDate(payload.EffectiveDate));

        foreach (var kind in Assignment.Kinds)
        {
            var before = payload.Before.Name(kind);
            var after = payload.After.Name(kind);
            var changed = payload.Before.Id(kind) != payload.After.Id(kind);

            if (changed || after.En != null)
            {
                preview.Add(L(Label(kind)), changed
                    ? $"{PickName(before.En, before.Ar) ?? "—"} → {PickName(after.En, after.Ar) ?? "—"}"
                    : PickName(after.En, after.Ar));
            }
        }

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
            var records = (await _assignments.GetAll(new GetAllEmployeeOrgAssignmentsInput { EmployeeId = payload.EmployeeId, MaxResultCount = 1000 })).Items;
            if (Snapshot(records) != payload.Snapshot)
            {
                throw new UserFriendlyException(L("AiAgentRecordChangedSinceDraft"));
            }

            if (payload.UpdatesRecordStartingSameDay)
            {
                // The service's update leaves the four group fields alone, so nothing needs copying here.
                var edit = (await _assignments.GetEmployeeOrgAssignmentForEdit(new EntityDto(payload.SourceRecordId!.Value))).EmployeeOrgAssignment;
                payload.After.ApplyTo(edit);
                await _assignments.CreateOrEdit(edit);
            }
            else
            {
                var create = new CreateOrEditEmployeeOrgAssignmentDto { EmployeeId = payload.EmployeeId, StartDate = payload.EffectiveDate };
                payload.After.ApplyTo(create);
                await _assignments.CreateOrEdit(create);

                if (payload.SourceRecordId.HasValue)
                {
                    await CarryGroupFieldsForwardAsync(payload);
                }
            }

            await uow.CompleteAsync();
        }

        return L("AiAgentOrgAssignmentChanged", payload.EmployeeCode, FormatDate(payload.EffectiveDate));
    }

    private async Task CarryGroupFieldsForwardAsync(Payload payload)
    {
        var source = await _assignmentRepository.GetAsync(payload.SourceRecordId!.Value);
        var created = await _assignmentRepository.GetAll()
            .SingleAsync(a => a.EmployeeId == payload.EmployeeId && a.StartDate == payload.EffectiveDate);

        created.EmployeeGroupId ??= source.EmployeeGroupId;
        created.EmployeeSubGroupId ??= source.EmployeeSubGroupId;
        created.PayrollAreaId ??= source.PayrollAreaId;
        created.WorkflowGroupId ??= source.WorkflowGroupId;

        await _unitOfWorkManager.Current.SaveChangesAsync();
    }

    /// <summary>Every assignment of the employee and what it points at, as drafted against.</summary>
    public static string Snapshot(IEnumerable<EmployeeOrgAssignmentDto> records) =>
        string.Join("|", records.OrderBy(r => r.Id).Select(r =>
            $"{r.Id}:{r.StartDate:yyyyMMdd}:{r.EndDate:yyyyMMdd}:{r.OrganizationUnitId}:{r.JobTitleId}:{r.CostCenterId}:{r.WorkLocationId}:{r.PostingGroupId}:{r.BudgetingGroupId}"));

    private static string Label(AiMasterDataKind kind) => kind switch
    {
        AiMasterDataKind.OrgUnit => "OrganizationUnit",
        AiMasterDataKind.JobTitle => "JobTitle",
        AiMasterDataKind.CostCenter => "CostCenter",
        AiMasterDataKind.WorkLocation => "WorkLocation",
        _ => kind.ToString()
    };

    public class Payload
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeNameEn { get; set; }

        public string EmployeeNameAr { get; set; }

        public DateTime EffectiveDate { get; set; }

        /// <summary>The assignment in effect on the effective date when drafted; null when there was none.</summary>
        public int? SourceRecordId { get; set; }

        public bool UpdatesRecordStartingSameDay { get; set; }

        public Assignment Before { get; set; } = new();

        public Assignment After { get; set; } = new();

        public string Snapshot { get; set; }
    }

    /// <summary>What an assignment points at, with names for the card. Posting and budgeting group are carried as is.</summary>
    public class Assignment
    {
        public static readonly AiMasterDataKind[] Kinds =
        {
            AiMasterDataKind.OrgUnit, AiMasterDataKind.JobTitle, AiMasterDataKind.CostCenter, AiMasterDataKind.WorkLocation
        };

        public long? OrgUnitId { get; set; }
        public string OrgUnitEn { get; set; }
        public string OrgUnitAr { get; set; }

        public int? JobTitleId { get; set; }
        public string JobTitleEn { get; set; }
        public string JobTitleAr { get; set; }

        public int? CostCenterId { get; set; }
        public string CostCenterEn { get; set; }
        public string CostCenterAr { get; set; }

        public int? WorkLocationId { get; set; }
        public string WorkLocationEn { get; set; }
        public string WorkLocationAr { get; set; }

        public int? PostingGroupId { get; set; }

        public int? BudgetingGroupId { get; set; }

        public static Assignment From(EmployeeOrgAssignmentDto dto) => new()
        {
            OrgUnitId = dto.OrganizationUnitId,
            OrgUnitEn = dto.OrganizationUnitName,
            JobTitleId = dto.JobTitleId,
            JobTitleEn = dto.JobTitleName,
            CostCenterId = dto.CostCenterId,
            CostCenterEn = dto.CostCenterName,
            WorkLocationId = dto.WorkLocationId,
            WorkLocationEn = dto.WorkLocationName,
            PostingGroupId = dto.PostingGroupId,
            BudgetingGroupId = dto.BudgetingGroupId
        };

        public Assignment Copy() => (Assignment)MemberwiseClone();

        public long? Id(AiMasterDataKind kind) => kind switch
        {
            AiMasterDataKind.OrgUnit => OrgUnitId,
            AiMasterDataKind.JobTitle => JobTitleId,
            AiMasterDataKind.CostCenter => CostCenterId,
            AiMasterDataKind.WorkLocation => WorkLocationId,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        public (string En, string Ar) Name(AiMasterDataKind kind) => kind switch
        {
            AiMasterDataKind.OrgUnit => (OrgUnitEn, OrgUnitAr),
            AiMasterDataKind.JobTitle => (JobTitleEn, JobTitleAr),
            AiMasterDataKind.CostCenter => (CostCenterEn, CostCenterAr),
            AiMasterDataKind.WorkLocation => (WorkLocationEn, WorkLocationAr),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        public void Set(AiMasterDataKind kind, AiNamedRecord record)
        {
            switch (kind)
            {
                case AiMasterDataKind.OrgUnit:
                    (OrgUnitId, OrgUnitEn, OrgUnitAr) = (record.Id, record.NameEn, record.NameAr);
                    break;
                case AiMasterDataKind.JobTitle:
                    (JobTitleId, JobTitleEn, JobTitleAr) = ((int)record.Id, record.NameEn, record.NameAr);
                    break;
                case AiMasterDataKind.CostCenter:
                    (CostCenterId, CostCenterEn, CostCenterAr) = ((int)record.Id, record.NameEn, record.NameAr);
                    break;
                case AiMasterDataKind.WorkLocation:
                    (WorkLocationId, WorkLocationEn, WorkLocationAr) = ((int)record.Id, record.NameEn, record.NameAr);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        public List<(AiMasterDataKind Kind, string Before, string After)> DifferencesFrom(Assignment before) =>
            Kinds.Where(k => Id(k) != before.Id(k))
                .Select(k => (k, before.Name(k).En, Name(k).En))
                .ToList();

        public void ApplyTo(CreateOrEditEmployeeOrgAssignmentDto dto)
        {
            dto.OrganizationUnitId = OrgUnitId;
            dto.JobTitleId = JobTitleId;
            dto.CostCenterId = CostCenterId;
            dto.WorkLocationId = WorkLocationId;
            dto.PostingGroupId = PostingGroupId;
            dto.BudgetingGroupId = BudgetingGroupId;
        }
    }
}
