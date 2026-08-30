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

/// <inheritdoc cref="IMyOvertimeAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyOvertime)]
public class MyOvertimeAppService : MyTransactionAppServiceBase<EmployeeOvertime>, IMyOvertimeAppService
{
    public MyOvertimeAppService(
        IRepository<EmployeeOvertime> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
    }

    protected override string RequestType => OvertimeWorkflowTransactionHandler.OvertimeRequestType;

    protected override string RequestNumberPrefix => "OT";

    [HttpPost]
    public async Task<PagedResultDto<MyOvertimeRequestDto>> GetMyOvertimes(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // Overtime is always claimed for a day that has already happened, so a date cutoff would hide
            // everything. History here means "already decided" instead.
            var query = MyRowsQuery(employeeId)
                .Include(o => o.OvertimeType)
                .WhereIf(!input.IncludeHistory,
                    o => o.WorkflowStatus == null
                         || o.WorkflowStatus == RequestStatus.Draft
                         || o.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyOvertimeRequestDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public Task<MyRequestSubmissionDto> RequestOvertime(RequestOvertimeInput input)
    {
        if (input.OvertimeHours <= 0)
        {
            throw new UserFriendlyException(L("OvertimeHoursMustBeGreaterThanZero"));
        }

        return CreateAndSubmitAsync(
            employeeId => new EmployeeOvertime
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                OvertimeTypeId = input.OvertimeTypeId,
                EffectiveDate = input.EffectiveDate.Date,
                OvertimeHours = input.OvertimeHours,
                ActualOvertimeHours = input.OvertimeHours,

                // Deliberately NOT pre-filled with what was claimed. Approved hours are what the approver
                // grants, and defaulting them to the claim would make an untouched form look like a
                // decision somebody made.
                ApprovedOvertimeHours = 0,

                Comments = input.Comments
            },
            input.OvertimeTypeId);
    }

    public Task CancelMyOvertime(EntityDto input) => CancelAsync(input.Id);

    private static MyOvertimeRequestDto ToDto(EmployeeOvertime entity)
    {
        return new MyOvertimeRequestDto
        {
            Id = entity.Id,
            OvertimeTypeId = entity.OvertimeTypeId,
            OvertimeTypeNameEn = entity.OvertimeType?.NameEn,
            OvertimeTypeNameAr = entity.OvertimeType?.NameAr,
            EffectiveDate = entity.EffectiveDate,
            OvertimeHours = entity.OvertimeHours,
            ApprovedOvertimeHours = entity.ApprovedOvertimeHours,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
