using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for passport (and other document) withdrawals -- <see cref="EmployeeDocumentMvmt"/>.
///
/// <para>
/// The clearest example in the codebase of what the single-row design buys. Legacy carried a whole
/// <c>PassportWithdrawalRequest</c> table alongside the movement log: the request was approved in one table
/// and then, at the final step, copied into <c>EmployeeDocumentMvmt</c> in the other -- with the copy
/// deciding, by a lookup on "is there a row with no actual return date", whether to overwrite an existing
/// movement or add a new one. Here the request IS the movement row; it is simply hidden by the
/// <c>ApprovedOnly</c> filter until approved, so there is no copy step to get wrong and no window where the
/// two tables disagree about who is holding whose passport.
/// </para>
/// </summary>
public class PassportWithdrawalWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeDocumentMvmt>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for passport withdrawals.</summary>
    public const string PassportWithdrawalRequestType = "EmployeeDocumentMvmt";

    private readonly IRepository<EmployeeDocument> _documentRepository;
    private readonly IRepository<DocumentType> _documentTypeRepository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public PassportWithdrawalWorkflowTransactionHandler(
        IRepository<EmployeeDocumentMvmt> repository,
        IRepository<EmployeeDocument> documentRepository,
        IRepository<DocumentType> documentTypeRepository,
        IRepository<WorkflowRequest> requestRepository,
        ISysSettingsAccessor sysSettingsAccessor,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _documentRepository = documentRepository;
        _documentTypeRepository = documentTypeRepository;
        _requestRepository = requestRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public override string RequestType => PassportWithdrawalRequestType;

    /// <summary>
    /// Three checks, all of them ported from the legacy controller's own validation block.
    ///
    /// <para>
    /// Note what is deliberately NOT here: a rule refusing a withdrawal while the employee already has one
    /// open. Legacy allowed it on purpose and treated the later request as an extension of the earlier one
    /// (see <see cref="OnApprovedAsync"/>). Someone whose passport is already out and who needs it for
    /// another week must be able to ask; blocking them would leave the only route as withdrawing the first
    /// request and starting again, which loses its approval history.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var movement = await GetTargetAsync(context);
        if (movement == null)
        {
            return;
        }

        if (movement.ReturnDate.Date < movement.WithdrawDate.Date)
        {
            throw new UserFriendlyException(L("ReturnDateCannotBeEarlierThanWithdrawDate"));
        }

        // The filter is disabled because the question is whether the document row exists at all, not
        // whether it is currently approved. A document that is itself awaiting approval is a different
        // problem, and reporting it as "no such document" would send the employee looking for a typo.
        bool documentExists;
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            documentExists = _documentRepository.GetAll().Any(d => d.Id == movement.EmployeeDocumentId);
        }

        if (!documentExists)
        {
            throw new UserFriendlyException(L("EmployeeDocumentNotFound"));
        }

        // Mandatory only where the tenant has asked for it. Legacy read the same flag from a JSON file on
        // the ESS server; it is a tenant setting here. No settings row yet means a fresh tenant that has
        // never opened the self-service screen -- treated as "not mandatory" rather than blocking every
        // request until somebody saves the form.
        if (movement.WithdrawalReason.IsNullOrWhiteSpace())
        {
            var settings = await _sysSettingsAccessor.GetSelfServiceSettingsAsync();

            if (settings != null && settings.IsWithdrawalReasonMandatory)
            {
                throw new UserFriendlyException(L("WithdrawalReasonIsRequired"));
            }
        }
    }

    /// <summary>
    /// The approver is deciding whether to hand over an original document and when they will get it back,
    /// so the summary is exactly those three facts: which document, out when, back when.
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var movement = await GetTargetAsync(context);
        if (movement == null)
        {
            return null;
        }

        string documentNum = null;
        int? documentTypeId = null;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var document = _documentRepository.GetAll()
                .Where(d => d.Id == movement.EmployeeDocumentId)
                .Select(d => new { d.DocumentNum, d.DocumentTypeId })
                .FirstOrDefault();

            if (document != null)
            {
                documentNum = document.DocumentNum;
                documentTypeId = document.DocumentTypeId;
            }
        }

        var documentType = documentTypeId.HasValue
            ? _documentTypeRepository.GetAll()
                .Where(t => t.Id == documentTypeId.Value)
                .Select(t => new { t.NameEn, t.NameAr })
                .FirstOrDefault()
            : null;

        var withdrawn = movement.WithdrawDate.ToString("dd MMM yyyy");
        var returned = movement.ReturnDate.ToString("dd MMM yyyy");

        var labelEn = documentNum.IsNullOrWhiteSpace()
            ? documentType?.NameEn ?? "Document"
            : (documentType?.NameEn ?? "Document") + " " + documentNum;

        var labelAr = documentNum.IsNullOrWhiteSpace()
            ? documentType?.NameAr ?? "مستند"
            : (documentType?.NameAr ?? "مستند") + " " + documentNum;

        return new WorkflowSummary(
            $"{labelEn}, out {withdrawn}, back {returned}",
            $"{labelAr}، الاستلام {withdrawn}، الإرجاع {returned}");
    }

    /// <summary>
    /// On approval, closes any other still-open withdrawal request the same employee has.
    ///
    /// <para>
    /// This is the legacy "extension" mechanism, ported deliberately (legacy
    /// <c>PassportWithdrawalRequestController</c>, final-step block). The employee whose passport is already
    /// out and who needs it a week longer raises a second request rather than being blocked; when that one
    /// is approved it SUPERSEDES the first, which is closed rather than left waiting in an approver's inbox
    /// for a period that has been overtaken.
    /// </para>
    /// <para>
    /// Only the workflow request is closed. The superseded transaction row keeps its in-flight status, so it
    /// stays invisible to the movement log and the document-status calculation -- exactly where a never
    /// approved withdrawal belongs. Stamping it <c>Closed</c> to match its request would be the tempting
    /// symmetry and would be wrong: <c>Closed</c> counts as effective for the <c>ApprovedOnly</c> filter, so
    /// the employee would end up with two live withdrawal periods for one document, one of which nobody ever
    /// approved.
    /// </para>
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        if (context.IsAmendment)
        {
            return;
        }

        var movement = await GetTargetAsync(context);

        var employeeId = movement?.EmployeeId ?? context.EmployeeId;
        if (!employeeId.HasValue)
        {
            return;
        }

        var thisRequestId = context.Request?.Id ?? 0;

        var superseded = _requestRepository.GetAll()
            .Where(r => r.RequestType == PassportWithdrawalRequestType
                        && r.EmployeeId == employeeId.Value
                        && r.RequestStatus == RequestStatus.Open
                        && r.Id != thisRequestId)
            .ToList();

        foreach (var request in superseded)
        {
            request.RequestStatus = RequestStatus.Closed;
            await _requestRepository.UpdateAsync(request);
        }
    }
}
