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

    public MyDocumentAppService(
        IRepository<EmployeeDocument> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IEmployeeDocumentDuplicateCheck duplicateCheck,
        IRepository<DocumentType> documentTypeRepository)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _duplicateCheck = duplicateCheck;
        _documentTypeRepository = documentTypeRepository;
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

            return new PagedResultDto<MyDocumentRequestDto>(totalCount, items.Select(d => ToDto(d, requests)).ToList());
        }
    }

    public async Task<MyRequestSubmissionDto> RequestDocument(RequestDocumentInput input)
    {
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
        // The one thing a draft is held to: the row cannot be stored without a document type that exists.
        // The number, the dates and the duplicate check are rules about what is submitted, and a draft is
        // what somebody saves because they cannot satisfy those yet.
        var documentTypeExists = await _documentTypeRepository.GetAll()
            .AnyAsync(t => t.Id == input.DocumentTypeId);

        if (!documentTypeExists)
        {
            throw new UserFriendlyException(L("DocumentTypeNotFound"));
        }

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
