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

/// <inheritdoc cref="IMyAttendanceAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyAttendance)]
public class MyAttendanceAppService : MyTransactionAppServiceBase<EmployeeAttendance>, IMyAttendanceAppService
{
    public MyAttendanceAppService(
        IRepository<EmployeeAttendance> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
    }

    protected override string RequestType => AttendanceWorkflowTransactionHandler.AttendanceRequestType;

    protected override string RequestNumberPrefix => "ATT";

    [HttpPost]
    public async Task<PagedResultDto<MyAttendanceRequestDto>> GetMyAttendances(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // A correction is always about a day that has already happened, so a date cutoff would hide
            // everything. History here means "already decided" instead.
            var query = MyRowsQuery(employeeId)
                .Include(a => a.AttendanceType)
                .WhereIf(!input.IncludeHistory,
                    a => a.WorkflowStatus == null
                         || a.WorkflowStatus == RequestStatus.Draft
                         || a.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyAttendanceRequestDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public Task<MyRequestSubmissionDto> RequestAttendanceCorrection(RequestAttendanceCorrectionInput input)
    {
        // Required, not merely encouraged. An approver cannot check a claim about a day that has already
        // passed without being told what went wrong.
        if (input.Comments.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("AReasonIsRequiredForAChange"));
        }

        return CreateAndSubmitAsync(
            employeeId => new EmployeeAttendance
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                AttendanceTypeId = input.AttendanceTypeId,
                StartDate = input.StartDate.Date,
                EndDate = input.EndDate.Date,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                AttendanceHours = input.AttendanceHours,
                Comments = input.Comments
            },
            input.AttendanceTypeId);
    }

    public Task CancelMyAttendance(EntityDto input) => CancelAsync(input.Id);

    private static MyAttendanceRequestDto ToDto(EmployeeAttendance entity)
    {
        return new MyAttendanceRequestDto
        {
            Id = entity.Id,
            AttendanceTypeId = entity.AttendanceTypeId,
            AttendanceTypeNameEn = entity.AttendanceType?.NameEn,
            AttendanceTypeNameAr = entity.AttendanceType?.NameAr,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            AttendanceHours = entity.AttendanceHours,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
