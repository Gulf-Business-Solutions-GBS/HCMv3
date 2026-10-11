using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyDocumentAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyDocuments)]
public class MyDocumentAppService : MyTransactionAppServiceBase<EmployeeDocument>, IMyDocumentAppService
{
    private readonly IEmployeeDocumentDuplicateCheck _duplicateCheck;
    private readonly IRepository<DocumentType> _documentTypeRepository;
    private readonly IRepository<EmployeeDocumentMvmt> _movementRepository;
    private readonly IWithdrawableDocumentTypeResolver _withdrawableDocumentTypeResolver;

    public MyDocumentAppService(
        IRepository<EmployeeDocument> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IEmployeeDocumentDuplicateCheck duplicateCheck,
        IRepository<DocumentType> documentTypeRepository,
        IRepository<EmployeeDocumentMvmt> movementRepository,
        IWithdrawableDocumentTypeResolver withdrawableDocumentTypeResolver)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _duplicateCheck = duplicateCheck;
        _documentTypeRepository = documentTypeRepository;
        _movementRepository = movementRepository;
        _withdrawableDocumentTypeResolver = withdrawableDocumentTypeResolver;
    }

    protected override string RequestType => DocumentWorkflowTransactionHandler.DocumentRequestType;

    protected override string RequestNumberPrefix => "DOC";

    [HttpPost]
    public async Task<PagedResultDto<MyDocumentRequestDto>> GetMyDocuments(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = System.DateTime.Today;

            var query = MyRowsQuery(employeeId)
                .Include(d => d.DocumentType)
                // A type HR keeps out of the employee's view stays out of it here.
                .Where(d => !d.DocumentType.IsHiddenFromEmployee)
                // An expired document is history; one still in flight is not, whatever its dates say. Nor is
                // a draft or a request sent back: either may hold no expiry date at all, which reads as long
                // expired, and it is the row the employee came back to finish.
                .WhereIf(!input.IncludeHistory,
                    d => d.ExpiryDate >= today
                         || d.WorkflowStatus == RequestStatus.Open
                         || d.WorkflowStatus == RequestStatus.Draft);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            // One read for the page: the request numbers, and whether a Draft row is one that was sent back.
            var requests = await GetOwnRequestsAsync(items);

            var dtos = items.Select(d => ToDto(d, requests)).ToList();

            await FillStandingAsync(employeeId, items, dtos, today);

            return new PagedResultDto<MyDocumentRequestDto>(totalCount, dtos);
        }
    }

    /// <summary>
    /// What each document on the page is today -- active, expired or superseded, in the safe or out -- and
    /// what the employee may do with it. Only for documents in effect: a draft or one awaiting approval has
    /// no standing yet. Three reads for the page, whatever its length.
    /// </summary>
    private async Task FillStandingAsync(
        int employeeId,
        IReadOnlyList<EmployeeDocument> items,
        IReadOnlyList<MyDocumentRequestDto> dtos,
        System.DateTime today)
    {
        if (items.Count == 0)
        {
            return;
        }

        // Every document of the employee's that is in effect, not only this page's: "a newer one exists"
        // is a question about all of them.
        var inEffect = (await Repository.GetAll()
                .Where(d => d.EmployeeId == employeeId)
                .Select(d => new { d.Id, d.DocumentTypeId, d.IssueDate, d.WorkflowStatus })
                .ToListAsync())
            .Where(d => IsInEffect(d.WorkflowStatus))
            .ToList();

        var documentIds = items.Select(d => d.Id).ToList();

        var outIds = (await _movementRepository.GetAll()
                .Where(m => documentIds.Contains(m.EmployeeDocumentId) && m.ActualReturnDate == default(System.DateTime))
                .Select(m => new { m.EmployeeDocumentId, m.WorkflowStatus })
                .ToListAsync())
            .Where(m => IsInEffect(m.WorkflowStatus))
            .Select(m => m.EmployeeDocumentId)
            .ToHashSet();

        var withdrawableTypeIds = await _withdrawableDocumentTypeResolver.ResolveDocumentTypeIdsAsync();

        var typeIds = items.Select(d => d.DocumentTypeId).Distinct().ToList();

        var recordableTypeIds = (await _documentTypeRepository.GetAll()
                .Where(DocumentTypeEssRules.CanBeRecordedByEmployee)
                .Where(t => typeIds.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync())
            .ToHashSet();

        for (var i = 0; i < items.Count; i++)
        {
            var document = items[i];
            var dto = dtos[i];

            if (!IsInEffect(document.WorkflowStatus))
            {
                continue;
            }

            var latestOther = inEffect
                .Where(d => d.Id != document.Id && d.DocumentTypeId == document.DocumentTypeId)
                .Select(d => (System.DateTime?)d.IssueDate)
                .Max();

            dto.DocumentStatus = DocumentStatusCalculator.Calculate(
                document.IssueDate, document.ExpiryDate, latestOther, today);

            dto.IsOut = outIds.Contains(document.Id);

            // Legacy withdrew "the active one". A superseded or expired document is not handed out.
            dto.CanWithdraw = dto.DocumentStatus == DocumentStatus.Active
                              && (withdrawableTypeIds == null || withdrawableTypeIds.Contains(document.DocumentTypeId));

            dto.CanRenew = recordableTypeIds.Contains(document.DocumentTypeId);
        }
    }

    /// <summary>Recorded by HR, approved, or closed -- the three states the <c>ApprovedOnly</c> filter lets through.</summary>
    private static bool IsInEffect(RequestStatus? status)
    {
        return status is null or RequestStatus.Approved or RequestStatus.Closed;
    }

    /// <summary>
    /// The type must be one an employee may record a document of -- the rule the dropdown is filled by,
    /// applied again because a filtered dropdown is not a check.
    /// </summary>
    private async Task EnsureCanBeRecordedAsync(int documentTypeId)
    {
        var documentTypeExists = await _documentTypeRepository.GetAll()
            .AnyAsync(t => t.Id == documentTypeId);

        if (!documentTypeExists)
        {
            throw new UserFriendlyException(L("DocumentTypeNotFound"));
        }

        var canBeRecorded = await _documentTypeRepository.GetAll()
            .Where(DocumentTypeEssRules.CanBeRecordedByEmployee)
            .AnyAsync(t => t.Id == documentTypeId);

        if (!canBeRecorded)
        {
            throw new UserFriendlyException(L("ThatDocumentTypeCannotBeRecordedByAnEmployee"));
        }
    }

    public async Task<MyRequestSubmissionDto> RequestDocument(RequestDocumentInput input)
    {
        await EnsureCanBeRecordedAsync(input.DocumentTypeId);

        // The handler enforces this too. Checked here as well so the employee is told before the row is
        // created rather than by a rollback, which reads like a system fault rather than a missing field.
        if (input.DocumentNum.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("DocumentNumberIsRequired"));
        }

        // Checked here too because with workflow off for documents no handler runs.
        await _duplicateCheck.EnsureNotDuplicateAsync(
            await GetMyEmployeeIdAsync(), input.DocumentTypeId, input.DocumentNum, null);

        return await CreateAndSubmitAsync(
            employeeId => new EmployeeDocument
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                DocumentTypeId = input.DocumentTypeId,
                DocumentNum = input.DocumentNum,
                IssueDate = input.IssueDate.Date,
                ExpiryDate = input.ExpiryDate.Date,
                Comments = input.Comments
            },
            input.DocumentTypeId);
    }

    public async Task<MyRequestSubmissionDto> SaveMyDocumentDraft(SaveDocumentDraftInput input)
    {
        // The one thing a draft is held to: the row cannot be stored without a document type, and it must be
        // one an employee may record. The number, the dates and the duplicate check are rules about what is
        // submitted, and a draft is what somebody saves because they cannot satisfy those yet.
        await EnsureCanBeRecordedAsync(input.DocumentTypeId);

        if (input.Id.HasValue)
        {
            await UpdateEditableAsync(input.Id.Value, document => Apply(input, document));

            return new MyRequestSubmissionDto
            {
                TransactionId = input.Id.Value,
                SubmittedForApproval = false,
                SavedAsDraft = true
            };
        }

        return await SaveDraftAsync(employeeId =>
        {
            var document = new EmployeeDocument
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId
            };

            Apply(input, document);

            return document;
        });
    }

    public async Task<MyDocumentRequestDto> GetMyDocumentDraftForEdit(EntityDto input)
    {
        var document = await GetMyEditableAsync(input.Id);

        var dto = ToDto(document, await GetOwnRequestsAsync(new[] { document }));

        var documentType = await _documentTypeRepository.GetAll()
            .Where(t => t.Id == document.DocumentTypeId)
            .Select(t => new { t.NameEn, t.NameAr })
            .FirstOrDefaultAsync();

        dto.DocumentTypeNameEn = documentType?.NameEn;
        dto.DocumentTypeNameAr = documentType?.NameAr;

        return dto;
    }

    public Task<MyRequestSubmissionDto> SubmitMyDocumentDraft(EntityDto input)
    {
        return SubmitEditableAsync(input.Id, async document =>
        {
            // Again, not only at save: the type's switches may have changed since the draft was filed.
            await EnsureCanBeRecordedAsync(document.DocumentTypeId);

            // The two checks the one-step request makes, against what the row now holds. The handler makes
            // them as well, but with workflow off for documents no handler runs.
            if (document.DocumentNum.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException(L("DocumentNumberIsRequired"));
            }

            // Excluding the row itself: it is the document being submitted, not a duplicate of it.
            await _duplicateCheck.EnsureNotDuplicateAsync(
                document.EmployeeId, document.DocumentTypeId, document.DocumentNum, document.Id);

            return document.DocumentTypeId;
        });
    }

    public Task CancelMyDocument(EntityDto input) => CancelAsync(input.Id);

    /// <summary>
    /// Copies the form onto the document. Missing dates stay unset rather than becoming today's: a draft
    /// with no expiry date must not turn into a document that expires today.
    /// </summary>
    private static void Apply(SaveDocumentDraftInput input, EmployeeDocument document)
    {
        document.DocumentTypeId = input.DocumentTypeId;
        document.DocumentNum = input.DocumentNum;
        document.IssueDate = input.IssueDate?.Date ?? default;
        document.ExpiryDate = input.ExpiryDate?.Date ?? default;
        document.Comments = input.Comments;
    }

    private static MyDocumentRequestDto ToDto(
        EmployeeDocument entity,
        IReadOnlyDictionary<int, WorkflowRequestInfo> requests)
    {
        var isDraft = IsDraft(entity);
        var isSentBack = IsSentBack(entity, requests);

        return new MyDocumentRequestDto
        {
            Id = entity.Id,
            DocumentTypeId = entity.DocumentTypeId,
            DocumentTypeNameEn = entity.DocumentType?.NameEn,
            DocumentTypeNameAr = entity.DocumentType?.NameAr,
            DocumentNum = entity.DocumentNum,
            IssueDate = entity.IssueDate,
            ExpiryDate = entity.ExpiryDate,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity),
            RequestNumber = RequestNumberOf(entity, requests),
            IsDraft = isDraft,
            IsSentBack = isSentBack,
            CanEdit = isDraft || isSentBack
        };
    }
}
