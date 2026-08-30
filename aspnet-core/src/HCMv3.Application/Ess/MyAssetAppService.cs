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

/// <inheritdoc cref="IMyAssetAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyAssets)]
public class MyAssetAppService : MyTransactionAppServiceBase<EmployeeAsset>, IMyAssetAppService
{
    public MyAssetAppService(
        IRepository<EmployeeAsset> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
    }

    protected override string RequestType => AssetWorkflowTransactionHandler.AssetRequestType;

    protected override string RequestNumberPrefix => "AST";

    [HttpPost]
    public async Task<PagedResultDto<MyAssetRequestDto>> GetMyAssets(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        // Own in-flight rows must be visible -- that is what the employee came here to see.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = System.DateTime.Today;

            var query = MyRowsQuery(employeeId)
                .Include(a => a.Asset)
                .WhereIf(!input.IncludeHistory, a => a.EndDate >= today || a.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyAssetRequestDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public Task<MyRequestSubmissionDto> RequestAsset(RequestAssetInput input)
    {
        return CreateAndSubmitAsync(
            employeeId => new EmployeeAsset
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                AssetId = input.AssetId,
                StartDate = input.StartDate.Date,
                EndDate = input.EndDate.Date,
                Comments = input.Comments
            },
            // Assets route by the asset itself, since an approval chain for a laptop is not necessarily the
            // one for a vehicle.
            input.AssetId);
    }

    public Task CancelMyAsset(EntityDto input) => CancelAsync(input.Id);

    private static MyAssetRequestDto ToDto(EmployeeAsset entity)
    {
        return new MyAssetRequestDto
        {
            Id = entity.Id,
            AssetId = entity.AssetId,
            AssetNameEn = entity.Asset?.NameEn,
            AssetNameAr = entity.Asset?.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
