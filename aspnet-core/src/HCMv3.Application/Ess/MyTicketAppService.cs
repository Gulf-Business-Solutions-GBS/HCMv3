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

/// <inheritdoc cref="IMyTicketAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyTickets)]
public class MyTicketAppService : MyTransactionAppServiceBase<EmployeeTicket>, IMyTicketAppService
{
    public MyTicketAppService(
        IRepository<EmployeeTicket> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
    }

    protected override string RequestType => TicketWorkflowTransactionHandler.TicketRequestType;

    protected override string RequestNumberPrefix => "TKT";

    [HttpPost]
    public async Task<PagedResultDto<MyTicketRequestDto>> GetMyTickets(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var thisYear = System.DateTime.Today.Year;

            // Entitlement is reckoned per calendar year, so the current year is the useful default view.
            var query = MyRowsQuery(employeeId)
                .Include(t => t.TicketType)
                .WhereIf(!input.IncludeHistory,
                    t => t.TicketDate.Year >= thisYear || t.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyTicketRequestDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public Task<MyRequestSubmissionDto> RequestTicket(RequestTicketInput input)
    {
        if (input.Amount < 0)
        {
            throw new UserFriendlyException(L("TicketAmountCannotBeNegative"));
        }

        return CreateAndSubmitAsync(
            employeeId => new EmployeeTicket
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                TicketTypeId = input.TicketTypeId,
                TicketDate = input.TicketDate.Date,

                // Same date until payroll says otherwise; leaving it at default would put the payment in
                // year one.
                PaymentDate = input.TicketDate.Date,

                Amount = input.Amount,
                Comments = input.Comments
            },
            input.TicketTypeId);
    }

    public Task CancelMyTicket(EntityDto input) => CancelAsync(input.Id);

    private static MyTicketRequestDto ToDto(EmployeeTicket entity)
    {
        return new MyTicketRequestDto
        {
            Id = entity.Id,
            TicketTypeId = entity.TicketTypeId,
            TicketTypeNameEn = entity.TicketType?.NameEn,
            TicketTypeNameAr = entity.TicketType?.NameAr,
            TicketDate = entity.TicketDate,
            Amount = entity.Amount,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
