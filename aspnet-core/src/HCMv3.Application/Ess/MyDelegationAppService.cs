using System;
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
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyDelegationAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyDelegations)]
public class MyDelegationAppService : MyTransactionAppServiceBase<EmployeeDelegation>, IMyDelegationAppService
{
    public MyDelegationAppService(
        IRepository<EmployeeDelegation> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
    }

    protected override string RequestType => DelegationWorkflowTransactionHandler.DelegationRequestType;

    protected override string RequestNumberPrefix => "DEL";

    [HttpPost]
    public async Task<PagedResultDto<MyDelegationDto>> GetMyDelegations(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = DateTime.Today;

            // Cover is about a period that is coming up or running now, so the default view is anything not
            // yet finished plus anything still sitting with an approver.
            var query = MyRowsQuery(employeeId)
                .Include(d => d.DelegateTo)
                .WhereIf(!input.IncludeHistory,
                    d => d.EndDate >= today
                         || d.WorkflowStatus == RequestStatus.Draft
                         || d.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyDelegationDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public async Task<MyRequestSubmissionDto> RequestDelegation(RequestDelegationInput input)
    {
        if (input.EndDate.Date < input.StartDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var employeeId = await GetMyEmployeeIdAsync();

        // The early failure, before a row is created and rolled back. The handler refuses it too, because
        // this rule has to hold for an HR-raised delegation as well -- and a no-op that looks like cover is
        // worse than no cover, since nobody goes looking for it.
        if (input.DelegateToId == employeeId)
        {
            throw new UserFriendlyException(L("CannotDelegateToYourself"));
        }

        return await CreateAndSubmitAsync(
            me => new EmployeeDelegation
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = me,
                DelegateToId = input.DelegateToId,
                StartDate = input.StartDate.Date,
                EndDate = input.EndDate.Date,
                Comments = input.Comments
            },
            // No sub-type: a delegation routes the same way whoever is covering. Null falls through to the
            // type's default chain.
            null);
    }

    public Task CancelMyDelegation(EntityDto input) => CancelAsync(input.Id);

    private static MyDelegationDto ToDto(EmployeeDelegation entity)
    {
        var today = DateTime.Today;

        return new MyDelegationDto
        {
            Id = entity.Id,
            DelegateToId = entity.DelegateToId,
            DelegateToCode = entity.DelegateTo?.Code,
            DelegateToFullNameEn = entity.DelegateTo?.FullNameEn,
            DelegateToFullNameAr = entity.DelegateTo?.FullNameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity),

            // Effective (approved, closed, or recorded directly by HR with workflow off) AND inside its
            // dates. A pending delegation is never active, which is the whole point of routing it.
            IsCurrentlyActive =
                (entity.WorkflowStatus == null
                 || entity.WorkflowStatus == RequestStatus.Approved
                 || entity.WorkflowStatus == RequestStatus.Closed)
                && entity.StartDate.Date <= today
                && entity.EndDate.Date >= today
        };
    }
}
