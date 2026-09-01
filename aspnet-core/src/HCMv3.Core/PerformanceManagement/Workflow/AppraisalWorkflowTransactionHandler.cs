using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
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
/// <b>No approval side effect, on purpose.</b> Legacy did have one, but it fired when APPROVERS WERE
/// ASSIGNED rather than on approval (<c>WorkFlowServices.OperateApproverInfo</c>): it auto-created
/// <see cref="PAEmployeeReviewer"/> rows for the appraisee, the approver, the current manager and anyone
/// related through configured relationship types. Every branch of it is gated on the legacy <c>PARequest</c>
/// flags <c>CanEmployeeSelfRate</c>, <c>CanAddRatings</c>, <c>AutoAddManager</c> and
/// <c>AutoAddRelationShip</c>, which hang off the legacy request master and have no counterpart on HCMv3's
/// <c>RequestMaster</c>/<c>RequestDetail</c>. Reproducing it would mean inventing those flags and picking
/// defaults, and a wrong default here silently adds or omits a reviewer -- which changes the score. So the
/// base behaviour stands and the gap is reported rather than guessed at.
/// </para>
/// </summary>
public class AppraisalWorkflowTransactionHandler : WorkflowTransactionHandlerBase<PAEmployee>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for appraisals.</summary>
    public const string AppraisalRequestType = "PAEmployee";

    private readonly IRepository<PACycle> _cycleRepository;
    private readonly IRepository<FormType> _formTypeRepository;

    public AppraisalWorkflowTransactionHandler(
        IRepository<PAEmployee> repository,
        IRepository<PACycle> cycleRepository,
        IRepository<FormType> formTypeRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _cycleRepository = cycleRepository;
        _formTypeRepository = formTypeRepository;
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
}
