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

/// <inheritdoc cref="IMyLetterRequestAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyLetterRequests)]
public class MyLetterRequestAppService : MyTransactionAppServiceBase<EmployeeLetterRequest>, IMyLetterRequestAppService
{
    private readonly IRepository<DocumentType> _documentTypeRepository;

    public MyLetterRequestAppService(
        IRepository<EmployeeLetterRequest> repository,
        IRepository<DocumentType> documentTypeRepository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _documentTypeRepository = documentTypeRepository;
    }

    protected override string RequestType => LetterRequestWorkflowTransactionHandler.LetterRequestType;

    protected override string RequestNumberPrefix => "LTR";

    [HttpPost]
    public async Task<PagedResultDto<MyLetterRequestDto>> GetMyLetterRequests(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        // Own in-flight rows must be visible -- that is what the employee came here to see, and for this
        // type it is the whole point of the screen: they are waiting to be told the letter is ready.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // No date-based "current vs history" split here, unlike leave or exit permits. A letter has no
            // period it is inside or outside of; what makes one interesting is whether it is still waiting.
            // So the default view is everything undecided, and IncludeHistory adds the settled ones.
            var query = MyRowsQuery(employeeId)
                .Include(l => l.DocumentType)
                .WhereIf(!input.IncludeHistory,
                    l => l.WorkflowStatus == RequestStatus.Open
                         || l.WorkflowStatus == RequestStatus.Draft
                         || l.WorkflowStatus == RequestStatus.Approved
                         || l.WorkflowStatus == null);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            // One read for the page: the request numbers, and whether a Draft row is one that was sent back.
            var requests = await GetOwnRequestsAsync(items);

            return new PagedResultDto<MyLetterRequestDto>(totalCount, items.Select(l => ToDto(l, requests)).ToList());
        }
    }

    public async Task<MyRequestSubmissionDto> RequestLetter(RequestLetterInput input)
    {
        // Checked before a row is created, because the alternative is an employee requesting a "letter" of
        // type Passport by posting the id of one -- the dropdown is filtered client-side, and a filtered
        // dropdown is not a rule. The handler cannot make this check on the employee's behalf either: an
        // HR-entered row against a non-letter type is an admin's deliberate choice, not a mistake.
        await EnsureLetterTypeAsync(input.DocumentTypeId);

        return await CreateAndSubmitAsync(
            employeeId => new EmployeeLetterRequest
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                DocumentTypeId = input.DocumentTypeId,
                EffectiveDate = input.EffectiveDate.Date,
                AddressToEn = input.AddressToEn?.Trim(),
                AddressToAr = input.AddressToAr?.Trim(),
                Comments = input.Comments
            },
            // Routed by document type: a salary certificate disclosing pay to a bank is not necessarily
            // signed off by whoever signs a to-whom-it-may-concern letter.
            input.DocumentTypeId);
    }

    public async Task<MyRequestSubmissionDto> SaveMyLetterRequestDraft(SaveLetterDraftInput input)
    {
        // The one rule a draft is held to. The row cannot be stored without a document type, and letting a
        // draft hold a passport's would put a non-letter in this table for as long as nobody submitted it.
        // The date and the addressees are what a draft is allowed to be missing; they are checked at submit.
        await EnsureLetterTypeAsync(input.DocumentTypeId);

        if (input.Id.HasValue)
        {
            await UpdateEditableAsync(input.Id.Value, letter => Apply(input, letter));

            return new MyRequestSubmissionDto
            {
                TransactionId = input.Id.Value,
                SubmittedForApproval = false,
                SavedAsDraft = true
            };
        }

        return await SaveDraftAsync(employeeId =>
        {
            var letter = new EmployeeLetterRequest
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId
            };

            Apply(input, letter);

            return letter;
        });
    }

    public async Task<MyLetterRequestDto> GetMyLetterRequestDraftForEdit(EntityDto input)
    {
        var letter = await GetMyEditableAsync(input.Id);

        var dto = ToDto(letter, await GetOwnRequestsAsync(new[] { letter }));

        var documentType = await _documentTypeRepository.GetAll()
            .Where(t => t.Id == letter.DocumentTypeId)
            .Select(t => new { t.NameEn, t.NameAr })
            .FirstOrDefaultAsync();

        dto.DocumentTypeNameEn = documentType?.NameEn;
        dto.DocumentTypeNameAr = documentType?.NameAr;

        return dto;
    }

    public Task<MyRequestSubmissionDto> SubmitMyLetterRequestDraft(EntityDto input)
    {
        return SubmitEditableAsync(input.Id, async letter =>
        {
            // Again, not only at save: the type may have stopped being a letter type since the draft was
            // filed, and a sent-back request is being sent under whatever it holds now.
            await EnsureLetterTypeAsync(letter.DocumentTypeId);

            // What RequestLetterInput's [Required] annotations refuse on the one-step path. The handler
            // refuses them as well, but with workflow off for letters no handler runs, and a draft is the
            // one way a row can reach this point without them.
            if (letter.EffectiveDate == default)
            {
                throw new UserFriendlyException(L("EffectiveDateIsRequired"));
            }

            if (letter.AddressToEn.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException(L("AddressToEnIsRequired"));
            }

            if (letter.AddressToAr.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException(L("AddressToArIsRequired"));
            }

            // Routed by document type, as a one-step request is.
            return letter.DocumentTypeId;
        });
    }

    public Task CancelMyLetterRequest(EntityDto input) => CancelAsync(input.Id);

    private async Task EnsureLetterTypeAsync(int documentTypeId)
    {
        // The rule the dropdown is filled by, so what is offered and what is accepted cannot differ.
        var isLetterType = await _documentTypeRepository.GetAll()
            .Where(DocumentTypeEssRules.CanBeRequestedAsLetter)
            .AnyAsync(t => t.Id == documentTypeId);

        if (!isLetterType)
        {
            throw new UserFriendlyException(L("ThatDocumentTypeIsNotIssuedAsALetter"));
        }
    }

    /// <summary>
    /// Copies the form onto the letter. A missing date stays unset rather than becoming today's, so a draft
    /// with no date cannot be submitted for a day nobody chose. The addressees become empty rather than
    /// null because both columns are NOT NULL, and stay that way -- empty reads as "not filled in yet" to the
    /// submit-time checks, which refuse blank and whitespace alike.
    /// </summary>
    private static void Apply(SaveLetterDraftInput input, EmployeeLetterRequest letter)
    {
        letter.DocumentTypeId = input.DocumentTypeId;
        letter.EffectiveDate = input.EffectiveDate?.Date ?? default;
        letter.AddressToEn = input.AddressToEn?.Trim() ?? string.Empty;
        letter.AddressToAr = input.AddressToAr?.Trim() ?? string.Empty;
        letter.Comments = input.Comments;
    }

    private static MyLetterRequestDto ToDto(
        EmployeeLetterRequest entity,
        IReadOnlyDictionary<int, WorkflowRequestInfo> requests)
    {
        var isDraft = IsDraft(entity);
        var isSentBack = IsSentBack(entity, requests);

        return new MyLetterRequestDto
        {
            Id = entity.Id,
            DocumentTypeId = entity.DocumentTypeId,
            DocumentTypeNameEn = entity.DocumentType?.NameEn,
            DocumentTypeNameAr = entity.DocumentType?.NameAr,
            EffectiveDate = entity.EffectiveDate,
            AddressToEn = entity.AddressToEn,
            AddressToAr = entity.AddressToAr,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity),
            CanPrint = CanPrint(entity),
            RequestNumber = RequestNumberOf(entity, requests),
            IsDraft = isDraft,
            IsSentBack = isSentBack,
            CanEdit = isDraft || isSentBack
        };
    }

    /// <summary>
    /// Approved and nothing else. See <see cref="MyLetterRequestDto.CanPrint"/> for why a null status --
    /// which every other query treats as effective -- deliberately does NOT count here.
    /// </summary>
    private static bool CanPrint(EmployeeLetterRequest entity)
    {
        return entity.WorkflowStatus == RequestStatus.Approved;
    }
}
