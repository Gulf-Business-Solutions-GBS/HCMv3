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
using HCMv3.PersonnelAdministration.Services;
using HCMv3.TravelManagement;
using HCMv3.TravelManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyTravelClaimAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyTravelClaims)]
public class MyTravelClaimAppService : MyTransactionAppServiceBase<TravelFeedback>, IMyTravelClaimAppService
{
    public MyTravelClaimAppService(
        IRepository<TravelFeedback> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
    }

    protected override string RequestType => TravelClaimWorkflowTransactionHandler.TravelClaimRequestType;

    protected override string RequestNumberPrefix => "TRV";

    [HttpPost]
    public async Task<PagedResultDto<MyTravelClaimDto>> GetMyTravelClaims(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var thisYear = DateTime.Today.Year;

            // A claim is always for a trip that has happened, so a date cutoff would hide everything. The
            // current year is the useful default view.
            // Queried on the concrete column, not through MyRowsQuery: EmployeeId is nullable here (an applicant's
            // rows have none), and the shared helper binds to the non-nullable interface member.
            var query = Repository.GetAll().Where(t => t.EmployeeId == employeeId)
                .WhereIf(!input.IncludeHistory,
                    c => c.ClaimDate.Year >= thisYear || c.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyTravelClaimDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public Task<MyRequestSubmissionDto> RequestTravelClaim(RequestTravelClaimInput input)
    {
        if (input.ClaimAmount <= 0)
        {
            throw new UserFriendlyException(L("ClaimAmountMustBeGreaterThanZero"));
        }

        return CreateAndSubmitAsync(
            employeeId => new TravelFeedback
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                TravelId = input.TravelId,
                ClaimDate = input.ClaimDate.Date,
                ClaimAmount = input.ClaimAmount,
                Comments = input.Comments
            },
            // Claims route by trip where one is recorded, so a chain can differ by destination or cost
            // centre; null falls through to the type's default chain.
            input.TravelId);
    }

    public Task CancelMyTravelClaim(EntityDto input) => CancelAsync(input.Id);

    private static MyTravelClaimDto ToDto(TravelFeedback entity)
    {
        return new MyTravelClaimDto
        {
            Id = entity.Id,
            TravelId = entity.TravelId,
            ClaimDate = entity.ClaimDate,
            ClaimAmount = entity.ClaimAmount,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
