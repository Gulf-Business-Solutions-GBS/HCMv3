using System;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for disciplinary records (<see cref="EmployeeDisciplinary"/>) -- warnings, violations
/// and the deductions they carry.
///
/// <para>
/// NOT self-service, and the evidence is structural rather than a judgement call. Legacy's
/// <c>DisciplinaryRequest</c> auto-filled <c>ReportedBy</c> with the logged-in employee while the subject
/// <c>Employee</c> was picked separately: the reporter is never the subject. A form on which you accuse
/// yourself is not a workflow. So the catalog row carries no <c>EssRoute</c> and no
/// <c>EssPermissionName</c>, and submission happens in <c>EmployeeDisciplinariesAppService</c> through
/// <c>IAdminTransactionSubmitter</c>, exactly as for <see cref="EmployeeActionWorkflowTransactionHandler"/>.
/// </para>
///
/// <para>
/// <b>There is no second table.</b> Legacy mirrored every field onto a new <c>EmployeeDisciplinary</c> when
/// the request reached Approved, inside an <c>OnSaving</c> block with no already-created guard -- so each
/// later save of an approved request inserted another disciplinary record against the employee, and the
/// escalation ladder below would then have counted those duplicates as real repeat offences. Here the
/// record IS the request, so approval has nothing to copy and that bug has nowhere to live.
/// </para>
///
/// <para>
/// <b>What approval actually decides.</b> The penalty. Until this handler runs, the record says what
/// happened; it does not say what it costs. <see cref="OnApprovedAsync"/> reads the employee's offence
/// count for this type inside the type's repeat window and stamps the matching tier's deduction type onto
/// the row -- which is why <see cref="BuildSummaryAsync"/> puts that penalty in front of the approver
/// rather than only the offence.
/// </para>
/// </summary>
public class DisciplinaryWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeDisciplinary>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for disciplinary records.</summary>
    public const string DisciplinaryRequestType = "EmployeeDisciplinary";

    private readonly IDisciplinaryEscalationService _escalationService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public DisciplinaryWorkflowTransactionHandler(
        IRepository<EmployeeDisciplinary> repository,
        IDisciplinaryEscalationService escalationService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _escalationService = escalationService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => DisciplinaryRequestType;

    /// <summary>
    /// The legacy required-field checks that were actually live: work location, disciplinary date, reporter
    /// and offence type.
    ///
    /// <para>
    /// Legacy also tested <c>DisciplinaryTime == null</c> and <c>Severity == null</c>. Both are
    /// non-nullable value types there, so both comparisons are constantly false and neither check has ever
    /// refused anything. They are not reproduced: porting a check that cannot fire would suggest a rule
    /// exists where none does, and would start rejecting records legacy accepted.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var record = await GetTargetAsync(context);
        if (record == null)
        {
            return;
        }

        if (record.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeIsRequired"));
        }

        if (!record.WorkLocationId.HasValue)
        {
            throw new UserFriendlyException(L("WorkLocationIsRequired"));
        }

        if (record.DisciplinaryDate == default)
        {
            throw new UserFriendlyException(L("DisciplinaryDateIsRequired"));
        }

        if (!record.ReportedById.HasValue)
        {
            throw new UserFriendlyException(L("ReportedByIsRequired"));
        }

        if (record.DisciplinaryTypeId <= 0)
        {
            throw new UserFriendlyException(L("DisciplinaryTypeIsRequired"));
        }
    }

    /// <summary>
    /// Stamps the penalty. This is the whole reason a disciplinary record is worth routing.
    ///
    /// <para>
    /// The record is not merely made visible on approval -- it acquires its consequence. The escalation
    /// ladder is read at THIS moment, not at creation, and that is deliberate: between raising and
    /// approving, another offence of the same type may have been raised or one may have aged out of the
    /// repeat window, and the penalty that gets written must be the one that is true when the decision is
    /// made.
    /// </para>
    /// <para>
    /// It overwrites whatever deduction type was on the row. The ladder, not the person filling the form,
    /// decides what a third offence in a fortnight costs -- otherwise the tiers configured on the type are
    /// a suggestion.
    /// </para>
    /// <para>
    /// If the type has no tier at or below this offence number, the deduction is cleared rather than left
    /// stale. A record carrying a penalty the ladder does not support is worse than one carrying none,
    /// because nothing downstream can tell it was never resolved.
    /// </para>
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        var record = await GetTargetAsync(context);
        if (record == null)
        {
            return;
        }

        var escalation = await _escalationService.ResolveAsync(
            record.EmployeeId,
            record.DisciplinaryTypeId,
            record.DisciplinaryDate,
            record.Id);

        record.DisciplinaryDeductionTypeId = escalation.DisciplinaryDeductionTypeId;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await Repository.UpdateAsync(record);
        }
    }

    /// <summary>
    /// The offence, when it happened, and -- the part that matters -- the penalty it resolves to.
    ///
    /// <para>
    /// An approver of a disciplinary record is not agreeing that something happened; they are agreeing to
    /// what it costs the employee. "Late Attendance, 12 Sep 2026, 3rd offence, penalty: One Day Deduction"
    /// is a decision. "Late Attendance, 12 Sep 2026" is a note, and someone approving it would be signing
    /// off a deduction they were never shown.
    /// </para>
    /// <para>
    /// The penalty shown here is a projection from the state at submission time, and the summary is a
    /// snapshot -- if the count moves before the decision, <see cref="OnApprovedAsync"/> writes the newer
    /// answer. Showing the approver the projection is still far better than showing them nothing.
    /// </para>
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var record = await GetTargetAsync(context);
        if (record == null)
        {
            return null;
        }

        var escalation = await _escalationService.ResolveAsync(
            record.EmployeeId,
            record.DisciplinaryTypeId,
            record.DisciplinaryDate,
            record.Id);

        var on = record.DisciplinaryDate.ToString("dd MMM yyyy");
        var offence = escalation.OffenceNumber;
        var typeEn = escalation.DisciplinaryTypeNameEn ?? "Disciplinary";
        var typeAr = escalation.DisciplinaryTypeNameAr ?? "إجراء تأديبي";
        var penaltyEn = escalation.DisciplinaryDeductionTypeNameEn ?? "no deduction";
        var penaltyAr = escalation.DisciplinaryDeductionTypeNameAr ?? "بدون خصم";

        return new WorkflowSummary(
            $"{typeEn}, {on}, offence {offence}, penalty: {penaltyEn}",
            $"{typeAr}، {on}، مخالفة {offence}، العقوبة: {penaltyAr}");
    }
}
