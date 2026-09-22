using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;

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
///
/// <para>
/// The RETURN leg lives here too, as an amendment (<see cref="DocumentMvmtAmendment"/>). Legacy expressed it
/// as a second <c>MovementType</c> on the same request object; here it is what it actually is -- a change to
/// one field of a row that is already live and approved -- so it needs no entity, no table and no catalog row
/// of its own, and it inherits the amendment rule that a REFUSED return leaves the movement exactly as it
/// was.
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

        // An amendment here is a RETURN, and none of the withdrawal checks below apply to it: the row still
        // holds the values that were already approved, so re-running them would re-validate a decision
        // nobody is asking about and could refuse a return because of a setting that changed since.
        if (context.IsAmendment)
        {
            ValidateReturn(movement, ReadAmendment(context));
            return;
        }

        // Legacy refused a blank withdraw or return date outright ("WithdrawDate cannot be blank").
        if (movement.WithdrawDate == default)
        {
            throw new UserFriendlyException(L("WithdrawDateIsRequired"));
        }

        if (movement.ReturnDate == default)
        {
            throw new UserFriendlyException(L("ReturnDateIsRequired"));
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
    /// The rules for a RETURN, applied to a set of values rather than to the row -- so the same checks serve
    /// the submit (where the return is only proposed) and the approval (where it is about to be written).
    ///
    /// <para>
    /// The already-returned check is the one legacy did not have. Legacy looked up the movement row with no
    /// actual return date and, finding none, saved the request and silently stamped nothing -- so a document
    /// returned twice produced an approved request that changed no data at all, and nobody could tell from
    /// the screens which of the two returns was the real one. Refusing is louder and safer.
    /// </para>
    /// </summary>
    private void ValidateReturn(EmployeeDocumentMvmt movement, DocumentMvmtAmendment proposed)
    {
        if (!proposed.ActualReturnDate.HasValue)
        {
            throw new UserFriendlyException(L("ActualReturnDateIsRequired"));
        }

        if (movement.ActualReturnDate != default)
        {
            throw new UserFriendlyException(L("ThisDocumentHasAlreadyBeenReturned"));
        }

        if (proposed.ActualReturnDate.Value.Date < movement.WithdrawDate.Date)
        {
            throw new UserFriendlyException(L("ActualReturnDateCannotBeEarlierThanWithdrawDate"));
        }
    }

    /// <inheritdoc/>
    public override bool SupportsAmendment => true;

    /// <summary>
    /// Captures what the movement said before the return was asked for.
    ///
    /// <para>
    /// It is a single field, and that is exactly the field the change is about: without it the record of
    /// "what this movement looked like before" would be missing the whole point of the change. Stored as
    /// null when the row carries <c>default(DateTime)</c>, so the snapshot says "the document was still out"
    /// rather than "it came back in year one".
    /// </para>
    /// </summary>
    public override async Task<string> BuildSnapshotAsync(WorkflowTransactionContext context)
    {
        var movement = await GetTargetAsync(context);
        if (movement == null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new DocumentMvmtAmendment
        {
            ActualReturnDate = movement.ActualReturnDate == default
                ? (DateTime?)null
                : movement.ActualReturnDate
        });
    }

    /// <summary>
    /// Stamps an approved return onto the live movement row.
    ///
    /// <para>
    /// Re-validated here, not only at submit. Between raising the return and approving it somebody else may
    /// have recorded the document as back -- an HR clerk taking it at the counter, say -- and stamping a
    /// second date over the first would silently rewrite the custody record. Throwing rolls the approval
    /// back with it.
    /// </para>
    /// </summary>
    public override async Task ApplyAmendmentAsync(WorkflowTransactionContext context)
    {
        var movement = await GetRequiredTargetAsync(context);
        var proposed = ReadAmendment(context);

        ValidateReturn(movement, proposed);

        movement.ActualReturnDate = proposed.ActualReturnDate.Value.Date;

        // Status is untouched on purpose: the withdrawal was approved before the return and stays approved.
        // The filter must not so much as blink, or the movement would vanish from the log mid-change and the
        // document would read as never having been withdrawn at all.
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await Repository.UpdateAsync(movement);
        }
    }

    private static DocumentMvmtAmendment ReadAmendment(WorkflowTransactionContext context)
    {
        if (context.AmendmentJson.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("This amendment carries no proposed values.");
        }

        return JsonSerializer.Deserialize<DocumentMvmtAmendment>(context.AmendmentJson);
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

        if (context.IsAmendment)
        {
            // A return reads as a return, not as a generic change. "Change: ... to ..." is the right shape
            // for an amendment that moves dates about, but here the approver is being asked to confirm that
            // a physical document is back in the safe, and a summary that made them work that out from two
            // dates would be a summary of the wrong decision.
            var proposed = ReadAmendment(context);

            var handedBack = proposed.ActualReturnDate.HasValue
                ? proposed.ActualReturnDate.Value.ToString("dd MMM yyyy")
                : "-";

            return new WorkflowSummary(
                $"{labelEn} returned {handedBack} (out {withdrawn})",
                $"إرجاع {labelAr} بتاريخ {handedBack} (الاستلام {withdrawn})");
        }

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
                        // A return rides the same request type. Closing one here would cancel a pending
                        // hand-back because somebody withdrew a different document -- legacy closed only
                        // withdrawal requests.
                        && !r.IsAmendment
                        && r.Id != thisRequestId)
            .ToList();

        foreach (var request in superseded)
        {
            request.RequestStatus = RequestStatus.Closed;
            await _requestRepository.UpdateAsync(request);
        }

        await CloseSupersededMovementsAsync(movement);
    }

    /// <summary>
    /// Leaves one open movement per document, as legacy did.
    ///
    /// <para>
    /// Legacy's approval step looked for the document's still-open movement and overwrote its dates rather
    /// than inserting a second one (<c>EmployeeDocMvmtRequest.OnSaving</c>), so an extension moved the
    /// existing period instead of stacking another on top of it. Here the newly approved row already carries
    /// the later dates, so the same end state is reached by removing the movement it replaces. Without this,
    /// one document has two movements with no return date: it reads as out even after it is handed back, and
    /// the "already out" check would refuse every future withdrawal.
    /// </para>
    /// </summary>
    private async Task CloseSupersededMovementsAsync(EmployeeDocumentMvmt movement)
    {
        if (movement == null)
        {
            return;
        }

        var replaced = Repository.GetAll()
            .Where(m => m.EmployeeDocumentId == movement.EmployeeDocumentId
                        && m.Id != movement.Id
                        && m.ActualReturnDate == default)
            .ToList();

        foreach (var old in replaced)
        {
            await Repository.DeleteAsync(old);
        }
    }
}
