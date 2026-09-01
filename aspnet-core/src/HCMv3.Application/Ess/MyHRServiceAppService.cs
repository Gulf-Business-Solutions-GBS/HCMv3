using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
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

/// <inheritdoc cref="IMyHRServiceAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyHRServices)]
public class MyHRServiceAppService : MyTransactionAppServiceBase<EmployeeHRService>, IMyHRServiceAppService
{
    public MyHRServiceAppService(
        IRepository<EmployeeHRService> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
    }

    protected override string RequestType => HRServiceWorkflowTransactionHandler.HRServiceRequestType;

    protected override string RequestNumberPrefix => "HRS";

    [HttpPost]
    public async Task<PagedResultDto<MyHRServiceRequestDto>> GetMyHRServices(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        // Own in-flight rows must be visible -- that is what the employee came here to see.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = System.DateTime.Today;

            var query = MyRowsQuery(employeeId)
                .Include(s => s.HRServiceType)
                .WhereIf(!input.IncludeHistory,
                    s => s.ServiceDate >= today || s.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyHRServiceRequestDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public Task<MyRequestSubmissionDto> RequestHRService(RequestHRServiceInput input)
    {
        return CreateAndSubmitAsync(
            employeeId => new EmployeeHRService
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                HRServiceTypeId = input.HRServiceTypeId,
                ServiceDate = input.ServiceDate.Date,
                Amount = input.Amount,
                Comments = input.Comments
            },
            // Routed by service type: the chain that signs off a salary certificate is not necessarily the
            // one that signs off a loan-guarantee letter.
            input.HRServiceTypeId);
    }

    public Task CancelMyHRService(EntityDto input) => CancelAsync(input.Id);

    private static MyHRServiceRequestDto ToDto(EmployeeHRService entity)
    {
        return new MyHRServiceRequestDto
        {
            Id = entity.Id,
            HRServiceTypeId = entity.HRServiceTypeId,
            HRServiceTypeNameEn = entity.HRServiceType?.NameEn,
            HRServiceTypeNameAr = entity.HRServiceType?.NameAr,
            ServiceDate = entity.ServiceDate,
            Amount = entity.Amount,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
