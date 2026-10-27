using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using HCMv3.TimeManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyMobileDeviceAppService"/>
///
/// <remarks>
/// <para>
/// The rules are checked here as well as in the handler, and that is not belt-and-braces. The handler only
/// runs when the type is workflow-enabled; with routing off for a tenant this is the ONLY place the
/// device-id, device-type and duplicate rules would be applied. Both call the same
/// <see cref="IMobileDeviceRegistrationService"/>, so there is one implementation rather than two that agree
/// today.
/// </para>
/// </remarks>
[AbpAuthorize(AppPermissions.Ess_MyMobileDevices)]
public class MyMobileDeviceAppService : MyTransactionAppServiceBase<EmployeeMobileDevice>, IMyMobileDeviceAppService
{
    private readonly IMobileDeviceRegistrationService _registrationService;

    public MyMobileDeviceAppService(
        IRepository<EmployeeMobileDevice> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IMobileDeviceRegistrationService registrationService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _registrationService = registrationService;
    }

    protected override string RequestType => MobileDeviceWorkflowTransactionHandler.MobileDeviceRequestType;

    protected override string RequestNumberPrefix => "MDV";

    /// <summary>
    /// The caller's own devices. <c>[HttpPost]</c> because the input is a custom DTO: ABP would route a
    /// <c>Get*</c> method as HTTP GET, NSwag would flatten <see cref="GetMyRequestsInput"/> into query-string
    /// parameters, and the DTO class would never appear in the generated proxies.
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<MyMobileDeviceDto>> GetMyMobileDevices(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            // "Not history" hides devices the employee has retired or that were refused. A registration that
            // is merely future-dated is NOT history -- it is the one they are waiting on.
            var query = MyRowsQuery(employeeId)
                .WhereIf(!input.IncludeHistory,
                    d => d.IsActive && d.WorkflowStatus != RequestStatus.Rejected);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            var today = DateTime.Today;

            return new PagedResultDto<MyMobileDeviceDto>(
                totalCount,
                items.Select(d => ToDto(d, today)).ToList());
        }
    }

    public async Task<MyRequestSubmissionDto> RegisterMyMobileDevice(RegisterMyMobileDeviceInput input)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        // Checked before the row exists, so the employee is told what is wrong rather than having a
        // half-created request rolled back underneath them -- and so the rules still apply when this type's
        // workflow is switched off and the handler never runs.
        await _registrationService.EnsureRegistrationIsAllowedAsync(
            employeeId, input.DeviceId, input.DeviceType);

        // Legacy defaulted this to today in AfterConstruction. Kept, because an unset DateTime lands on year
        // 1, which would read as "in effect since forever" to the lookup.
        var effectiveDate = input.EffectiveDate?.Date ?? DateTime.Today;

        return await CreateAndSubmitAsync(
            meId => new EmployeeMobileDevice
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = meId,
                DeviceId = input.DeviceId.Trim(),
                DeviceType = input.DeviceType.Trim(),
                EffectiveDate = effectiveDate,

                // Registered live. A registration created inactive would be a row that approval could never
                // make usable, which is the one outcome this form must not produce.
                IsActive = true,
                Comments = input.Comments
            },
            // No sub-type: legacy routed every device registration the same way, and there is nothing on the
            // row a routing rule could sensibly discriminate on -- one handset is not a bigger commitment
            // than another.
            subTypeId: null);
    }

    public Task CancelMyMobileDevice(EntityDto input) => CancelAsync(input.Id);

    private static MyMobileDeviceDto ToDto(EmployeeMobileDevice entity, DateTime today)
    {
        return new MyMobileDeviceDto
        {
            Id = entity.Id,
            DeviceId = entity.DeviceId,
            DeviceType = entity.DeviceType,
            EffectiveDate = entity.EffectiveDate,
            IsActive = entity.IsActive,
            Comments = entity.Comments,

            // The same four conditions IMobileDeviceRegistrationService applies, so the badge on the screen
            // and the answer the punch endpoint would give cannot disagree.
            IsUsableToday = entity.IsActive
                            && entity.EffectiveDate.Date <= today
                            && (entity.WorkflowStatus == null
                                || entity.WorkflowStatus == RequestStatus.Approved
                                || entity.WorkflowStatus == RequestStatus.Closed),
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
