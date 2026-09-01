using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.TimeManagement.Workflow;

/// <summary>
/// Workflow handler for the shift override (<see cref="EmployeeSubstituteDuty"/>) -- the port of legacy
/// <c>RosterPlannerRequest</c>.
///
/// <para>
/// <b>No RosterPlannerRequest tables.</b> Legacy carried a request header plus a detail line per employee
/// and date, mirroring rows that already existed in <c>EmployeeSubstituteDuty</c>, and the two drifted the
/// moment anybody edited either side. Here the override row IS the request: one employee, one date, one
/// Duty, routed in place. Legacy's batch grid is deliberately not ported in this pass, because
/// <c>WorkflowRequest.TargetEntityId</c> addresses one row.
/// </para>
///
/// <para>
/// <b>What approval buys.</b> A pending override is Draft or Open, which the <c>ApprovedOnly</c> filter
/// hides, and every roster read path (<c>RosterService.GetRosterDutyAsync</c>, <c>TimeDataService</c>, the
/// Time Engine's day loop, the attendance reports) reaches this table through an ordinary
/// <c>IRepository&lt;EmployeeSubstituteDuty&gt;</c> query. So an un-approved override changes nobody's
/// calculated roster and nobody's TimeRegister day. That matters more here than it looks: a substitute duty
/// wins outright over the employee's duty period and their roster group, so an override that leaked into
/// those queries would silently rewrite what the employee was due to work -- and therefore their late,
/// early, overtime and absence figures for the day.
/// </para>
///
/// <para>
/// <b>Removal is an amendment.</b> Taking a planned shift away is expressed by amending the override to an
/// off-duty / rest <see cref="Duty"/>, not by deleting the row -- see <see cref="SubstituteDutyAmendment"/>
/// and the entity remarks for why the engine has no pending-removal state.
/// </para>
/// </summary>
public class SubstituteDutyWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeSubstituteDuty>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for shift overrides.</summary>
    public const string SubstituteDutyRequestType = "EmployeeSubstituteDuty";

    private readonly IRepository<Duty> _dutyRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public SubstituteDutyWorkflowTransactionHandler(
        IRepository<EmployeeSubstituteDuty> repository,
        IRepository<Duty> dutyRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _dutyRepository = dutyRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => SubstituteDutyRequestType;

    /// <summary>
    /// Refuses a second override for the same employee on the same date.
    ///
    /// <para>
    /// Run with the <c>ApprovedOnly</c> filter OFF, and that is the whole point of doing this here rather
    /// than leaving it to the app service. An override that has been asked for but not yet granted is Draft
    /// or Open -- exactly what that filter hides -- so the app service's check can only ever see the
    /// approved ones, and a planner could raise the same date over and over with every one passing
    /// validation. Approve two of them and which Duty the employee works becomes whichever row the roster
    /// query happens to return first.
    /// </para>
    /// <para>
    /// Rejected and sent-back rows do not block: neither is a claim on the employee's day.
    /// </para>
    /// <para>
    /// For an AMENDMENT the row still holds the OLD values -- the proposed ones live on the request until
    /// approval -- so the proposed date is what gets checked. Validating the row would check a date nobody
    /// is asking for, and would pass an amendment that in fact collides with an existing override.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var substituteDuty = await GetTargetAsync(context);
        if (substituteDuty == null)
        {
            return;
        }

        var proposed = context.IsAmendment ? ReadAmendment(context) : null;

        var dutyId = proposed?.DutyId ?? substituteDuty.DutyId;
        var substituteDate = (proposed?.SubstituteDate ?? substituteDuty.SubstituteDate).Date;

        ValidateValues(substituteDuty.EmployeeId, dutyId, substituteDate);

        EnsureNoOtherOverrideExists(substituteDuty.EmployeeId, substituteDate, excludeId: substituteDuty.Id);
    }

    /// <inheritdoc/>
    public override bool SupportsAmendment => true;

    /// <summary>
    /// What the override looked like before the change was proposed, so a rejected amendment leaves no
    /// question about what the row is supposed to say -- and so the approver can be shown the before as well
    /// as the after.
    /// </summary>
    public override async Task<string> BuildSnapshotAsync(WorkflowTransactionContext context)
    {
        var substituteDuty = await GetTargetAsync(context);
        if (substituteDuty == null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new SubstituteDutyAmendment
        {
            DutyId = substituteDuty.DutyId,
            SubstituteDate = substituteDuty.SubstituteDate
        });
    }

    /// <summary>
    /// Writes an approved amendment onto the live override row.
    ///
    /// <para>
    /// Re-validated here, not only at submit. Between raising the change and approving it another override
    /// may have been approved for the date this one is moving to, and the approver would otherwise be
    /// granting a second override for the same day. Throwing rolls the approval back with it.
    /// </para>
    /// <para>
    /// The row's status is untouched: the override was approved before the amendment and stays approved.
    /// The filter must not so much as blink, or the shift would vanish from the roster mid-change.
    /// </para>
    /// </summary>
    public override async Task ApplyAmendmentAsync(WorkflowTransactionContext context)
    {
        var substituteDuty = await GetRequiredTargetAsync(context);
        var proposed = ReadAmendment(context);

        var substituteDate = proposed.SubstituteDate.Date;

        ValidateValues(substituteDuty.EmployeeId, proposed.DutyId, substituteDate);
        EnsureNoOtherOverrideExists(substituteDuty.EmployeeId, substituteDate, excludeId: substituteDuty.Id);

        substituteDuty.DutyId = proposed.DutyId;
        substituteDuty.SubstituteDate = substituteDate;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await Repository.UpdateAsync(substituteDuty);
        }
    }

    /// <summary>
    /// The one-line inbox label. A new override reads as "Night Duty, 14 Mar 2026"; an amendment reads as a
    /// CHANGE -- "Change: Day Duty to Night Duty, 14 Mar 2026" -- because an approver deciding whether to
    /// move somebody off days and onto nights needs the before as well as the after. Shown only once when
    /// the date is unchanged, and as two full descriptions when the shift is being moved to another day.
    ///
    /// <para>
    /// An off-duty or rest Duty is labelled as such, so the removal case reads unmistakably: "Change: Day
    /// Duty to Weekly Off (off duty), 14 Mar 2026" is an approver being asked to take the shift away, and
    /// nothing about that sentence needs explaining.
    /// </para>
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var substituteDuty = await GetTargetAsync(context);
        if (substituteDuty == null)
        {
            return null;
        }

        if (context.IsAmendment)
        {
            var proposed = ReadAmendment(context);

            var sameDate = proposed.SubstituteDate.Date == substituteDuty.SubstituteDate.Date;
            if (sameDate)
            {
                return new WorkflowSummary(
                    "Change: " + DutyName(substituteDuty.DutyId, arabic: false) +
                    " to " + DescribeOverride(proposed.DutyId, proposed.SubstituteDate, arabic: false),
                    "تعديل: " + DutyName(substituteDuty.DutyId, arabic: true) +
                    " إلى " + DescribeOverride(proposed.DutyId, proposed.SubstituteDate, arabic: true));
            }

            return new WorkflowSummary(
                "Change: " + DescribeOverride(substituteDuty.DutyId, substituteDuty.SubstituteDate, arabic: false) +
                " to " + DescribeOverride(proposed.DutyId, proposed.SubstituteDate, arabic: false),
                "تعديل: " + DescribeOverride(substituteDuty.DutyId, substituteDuty.SubstituteDate, arabic: true) +
                " إلى " + DescribeOverride(proposed.DutyId, proposed.SubstituteDate, arabic: true));
        }

        return new WorkflowSummary(
            DescribeOverride(substituteDuty.DutyId, substituteDuty.SubstituteDate, arabic: false),
            DescribeOverride(substituteDuty.DutyId, substituteDuty.SubstituteDate, arabic: true));
    }

    private void ValidateValues(int employeeId, int dutyId, DateTime substituteDate)
    {
        if (employeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        if (dutyId <= 0)
        {
            throw new UserFriendlyException(L("DutyCannotBeBlank"));
        }

        if (substituteDate == default)
        {
            throw new UserFriendlyException(L("SubstituteDateIsRequired"));
        }
    }

    /// <summary>
    /// One override per employee per date, counting the ones still awaiting approval -- see the remarks on
    /// <see cref="ValidateBeforeSubmitAsync"/> for why the filter comes off.
    /// </summary>
    private void EnsureNoOtherOverrideExists(int employeeId, DateTime substituteDate, int excludeId)
    {
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var duplicate = Repository.GetAll()
                .Any(e => e.Id != excludeId
                          && e.EmployeeId == employeeId
                          && e.SubstituteDate == substituteDate
                          && (e.WorkflowStatus == null
                              || e.WorkflowStatus == RequestStatus.Approved
                              || e.WorkflowStatus == RequestStatus.Closed
                              || e.WorkflowStatus == RequestStatus.Open));

            if (duplicate)
            {
                throw new UserFriendlyException(
                    L("SubstituteDateAlreadyExists", substituteDate.ToString("dd/MM/yyyy")));
            }
        }
    }

    private static SubstituteDutyAmendment ReadAmendment(WorkflowTransactionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.AmendmentJson))
        {
            throw new UserFriendlyException("This amendment carries no proposed values.");
        }

        return JsonSerializer.Deserialize<SubstituteDutyAmendment>(context.AmendmentJson);
    }

    private string DescribeOverride(int dutyId, DateTime substituteDate, bool arabic)
    {
        return DutyName(dutyId, arabic) + (arabic ? "، " : ", ") + substituteDate.ToString("dd MMM yyyy");
    }

    /// <summary>
    /// The Duty's name, with off-duty and rest days called out. Reading "Weekly Off" on its own leaves an
    /// approver to know from the name alone that this one means "do not come in"; the suffix says it.
    /// </summary>
    private string DutyName(int dutyId, bool arabic)
    {
        var duty = _dutyRepository.GetAll().FirstOrDefault(d => d.Id == dutyId);
        if (duty == null)
        {
            return arabic ? "وردية" : "duty";
        }

        var name = arabic ? duty.NameAr ?? duty.NameEn : duty.NameEn ?? duty.NameAr;
        var isOffDuty = duty.IsWeeklyOff || duty.IsRestDay;

        if (!isOffDuty)
        {
            return name;
        }

        return arabic ? name + " (بدون دوام)" : name + " (off duty)";
    }
}
