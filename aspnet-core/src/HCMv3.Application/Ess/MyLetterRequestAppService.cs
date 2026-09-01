using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
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

            return new PagedResultDto<MyLetterRequestDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public async Task<MyRequestSubmissionDto> RequestLetter(RequestLetterInput input)
    {
        // Checked before a row is created, because the alternative is an employee requesting a "letter" of
        // type Passport by posting the id of one -- the dropdown is filtered client-side, and a filtered
        // dropdown is not a rule. The handler cannot make this check on the employee's behalf either: an
        // HR-entered row against a non-letter type is an admin's deliberate choice, not a mistake.
        var isLetterType = await _documentTypeRepository.GetAll()
            .AnyAsync(t => t.Id == input.DocumentTypeId && t.IsLetterType);

        if (!isLetterType)
        {
            throw new UserFriendlyException(L("ThatDocumentTypeIsNotIssuedAsALetter"));
        }

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

    public Task CancelMyLetterRequest(EntityDto input) => CancelAsync(input.Id);

    private static MyLetterRequestDto ToDto(EmployeeLetterRequest entity)
    {
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
            CanPrint = CanPrint(entity)
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
