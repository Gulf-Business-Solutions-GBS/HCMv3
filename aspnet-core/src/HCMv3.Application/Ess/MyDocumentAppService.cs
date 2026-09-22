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

    public MyDocumentAppService(
        IRepository<EmployeeDocument> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IEmployeeDocumentDuplicateCheck duplicateCheck)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _duplicateCheck = duplicateCheck;
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
                // An expired document is history; one still in flight is not, whatever its dates say.
                .WhereIf(!input.IncludeHistory, d => d.ExpiryDate >= today || d.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyDocumentRequestDto>(totalCount, items.Select(ToDto).ToList());
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

    public Task CancelMyDocument(EntityDto input) => CancelAsync(input.Id);

    private static MyDocumentRequestDto ToDto(EmployeeDocument entity)
    {
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
            CanCancel = CanCancel(entity)
        };
    }
}
