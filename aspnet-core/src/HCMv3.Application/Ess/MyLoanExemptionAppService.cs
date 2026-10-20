using System.Collections.Generic;
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

/// <inheritdoc cref="IMyLoanExemptionAppService"/>
///
/// <remarks>
/// <para>
/// The loan is re-checked as the caller's own before anything is created, and the lookup that feeds the form
/// is scoped the same way. An exemption defers real money on a real repayment schedule, so the one thing this
/// endpoint must never allow is raising a request against somebody else's loan id.
/// </para>
///
/// <para>
/// The rules are checked here as well as in the handler, and that is not belt-and-braces. The handler only
/// runs when the type is workflow-enabled; with routing off for a tenant, this is the ONLY place the
/// duplicate-month and recovery-period rules would be applied. Both call the same
/// <see cref="ILoanExemptionService"/>, so there is one implementation rather than two that agree today.
/// </para>
/// </remarks>
[AbpAuthorize(AppPermissions.Ess_MyLoanExemptions)]
public class MyLoanExemptionAppService : MyTransactionAppServiceBase<EmployeeLoanExemption>, IMyLoanExemptionAppService
{
    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly ILoanExemptionService _loanExemptionService;
    private readonly IEmployeeLoanInstallmentService _installmentService;

    public MyLoanExemptionAppService(
        IRepository<EmployeeLoanExemption> repository,
        IRepository<WorkflowRequest> requestRepository,
        IEmployeeScopeProvider employeeScopeProvider,
        IRequestMasterResolver requestMasterResolver,
        IWorkflowApprovalService workflowApprovalService,
        IRepository<EmployeeLoan> loanRepository,
        ILoanExemptionService loanExemptionService,
        IEmployeeLoanInstallmentService installmentService)
        : base(repository, requestRepository, employeeScopeProvider, requestMasterResolver, workflowApprovalService)
    {
        _loanRepository = loanRepository;
        _loanExemptionService = loanExemptionService;
        _installmentService = installmentService;
    }

    protected override string RequestType => LoanExemptionWorkflowTransactionHandler.LoanExemptionRequestType;

    protected override string RequestNumberPrefix => "LEX";

    [HttpPost]
    public async Task<PagedResultDto<MyLoanExemptionDto>> GetMyLoanExemptions(GetMyRequestsInput input)
    {
        input.Normalize();

        var employeeId = await GetMyEmployeeIdAsync();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var today = System.DateTime.Today;

            // "Not history" is judged on the LOAN, not on the exemption: an exemption granted last year on a
            // loan still being repaid is current information -- it is why the loan runs a month longer than
            // the employee expected.
            var query = MyRowsQuery(employeeId)
                .Include(e => e.EmployeeLoan)
                .ThenInclude(l => l.LoanType)
                .WhereIf(!input.IncludeHistory,
                    e => e.EmployeeLoan.EndDate >= today || e.WorkflowStatus == RequestStatus.Open);

            var totalCount = await query.CountAsync();

            var items = await query.OrderBy(input.Sorting).PageBy(input).ToListAsync();

            var schedules = _installmentService.GetSchedules(items.Select(e => e.EmployeeLoanId));

            return new PagedResultDto<MyLoanExemptionDto>(totalCount, items.Select(e => ToDto(e, schedules)).ToList());
        }
    }

    /// <summary>
    /// The caller's own loans that an exemption could actually be granted against.
    ///
    /// <para>
    /// The <c>ApprovedOnly</c> filter is left ON here, unusually for a self-service read, and deliberately: a
    /// loan still awaiting approval has not been paid out, so there is no instalment to skip yet. Offering it
    /// on this form would invite a request that the handler would then have to refuse.
    /// </para>
    /// </summary>
    public async Task<List<MyLoanForExemptionDto>> GetMyLoansForExemption()
    {
        var employeeId = await GetMyEmployeeIdAsync();

        var today = System.DateTime.Today;

        var loans = await _loanRepository.GetAll()
            .Include(l => l.LoanType)
            .Where(l => l.EmployeeId == employeeId && l.EndDate >= today)
            .OrderBy(l => l.EndDate)
            .ToListAsync();

        var schedules = _installmentService.GetSchedules(loans.Select(l => l.Id));

        return loans.Select(l => new MyLoanForExemptionDto
        {
            EmployeeLoanId = l.Id,
            LoanTypeId = l.LoanTypeId,
            LoanTypeNameEn = l.LoanType?.NameEn,
            LoanTypeNameAr = l.LoanType?.NameAr,
            // The instalment in force this month -- what one exemption would defer -- not the loan's first one.
            FixedAmount = LoanInstallmentSchedule.GetInstallmentAmount(l.FixedAmount, schedules[l.Id], today),
            BalanceDueAmount = l.BalanceDueAmount,
            RecoveryStartDate = l.RecoveryStartDate,
            EndDate = l.EndDate
        }).ToList();
    }

    public async Task<MyRequestSubmissionDto> RequestLoanExemption(RequestLoanExemptionInput input)
    {
        var employeeId = await GetMyEmployeeIdAsync();

        // Scoped to the caller, not merely looked up: another employee's loan id must read as "not found"
        // rather than as a valid target. The ApprovedOnly filter stays on, so a loan still awaiting approval
        // is not exemptible either -- nothing has been paid out to defer.
        var loan = await _loanRepository.GetAll()
            .Where(l => l.Id == input.EmployeeLoanId && l.EmployeeId == employeeId)
            .Select(l => new { l.Id, l.LoanTypeId })
            .FirstOrDefaultAsync();

        if (loan == null)
        {
            throw new UserFriendlyException(L("TheLoanForThisExemptionWasNotFound"));
        }

        // Checked before the row exists, so the employee is told what is wrong rather than having a
        // half-created request rolled back underneath them -- and so the rules still apply when this type's
        // workflow is switched off and the handler never runs.
        await _loanExemptionService.EnsureExemptionIsAllowedAsync(loan.Id, input.ExemptionDate.Date);

        EmployeeLoanExemption exemption = null;

        var result = await CreateAndSubmitAsync(
            meId => exemption = new EmployeeLoanExemption
            {
                TenantId = AbpSession.TenantId ?? 0,
                EmployeeId = meId,
                EmployeeLoanId = loan.Id,
                ExemptionDate = input.ExemptionDate.Date,
                Comments = input.Comments
            },
            // Routed by loan type, as the loan itself is: deferring a month of a housing loan need not follow
            // the same chain as deferring a month of a small advance.
            loan.LoanTypeId);

        // With workflow off for exemptions it is in force at once: the loan's end date and the payroll retro date
        // move now. A routed one does this on approval, in the handler.
        if (exemption != null)
        {
            await _loanExemptionService.ApplyExemptionInForceAsync(exemption);
        }

        return result;
    }

    public Task CancelMyLoanExemption(EntityDto input) => CancelAsync(input.Id);

    private static MyLoanExemptionDto ToDto(EmployeeLoanExemption entity, ILookup<int, EmployeeLoanInstallment> schedules)
    {
        return new MyLoanExemptionDto
        {
            Id = entity.Id,
            EmployeeLoanId = entity.EmployeeLoanId,
            LoanTypeId = entity.EmployeeLoan?.LoanTypeId,
            LoanTypeNameEn = entity.EmployeeLoan?.LoanType?.NameEn,
            LoanTypeNameAr = entity.EmployeeLoan?.LoanType?.NameAr,
            ExemptionDate = entity.ExemptionDate,
            // The instalment in force in the exemption month: the sum this exemption defers.
            InstalmentAmount = entity.EmployeeLoan == null
                ? 0
                : LoanInstallmentSchedule.GetInstallmentAmount(
                    entity.EmployeeLoan.FixedAmount, schedules[entity.EmployeeLoanId], entity.ExemptionDate),
            LoanEndDate = entity.EmployeeLoan?.EndDate ?? default,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId,
            CanCancel = CanCancel(entity)
        };
    }
}
