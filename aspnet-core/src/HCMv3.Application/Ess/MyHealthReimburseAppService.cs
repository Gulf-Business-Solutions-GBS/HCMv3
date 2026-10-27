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
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <inheritdoc cref="IMyHealthReimburseAppService"/>
///
/// <remarks>
/// <para>
/// The claim row is created hidden and stays hidden until approved, so an unapproved medical claim never
/// reaches a pay run -- and, just as importantly, never counts against the employee's own entitlement as if
/// it had been paid. It does reduce what they can claim NEXT, which is a different thing and is computed
/// rather than stored (see <see cref="IHealthReimburseQuotaService"/>).
/// </para>
/// <para>
/// The balance is checked here as well as in the handler. Not belt and braces: the transaction type ships
/// dormant, and with workflow off nothing routes through the handler at all, so this is the only gate on the
/// self-service path until an administrator turns routing on. Both call the same method, so there is one rule
/// rather than two implementations of it.
/// </para>
/// </remarks>
[AbpAuthorize(AppPermissions.Ess_MyHealthReimburses)]
public class MyHealthReimburseAppService : MyTransactionAppServiceBase<EmployeeHealthReimburse>, IMyHealthReimburseAppService
{
    private readonly IHealthReimburseQuotaService _quotaService;

    public MyHealthReimburseAppService(
        IRepository<EmployeeHealthReimburse> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IHealthReimburseQuotaService quotaService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _quotaService = quotaService;
    }

    protected override string RequestType => HealthReimburseWorkflowTransactionHandler.HealthReimburseRequestType;

    protected override string RequestNumberPrefix => "HRB";

    [HttpPost]
    public async Task<PagedResultDto<MyHealthReimburseDto>> GetMyHealthReimburses(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = MyRowsQuery(employeeId)
                // "History" here means earlier entitlement years. A claim for this year stays on the list
                // whatever its status, because that is what the employee opened the page to check.
                .WhereIf(!input.IncludeHistory, c => c.PaymentDate >= DateTime.Today.AddYears(-1));

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyHealthReimburseDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    [HttpPost]
    public async Task<MyHealthReimburseBalanceDto> GetMyHealthReimburseBalance(GetMyHealthReimburseBalanceInput input)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        var balance = await _quotaService.GetBalanceAsync(employeeId, input?.AsOf);

        return new MyHealthReimburseBalanceDto
        {
            HasEntitlement = balance.HasEntitlement,
            EmployeeSubGroupNameEn = balance.EmployeeSubGroupNameEn,
            EmployeeSubGroupNameAr = balance.EmployeeSubGroupNameAr,
            PeriodFrom = balance.PeriodFrom,
            PeriodTo = balance.PeriodTo,
            Entitlement = balance.Entitlement,
            CarriedForward = balance.CarriedForward,
            Quota = balance.Quota,
            Used = balance.Used,
            Pending = balance.Pending,
            Remaining = balance.Remaining,
            Claimable = balance.Claimable
        };
    }

    public async Task<MyRequestSubmissionDto> RequestHealthReimburse(RequestHealthReimburseInput input)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        // Refused before the row is created, so the employee is told what is wrong instead of watching a
        // submission disappear. The handler enforces exactly the same rule for anything that reaches the
        // engine by another route.
        await _quotaService.EnsureClaimIsAllowedAsync(employeeId, input.PaymentDate.Date, input.Amount);

        return await CreateAndSubmitAsync(
            id => new EmployeeHealthReimburse
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = id,
                PaymentDate = input.PaymentDate.Date,
                Amount = input.Amount,
                Comments = input.Comments
            },
            subTypeId: null);
    }

    public Task CancelMyHealthReimburse(EntityDto input) => CancelAsync(input.Id);

    private static MyHealthReimburseDto ToDto(EmployeeHealthReimburse entity)
    {
        return new MyHealthReimburseDto
        {
            Id = entity.Id,
            PaymentDate = entity.PaymentDate,
            Amount = entity.Amount,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
