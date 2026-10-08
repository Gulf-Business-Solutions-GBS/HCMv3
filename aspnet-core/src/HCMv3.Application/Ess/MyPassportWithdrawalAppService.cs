using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Timing;
using Abp.UI;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.SystemSecurity.Services;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyPassportWithdrawalAppService"/>
///
/// <remarks>
/// The type where the single-row design saves the most. Legacy kept a <c>PassportWithdrawalRequest</c> table
/// next to <c>EmployeeDocumentMvmt</c> and copied one into the other at the final approval step, with the
/// copy having to decide whether it was updating an existing open movement or inserting a new one. Every one
/// of those failure modes -- an approved request with no movement row, a movement row for a request that was
/// later rejected, two rows for one withdrawal -- disappears when the movement row IS the request and is
/// merely hidden until approved.
///
/// <para>
/// The document is re-checked as the employee's own before anything is created. Withdrawing a passport is
/// physically handing over an original document, so the one thing this endpoint must never allow is raising
/// a request against somebody else's document id.
/// </para>
/// </remarks>
[AbpAuthorize(AppPermissions.Ess_MyPassportWithdrawals)]
public class MyPassportWithdrawalAppService : MyTransactionAppServiceBase<EmployeeDocumentMvmt>, IMyPassportWithdrawalAppService
{
    /// <summary>Tenant config key for the document type employees may withdraw (legacy's JSON setting).</summary>
    public const string WithdrawableDocumentTypeCodeKey = WithdrawableDocumentTypeResolver.DocumentTypeCodeKey;

    /// <summary>Legacy's default: document type code "2", the passport.</summary>
    public const string DefaultWithdrawableDocumentTypeCode = WithdrawableDocumentTypeResolver.DefaultDocumentTypeCode;

    private readonly IRepository<EmployeeDocument> _documentRepository;
    private readonly IRepository<DocumentType> _documentTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<WorkflowRequest> _requestRepository;
    private readonly INotificationDispatchService _notificationDispatchService;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IWorkflowAmendmentService _workflowAmendmentService;
    private readonly IWithdrawableDocumentTypeResolver _withdrawableDocumentTypeResolver;
    private readonly IDocumentMovementExtensionReader _extensionReader;

    public MyPassportWithdrawalAppService(
        IRepository<EmployeeDocumentMvmt> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IRepository<EmployeeDocument> documentRepository,
        IRepository<DocumentType> documentTypeRepository,
        IRepository<Employee> employeeRepository,
        INotificationDispatchService notificationDispatchService,
        ISysSettingsAccessor sysSettingsAccessor,
        IWorkflowAmendmentService workflowAmendmentService,
        IWithdrawableDocumentTypeResolver withdrawableDocumentTypeResolver,
        IDocumentMovementExtensionReader extensionReader)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _withdrawableDocumentTypeResolver = withdrawableDocumentTypeResolver;
        _extensionReader = extensionReader;
        _documentRepository = documentRepository;
        _documentTypeRepository = documentTypeRepository;
        _employeeRepository = employeeRepository;
        _requestRepository = requestRepository;
        _notificationDispatchService = notificationDispatchService;
        _sysSettingsAccessor = sysSettingsAccessor;
        _workflowAmendmentService = workflowAmendmentService;
    }

    protected override string RequestType => PassportWithdrawalWorkflowTransactionHandler.PassportWithdrawalRequestType;

    protected override string RequestNumberPrefix => "PPW";

    [HttpPost]
    public async Task<PagedResultDto<MyPassportWithdrawalDto>> GetMyPassportWithdrawals(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = DateTime.Today;
            var notReturned = default(DateTime);

            // "Not history" means the document is still out, or still due back, or the request has not been
            // decided. A plain date cutoff would hide the row that matters most: a passport withdrawn months
            // ago and never handed back.
            var query = MyRowsQuery(employeeId)
                .Include(m => m.EmployeeDocument)
                .ThenInclude(d => d.DocumentType)
                .Include(m => m.Guarantor)
                .WhereIf(!input.IncludeHistory,
                    m => m.ActualReturnDate == notReturned
                         || m.ReturnDate >= today
                         || m.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            // A superseded withdrawal keeps its row Open by design while its REQUEST was closed when the
            // extension was approved. Without the request's own status the screen would keep offering
            // "Withdraw request" on something nobody can act on any more.
            var requestIds = items.Where(m => m.WorkflowRequestId.HasValue)
                .Select(m => m.WorkflowRequestId.Value)
                .Distinct()
                .ToList();

            var ownRequests = await _requestRepository.GetAll()
                .Where(r => requestIds.Contains(r.Id))
                .Select(r => new { r.Id, r.RequestStatus, r.RequestNumber })
                .ToListAsync();

            var requestStatuses = ownRequests.ToDictionary(r => r.Id, r => r.RequestStatus);
            var requestNumbers = ownRequests.ToDictionary(r => r.Id, r => r.RequestNumber);

            // A return leaves the movement row exactly as it was until it is approved, so nothing on the row
            // says one is waiting. Without this the screen goes on offering "Return" and the second attempt
            // is only refused on submit. One read for the page, the same test the amendment service applies.
            var movementIds = items.Select(m => m.Id).ToList();

            var pendingReturns = (await _requestRepository.GetAll()
                    .Where(r => r.IsAmendment
                                && r.RequestType == RequestType
                                && r.TargetEntityId.HasValue
                                && movementIds.Contains(r.TargetEntityId.Value)
                                && (r.RequestStatus == RequestStatus.Open || r.RequestStatus == RequestStatus.Draft))
                    .Select(r => new { r.Id, MovementId = r.TargetEntityId.Value })
                    .ToListAsync())
                .GroupBy(r => r.MovementId)
                .ToDictionary(g => g.Key, g => g.Max(r => r.Id));

            var chains = await _extensionReader.GetChainsAsync(items);

            var dtos = items.Select(m =>
            {
                var dto = ToDto(m, requestStatuses);

                dto.RequestNumber = m.WorkflowRequestId.HasValue && requestNumbers.TryGetValue(m.WorkflowRequestId.Value, out var number)
                    ? number
                    : null;
                dto.IsOverdue = m.IsOverdueOn(today);

                if (pendingReturns.TryGetValue(m.Id, out var pendingReturnId))
                {
                    dto.HasPendingReturn = true;
                    dto.PendingReturnRequestId = pendingReturnId;
                    dto.CanReturn = false;
                }

                dto.Extensions = chains[m.Id].Select(x => new DocumentMovementExtensionDto
                {
                    Id = x.Id,
                    WithdrawDate = x.WithdrawDate,
                    ReturnDate = x.ReturnDate,
                    RequestNumber = x.RequestNumber,
                    SupersededOn = x.SupersededOn
                }).ToList();

                return dto;
            }).ToList();

            return new PagedResultDto<MyPassportWithdrawalDto>(totalCount, dtos);
        }
    }

    public async Task<MyRequestSubmissionDto> RequestPassportWithdrawal(RequestPassportWithdrawalInput input)
    {
        // Checked here as well as in the handler. The handler is the rule -- it applies to HR-raised rows
        // too -- but failing here means the employee is told before a row is created and rolled back.
        if (input.WithdrawDate == default)
        {
            throw new UserFriendlyException(L("WithdrawDateIsRequired"));
        }

        if (input.ReturnDate == default)
        {
            throw new UserFriendlyException(L("ReturnDateIsRequired"));
        }

        if (input.ReturnDate.Date < input.WithdrawDate.Date)
        {
            throw new UserFriendlyException(L("ReturnDateCannotBeEarlierThanWithdrawDate"));
        }

        var employeeId = await GetMyEmployeeIdAsync();

        // Scoped to the caller, not merely looked up. A document id belonging to another employee must read
        // as "not found" rather than as a valid target -- this endpoint hands over original documents.
        var document = await _documentRepository.GetAll()
            .Where(d => d.Id == input.EmployeeDocumentId && d.EmployeeId == employeeId)
            .Select(d => new { d.Id, d.DocumentTypeId })
            .FirstOrDefaultAsync();

        if (document == null)
        {
            throw new UserFriendlyException(L("EmployeeDocumentNotFound"));
        }

        // Legacy only ever offered the passport: it resolved one document type by code (its setting
        // defaulted to "2") and took the employee's active document of that type. The code is a tenant
        // config value here; with none configured the default stands, and if no such type exists the
        // restriction is not applied rather than blocking every request.
        var withdrawableTypeId = await ResolveWithdrawableDocumentTypeIdAsync();
        if (withdrawableTypeId.HasValue && document.DocumentTypeId != withdrawableTypeId.Value)
        {
            throw new UserFriendlyException(L("OnlyTheWithdrawableDocumentTypeCanBeWithdrawn"));
        }

        if (input.WithdrawalReason.IsNullOrWhiteSpace())
        {
            var settings = await _sysSettingsAccessor.GetSelfServiceSettingsAsync();

            if (settings != null && settings.IsWithdrawalReasonMandatory)
            {
                throw new UserFriendlyException(L("WithdrawalReasonIsRequired"));
            }
        }

        if (input.GuarantorId.HasValue)
        {
            var guarantorExists = await _employeeRepository.GetAll()
                .AnyAsync(e => e.Id == input.GuarantorId.Value);

            if (!guarantorExists)
            {
                throw new UserFriendlyException(L("EmployeeNotFound"));
            }
        }

        // Deliberately NO "the document is already out" refusal. Legacy's block for it
        // (PassportWithdrawalRequestController, ~264-277) is commented out: a request for a document that is
        // already out is an EXTENSION -- the employee who needs it a week longer -- and on its final approval
        // it replaces the open movement's dates and closes the employee's other open withdrawal requests
        // (see PassportWithdrawalWorkflowTransactionHandler.OnApprovedAsync).

        var submission = await CreateAndSubmitAsync(
            meId => new EmployeeDocumentMvmt
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = meId,
                EmployeeDocumentId = document.Id,
                WithdrawDate = input.WithdrawDate.Date,
                ReturnDate = input.ReturnDate.Date,

                // Left at default: the document has not come back yet, and it is default(DateTime) rather
                // than a nullable column because that is how the movement log has always recorded "still
                // out" (see EmployeeDocumentMvmt.ActualReturnDate).
                ActualReturnDate = default,

                GuarantorId = input.GuarantorId,
                WithdrawalReason = input.WithdrawalReason?.Trim(),
                Comments = input.Comments
            },
            // Routing by document type, so a passport can need a longer chain than a certificate.
            document.DocumentTypeId);

        if (input.GuarantorId.HasValue)
        {
            // Legacy mailed the guarantor as the request was raised. Deliberately after the request exists
            // and never allowed to fail it: a mail server that is down must not cost the employee their
            // request, and the guarantor is told again by the approval chain regardless.
            try
            {
                var requestNumber = submission.WorkflowRequestId.HasValue
                    ? await _requestRepository.GetAll()
                        .Where(r => r.Id == submission.WorkflowRequestId.Value)
                        .Select(r => r.RequestNumber)
                        .FirstOrDefaultAsync()
                    : null;

                await _notificationDispatchService.SendPassportGuarantorNotificationAsync(
                    input.GuarantorId.Value, employeeId, requestNumber);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not notify the guarantor of passport withdrawal {submission.TransactionId}.", ex);
            }
        }

        return submission;
    }

    public Task CancelMyPassportWithdrawal(EntityDto input) => CancelAsync(input.Id);

    /// <summary>
    /// The document type employees may ask to withdraw: the type whose code matches the tenant config
    /// <c>PassportWithdrawalDocumentTypeCode</c>, defaulting to legacy's "2". Null when no type carries that
    /// code, which leaves any document withdrawable rather than none.
    /// </summary>
    private Task<int?> ResolveWithdrawableDocumentTypeIdAsync()
    {
        // One resolver for this check and for the dropdown that offers the documents, so the two cannot
        // disagree about which type is withdrawable.
        return _withdrawableDocumentTypeResolver.ResolveDocumentTypeIdAsync();
    }

    /// <summary>
    /// The RETURN leg. Raises an amendment against the approved movement rather than creating anything:
    /// stamping the actual return date is a change to a row that is already live, and routing it through the
    /// same chain the withdrawal went through means a refused return leaves the movement exactly as it was
    /// -- the document simply stays recorded as out, which is the safe reading when nobody has confirmed it
    /// is back.
    /// </summary>
    public async Task<ReturnDocumentResultDto> ReturnMyDocument(ReturnDocumentInput input)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        EmployeeDocumentMvmt movement;

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            movement = await Repository.FirstOrDefaultAsync(m => m.Id == input.EmployeeDocumentMvmtId);
        }

        if (movement == null)
        {
            throw new UserFriendlyException(L("RequestNotFound"));
        }

        // Ownership re-checked on the write, exactly as CancelAsync and AmendMyLeave do: a scoped list
        // paired with an unscoped write is how one employee ends up returning another's passport on paper
        // while it is still in their drawer.
        if (movement.EmployeeId != employeeId)
        {
            throw new AbpAuthorizationException(L("YouCanOnlyCancelYourOwnRequests"));
        }

        // Only an approved withdrawal can be returned from. While it is still Open the document was never
        // released, so there is nothing to hand back; the employee should withdraw the request instead.
        if (movement.WorkflowStatus != RequestStatus.Approved)
        {
            throw new UserFriendlyException(L("OnlyAnApprovedWithdrawalCanBeReturned"));
        }

        // Checked here as well as in the handler. The handler is the rule -- it applies to an HR-raised
        // return too -- but failing here means the employee is told before a request is created and rolled
        // back.
        if (movement.ActualReturnDate != default)
        {
            throw new UserFriendlyException(L("ThisDocumentHasAlreadyBeenReturned"));
        }

        var actualReturnDate = input.ActualReturnDate.Date;

        // A return reports something that has happened. A date still to come is a promise, and the movement
        // log would record the document as back in the safe on a day that has not arrived.
        if (actualReturnDate > Clock.Now.Date)
        {
            throw new UserFriendlyException(L("ActualReturnDateCannotBeInTheFuture"));
        }

        if (actualReturnDate < movement.WithdrawDate.Date)
        {
            throw new UserFriendlyException(L("ActualReturnDateCannotBeEarlierThanWithdrawDate"));
        }

        var documentTypeId = await _documentRepository.GetAll()
            .Where(d => d.Id == movement.EmployeeDocumentId)
            .Select(d => (int?)d.DocumentTypeId)
            .FirstOrDefaultAsync();

        var amendment = new DocumentMvmtAmendment
        {
            ActualReturnDate = actualReturnDate,
            ReturnRemarks = input.ReturnRemarks.IsNullOrWhiteSpace() ? null : input.ReturnRemarks.Trim()
        };

        var requestId = await _workflowAmendmentService.CreateAndSubmitAsync(
            PassportWithdrawalWorkflowTransactionHandler.PassportWithdrawalRequestType,
            movement.Id,
            movement.EmployeeId,
            JsonSerializer.Serialize(amendment),
            employeeId,
            // Routed by document type, as the withdrawal was, so a return of a passport can follow the same
            // chain the withdrawal of that passport followed.
            documentTypeId,
            // Labelled as what it is, as a return from leave is, so a list can say "return" without opening
            // the request and a chain can differ by action. Returns stored under the generic amendment label
            // still apply: the handler reads the payload, not the label.
            WorkflowRequestAction.Return);

        return new ReturnDocumentResultDto
        {
            EmployeeDocumentMvmtId = movement.Id,
            WorkflowRequestId = requestId,
            ActualReturnDate = actualReturnDate,
            IsLate = actualReturnDate > movement.ReturnDate.Date
        };
    }

    private static MyPassportWithdrawalDto ToDto(
        EmployeeDocumentMvmt entity,
        IReadOnlyDictionary<int, RequestStatus> requestStatuses = null)
    {
        // Withdrawable only while the row is undecided AND its request is still open. The second half is
        // what a superseded extension fails.
        var requestStillOpen = !entity.WorkflowRequestId.HasValue
                               || requestStatuses == null
                               || !requestStatuses.TryGetValue(entity.WorkflowRequestId.Value, out var requestStatus)
                               || requestStatus == RequestStatus.Draft
                               || requestStatus == RequestStatus.Open;

        return new MyPassportWithdrawalDto
        {
            Id = entity.Id,
            EmployeeDocumentId = entity.EmployeeDocumentId,
            DocumentNum = entity.EmployeeDocument?.DocumentNum,
            DocumentTypeId = entity.EmployeeDocument?.DocumentTypeId,
            DocumentTypeNameEn = entity.EmployeeDocument?.DocumentType?.NameEn,
            DocumentTypeNameAr = entity.EmployeeDocument?.DocumentType?.NameAr,
            WithdrawDate = entity.WithdrawDate,
            ReturnDate = entity.ReturnDate,
            ActualReturnDate = entity.ActualReturnDate == default ? (DateTime?)null : entity.ActualReturnDate,
            GuarantorId = entity.GuarantorId,
            GuarantorNameEn = entity.Guarantor?.FullNameEn,
            GuarantorNameAr = entity.Guarantor?.FullNameAr,
            WithdrawalReason = entity.WithdrawalReason,
            Comments = entity.Comments,
            ActualWithdrawDate = entity.ActualWithdrawDate,
            ReturnRemarks = entity.ReturnRemarks,
            IssueDate = entity.EmployeeDocument?.IssueDate,
            ExpiryDate = entity.EmployeeDocument?.ExpiryDate,
            ExtendsMovementId = entity.ExtendsMovementId,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity) && requestStillOpen,

            // Only an approved withdrawal that has not already been handed back. Anything else and the
            // button would either be offering to return a document that was never released, or inviting a
            // second return date over the first.
            CanReturn = entity.WorkflowStatus == RequestStatus.Approved && entity.ActualReturnDate == default
        };
    }
}
