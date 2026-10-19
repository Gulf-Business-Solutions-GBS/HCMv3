using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PerformanceManagement.Review;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PerformanceManagement.Workflow;

/// <summary>
/// Workflow handler for the 360-style appraisal (<see cref="PAEmployee"/>).
///
/// <para>
/// Like the personnel action, and unlike leave or an asset request, an appraisal is raised ABOUT an
/// employee rather than BY them: <c>PAEmployeesAppService</c> takes the appraisee from the form, and legacy
/// did the same (its ESS entry point took an <c>employeeCode</c> separate from the signed-in user, so a
/// manager or a bulk org-unit launch could open appraisals for a whole team). The self-rating and the
/// manager's review that follow are recorded on the satellite rows -- <see cref="PAEmployeeReviewer"/> and
/// <see cref="PAEmployeeRating"/> -- not on this one. So the catalog row carries no ESS route and the
/// appraisal is submitted from the admin service through <see cref="IAdminTransactionSubmitter"/>.
/// </para>
///
/// <para>
/// Routing matters here because sign-off on an appraisal is what feeds a proposed increment and a proposed
/// job title into someone's pay and grade. Until it is approved the row stays invisible to everything that
/// reads appraisals, which is the ordinary <c>ApprovedOnly</c> behaviour inherited from the base.
/// </para>
///
/// <para>
/// <b>Routed on FORM TYPE.</b> The catalog row's <c>SubTypeSource</c> is <c>FormType</c> and that is what is
/// passed as the request sub-type, matching legacy (<c>WorkflowGroupDetail</c> offers "Form Type" as the
/// sub-type list for this request kind). It is the right axis: a 360 form is reviewed by a different set of
/// people from a probation form, and the cycle is just a period.
/// </para>
///
/// <para>
/// <b>Reviewers are added when approvers are assigned</b>, not on approval -- legacy's one side effect for
/// this type (<c>WorkFlowServices.OperateApproverInfo</c>). The step's <see cref="AppraisalStepSetting"/>
/// (legacy <c>PARequest</c>, "Appraisal WF Setup") decides who: the appraisee (self rate), each approver
/// (can add ratings), the current manager and anyone related through the configured relationship types.
/// Every flag defaults to off and nothing is seeded, so with no setting for the step nobody is added --
/// which is also what an appraisal already in flight gets: nothing is back-filled. A send-back removes
/// nobody; a reviewer once added stays (the owner's decision, as legacy).
/// </para>
/// </summary>
public class AppraisalWorkflowTransactionHandler : WorkflowTransactionHandlerBase<PAEmployee>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for appraisals.</summary>
    public const string AppraisalRequestType = "PAEmployee";

    private readonly IRepository<PACycle> _cycleRepository;
    private readonly IRepository<FormType> _formTypeRepository;
    private readonly IAppraisalStepSettingResolver _stepSettingResolver;
    private readonly IAppraisalReviewerManager _reviewerManager;

    public AppraisalWorkflowTransactionHandler(
        IRepository<PAEmployee> repository,
        IRepository<PACycle> cycleRepository,
        IRepository<FormType> formTypeRepository,
        IAppraisalStepSettingResolver stepSettingResolver,
        IAppraisalReviewerManager reviewerManager,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _cycleRepository = cycleRepository;
        _formTypeRepository = formTypeRepository;
        _stepSettingResolver = stepSettingResolver;
        _reviewerManager = reviewerManager;
    }

    public override string RequestType => AppraisalRequestType;

    /// <summary>
    /// Refuses an appraisal that names no cycle or no form type.
    ///
    /// <para>
    /// Both are the appraisal's identity rather than decoration. Without a cycle nobody can say which period
    /// is being signed off, so the same appraisal could be counted twice or against the wrong year; without
    /// a form type there is no set of KPIs and no reviewer limits, so there is literally nothing for the
    /// reviewers to fill in. Legacy checked the cycle at submit for the same reason
    /// (<c>WorkFlowServices</c>: "Period Cannot be blank"); the form type is checked here too because
    /// HCMv3's routing matches on it.
    /// </para>
    /// <para>
    /// Compared against zero rather than null: both FKs are non-nullable on the row, so "not set" is a 0,
    /// which would otherwise pass straight through and fail later as a broken foreign key.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var appraisal = await GetTargetAsync(context);
        if (appraisal == null)
        {
            return;
        }

        if (appraisal.PACycleId <= 0)
        {
            throw new UserFriendlyException(L("AnAppraisalNeedsACycle"));
        }

        if (appraisal.FormTypeId <= 0)
        {
            throw new UserFriendlyException(L("AnAppraisalNeedsAFormType"));
        }
    }

    /// <summary>
    /// The one-line inbox label, e.g. "2026 Annual, 360 Review, score 4.25".
    ///
    /// <para>
    /// Cycle and form type say WHICH appraisal is being signed off -- an approver seeing only an employee
    /// name cannot tell this year's annual review from a probation form. The overall score is the figure
    /// they are actually agreeing to, and it is the one thing a proposed increment is argued from, so it
    /// belongs on the line rather than one click away.
    /// </para>
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var appraisal = await GetTargetAsync(context);
        if (appraisal == null)
        {
            return null;
        }

        var cycle = _cycleRepository.GetAll()
            .Where(c => c.Id == appraisal.PACycleId)
            .Select(c => new { c.NameEn, c.NameAr })
            .FirstOrDefault();

        var formType = _formTypeRepository.GetAll()
            .Where(f => f.Id == appraisal.FormTypeId)
            .Select(f => new { f.NameEn, f.NameAr })
            .FirstOrDefault();

        var score = appraisal.OverallAverageScore.ToString("0.##");

        return new WorkflowSummary(
            $"{cycle?.NameEn ?? "Appraisal"}, {formType?.NameEn ?? "-"}, score {score}",
            $"{cycle?.NameAr ?? "تقييم"}، {formType?.NameAr ?? "-"}، النتيجة {score}");
    }

    /// <summary>
    /// Adds the step's reviewers the moment its approvers are known -- legacy's timing exactly. Inside the
    /// action: if a reviewer cannot be added (the form type's reviewer limit, most likely), the submission or
    /// approval fails with the reason, as it did in legacy, rather than opening a step that is missing
    /// somebody who should have scored.
    /// </summary>
    public override async Task OnApproversAssignedAsync(
        WorkflowTransactionContext context,
        int step,
        System.Collections.Generic.IReadOnlyList<int> approverEmployeeIds)
    {
        if (context.IsAmendment)
        {
            return;
        }

        var appraisal = await GetTargetAsync(context);
        if (appraisal == null)
        {
            return;
        }

        var setting = await _stepSettingResolver.GetForStepAsync(context.Request, step);
        if (setting == null)
        {
            return;
        }

        await _reviewerManager.AddStepReviewersAsync(appraisal, setting, approverEmployeeIds);
    }

    /// <summary>
    /// The appraisal's own fields for the approver's card: which cycle and form, the three scores and the
    /// proposals. The review itself -- KPI lines, ratings, reviewers -- is read live by the appraisal review
    /// form, which the card hosts by the target id.
    /// </summary>
    public override async Task<WorkflowRequestDetail> BuildDetailAsync(WorkflowTransactionContext context)
    {
        var appraisal = await GetTargetAsync(context);
        if (appraisal == null)
        {
            return await base.BuildDetailAsync(context);
        }

        var cycle = _cycleRepository.GetAll()
            .Where(c => c.Id == appraisal.PACycleId)
            .Select(c => new { c.NameEn, c.NameAr })
            .FirstOrDefault();

        var formType = _formTypeRepository.GetAll()
            .Where(f => f.Id == appraisal.FormTypeId)
            .Select(f => new { f.NameEn, f.NameAr })
            .FirstOrDefault();

        var isArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        return new WorkflowRequestDetail()
            .Add(WorkflowDetailField.Text(WorkflowDetailFieldNames.RequestNumber, L("RequestNumber"), context.Request?.RequestNumber))
            .Add(WorkflowDetailField.Text(nameof(PAEmployee.PACycle), L("PACycle"), isArabic ? cycle?.NameAr ?? cycle?.NameEn : cycle?.NameEn))
            .Add(WorkflowDetailField.Text(nameof(PAEmployee.FormType), L("FormType"), isArabic ? formType?.NameAr ?? formType?.NameEn : formType?.NameEn))
            .Add(WorkflowDetailField.Number(nameof(PAEmployee.OverallAverageScore), L("OverallAverageScore"), (decimal)appraisal.OverallAverageScore))
            .Add(WorkflowDetailField.Number(nameof(PAEmployee.PeerAverageScore), L("PeerAverageScore"), (decimal)appraisal.PeerAverageScore))
            .Add(WorkflowDetailField.Number(nameof(PAEmployee.EmployeeAverageScore), L("EmployeeAverageScore"), (decimal)appraisal.EmployeeAverageScore))
            .Add(WorkflowDetailField.Text(nameof(PAEmployee.ProposedIncrement), L("ProposedIncrement"), appraisal.ProposedIncrement))
            .Add(WorkflowDetailField.Text(nameof(PAEmployee.ProposedJobTitle), L("ProposedJobTitle"), appraisal.ProposedJobTitle));
    }
}
