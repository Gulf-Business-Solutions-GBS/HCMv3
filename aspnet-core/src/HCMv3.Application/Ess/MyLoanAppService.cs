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

/// <inheritdoc cref="IMyLoanAppService"/>
[AbpAuthorize(AppPermissions.Ess_MyLoans)]
public class MyLoanAppService : MyTransactionAppServiceBase<EmployeeLoan>, IMyLoanAppService
{
    public MyLoanAppService(
        IRepository<EmployeeLoan> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IEmployeeLoanInstallmentService installmentService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _installmentService = installmentService;
    }

    private readonly IEmployeeLoanInstallmentService _installmentService;

    protected override string RequestType => LoanWorkflowTransactionHandler.LoanRequestType;

    protected override string RequestNumberPrefix => "LOAN";

    [HttpPost]
    public async Task<PagedResultDto<MyLoanRequestDto>> GetMyLoans(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = System.DateTime.Today;

            var query = MyRowsQuery(employeeId)
                .Include(l => l.LoanType)
                // A loan is current until it is repaid, so the cutoff is its end date, not its start.
                .WhereIf(!input.IncludeHistory, l => l.EndDate >= today || l.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            return new PagedResultDto<MyLoanRequestDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public async Task<MyRequestSubmissionDto> RequestLoan(RequestLoanInput input)
    {
        // Refused early rather than by a rollback from the handler, which reads like a system fault to the
        // person filling the form in.
        if (input.ActualAmount <= 0)
        {
            throw new UserFriendlyException(L("LoanAmountMustBeGreaterThanZero"));
        }

        EmployeeLoan loan = null;

        var result = await CreateAndSubmitAsync(
            employeeId => loan = new EmployeeLoan
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = employeeId,
                LoanTypeId = input.LoanTypeId,
                ActualAmount = input.ActualAmount,

                // The full amount is outstanding on day one. Set here rather than left to the approver so
                // the figure is right even if nobody edits it.
                DueAmount = input.ActualAmount,
                BalanceDueAmount = input.ActualAmount,

                FixedAmount = input.FixedAmount,
                StartDate = input.StartDate.Date,
                RecoveryStartDate = input.RecoveryStartDate.Date,
                EndDate = input.EndDate.Date,
                Comments = input.Comments,
                IncludeInLoanPay = true
            },
            input.LoanTypeId);

        // With workflow off for loans the loan is in force at once, so it gets its first instalment row now; a
        // routed one gets it on approval (LoanWorkflowTransactionHandler).
        if (loan != null && loan.WorkflowStatus is null or RequestStatus.Approved or RequestStatus.Closed)
        {
            await _installmentService.EnsureInitialInstallmentAsync(loan);
        }

        return result;
    }

    public Task CancelMyLoan(EntityDto input) => CancelAsync(input.Id);

    private static MyLoanRequestDto ToDto(EmployeeLoan entity)
    {
        return new MyLoanRequestDto
        {
            Id = entity.Id,
            LoanTypeId = entity.LoanTypeId,
            LoanTypeNameEn = entity.LoanType?.NameEn,
            LoanTypeNameAr = entity.LoanType?.NameAr,
            ActualAmount = entity.ActualAmount,
            FixedAmount = entity.FixedAmount,
            StartDate = entity.StartDate,
            RecoveryStartDate = entity.RecoveryStartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
