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
            // happened yet, plus anything still waiting on an approver.
            var query = MyRowsQuery(employeeId)
                .WhereIf(!input.IncludeHistory,
                    p => p.ExitDate >= today || p.WorkflowStatus == RequestStatus.Open);

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

    public Task CancelMyExitPermit(EntityDto input) => CancelAsync(input.Id);

    private static MyExitPermitDto ToDto(EmployeeExitPermit entity)
    {
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
            CanCancel = CanCancel(entity)
        };
    }
}
