using System;
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
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Workflow;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyExitPermitAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyExitPermits)]
public class MyExitPermitAppService : MyTransactionAppServiceBase<EmployeeExitPermit>, IMyExitPermitAppService
{
    public MyExitPermitAppService(
        IRepository<EmployeeExitPermit> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
    }

    protected override string RequestType => ExitPermitWorkflowTransactionHandler.ExitPermitRequestType;

    protected override string RequestNumberPrefix => "EXP";

    [HttpPost]
    public async Task<PagedResultDto<MyExitPermitDto>> GetMyExitPermits(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = DateTime.Today;

            // A permit is about a trip that is coming up, so the default view is the ones that have not
            // happened yet, plus anything still waiting on an approver -- and drafts, unconditionally. A
            // draft is allowed to have no exit date at all, so a date test would hide the very rows the
            // employee came back to the screen to finish.
            var query = MyRowsQuery(employeeId)
                .WhereIf(!input.IncludeHistory,
                    p => p.ExitDate >= today
                         || p.WorkflowStatus == RequestStatus.Open
                         || p.WorkflowStatus == RequestStatus.Draft);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyExitPermitDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public Task<MyRequestSubmissionDto> RequestExitPermit(RequestExitPermitInput input)
    {
        // The permit number is the only link between this record and the document the government issued.
        // The handler refuses it too; this is the early failure, before a row is created and rolled back.
        if (input.ExternalRequestNumber.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("ExitPermitNumberIsRequired"));
        }

        return CreateAndSubmitAsync(
            employeeId => new EmployeeExitPermit
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                ExternalRequestNumber = input.ExternalRequestNumber.Trim(),
                TravelReason = input.TravelReason,
                ExitDate = input.ExitDate.Date,
                StartDate = input.StartDate.Date,
                Comments = input.Comments
            },
            // No sub-type: an exit permit routes the same way whatever the reason for travel. Null falls
            // through to the type's default chain.
            null);
    }

    public async Task<MyRequestSubmissionDto> SaveMyExitPermitDraft(SaveExitPermitDraftInput input)
    {
        // Nothing is validated here, and that is the point: a draft is what an employee saves precisely
        // because they cannot complete it yet. The permit number, the exit date and the start date are all
        // checked at submit, by the handler, which is where they first matter to somebody other than the
        // requester.
        if (input.Id.HasValue)
        {
            await UpdateDraftAsync(input.Id.Value, permit => Apply(input, permit));

            return new MyRequestSubmissionDto
            {
                TransactionId = input.Id.Value,
                SubmittedForApproval = false,
                SavedAsDraft = true
            };
        }

        return await SaveDraftAsync(employeeId =>
        {
            var permit = new EmployeeExitPermit
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId
            };

            Apply(input, permit);

            return permit;
        });
    }

    public async Task<MyExitPermitDto> GetMyExitPermitDraftForEdit(EntityDto input)
    {
        var permit = await GetMyDraftAsync(input.Id);

        return ToDto(permit);
    }

    public Task<MyRequestSubmissionDto> SubmitMyExitPermitDraft(EntityDto input)
    {
        // No sub-type, the same as a one-step submission: an exit permit routes the same way whatever the
        // reason for travel.
        return SubmitDraftAsync(input.Id, null);
    }

    public Task CancelMyExitPermit(EntityDto input) => CancelAsync(input.Id);

    /// <summary>
    /// Copies the form onto the permit. Missing dates become an unset date rather than today's: a draft
    /// with no date must stay a draft with no date, and guessing one would let a permit be submitted for a
    /// day nobody chose.
    /// </summary>
    private static void Apply(SaveExitPermitDraftInput input, EmployeeExitPermit permit)
    {
        // Empty rather than null, because the column is NOT NULL and deliberately stays that way -- the
        // permit number is genuinely required of a finished record, and widening the column to let drafts
        // through would weaken the rule for every row to accommodate the one state that is allowed to be
        // incomplete. Empty reads as "not filled in yet" to the submit-time check, which refuses whitespace
        // and blank alike.
        permit.ExternalRequestNumber = input.ExternalRequestNumber?.Trim() ?? string.Empty;
        permit.TravelReason = input.TravelReason;
        permit.ExitDate = input.ExitDate?.Date ?? default;
        permit.StartDate = input.StartDate?.Date ?? default;
        permit.Comments = input.Comments;
    }

    private static MyExitPermitDto ToDto(EmployeeExitPermit entity)
    {
        var isDraft = IsDraft(entity);

        return new MyExitPermitDto
        {
            Id = entity.Id,
            ExternalRequestNumber = entity.ExternalRequestNumber,
            TravelReason = entity.TravelReason,
            ExitDate = entity.ExitDate,
            StartDate = entity.StartDate,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity),
            IsDraft = isDraft,
            CanEdit = isDraft
        };
    }
}
