using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using HCMv3.PayrollManagement.PayrollLock;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeLoan"/> (ported from legacy XAF EmployeeLoan business object,
/// Wave 2 deferred bucket). Ports the essential active validations only (required fields, EndDate not
/// earlier than StartDate, RecoveryStartDate not earlier than StartDate, ActualAmount not less than
/// DueAmount). The legacy loan-eligibility-period/eligible-count-per-year/eligible-amount checks are NOT ported.
///
/// <para>
/// The recovery end date IS recalculated on save when the payroll setting <c>AutoUpdateLoanRecoveryEndDate</c> is
/// on (legacy <c>EmployeeLoan.OnSaving</c> / <c>CalculateRecoveryDate</c>), from the loan's instalment schedule and
/// its exemptions; see <see cref="IEmployeeLoanInstallmentService"/>. A loan that is in force when it is saved --
/// recorded without approval, or with workflow off -- gets its first instalment row from the fixed amount there and
/// then; one that goes for approval gets it on approval (<see cref="LoanWorkflowTransactionHandler"/>).
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans)]
public class EmployeeLoansAppService : HCMv3AppServiceBase, IEmployeeLoansAppService
{
    private readonly IRepository<EmployeeLoan> _repository;
    private readonly IRepository<EmployeeLoanInstallment> _installmentRepository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;
    private readonly IEmployeeLoanInstallmentService _installmentService;
    private readonly IPayrollRetroTrigger _retroTrigger;

    public EmployeeLoansAppService(IRepository<EmployeeLoan> repository,
        IRepository<EmployeeLoanInstallment> installmentRepository,
        IAdminTransactionSubmitter transactionSubmitter,
        IEmployeeLoanInstallmentService installmentService,
        IPayrollRetroTrigger retroTrigger)
    {
        _transactionSubmitter = transactionSubmitter;
        _repository = repository;
        _installmentRepository = installmentRepository;
        _installmentService = installmentService;
        _retroTrigger = retroTrigger;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeLoanDto>> GetAll(GetAllEmployeeLoansInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(l => l.Employee)
            .Include(l => l.LoanType)
            .WhereIf(input.EmployeeId.HasValue, l => l.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeLoanDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeLoanForEditOutput> GetEmployeeLoanForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeLoanForEditOutput
        {
            EmployeeLoan = new CreateOrEditEmployeeLoanDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                LoanTypeId = entity.LoanTypeId,
                StartDate = entity.StartDate,
                RecoveryStartDate = entity.RecoveryStartDate,
                EndDate = entity.EndDate,
                ActualAmount = entity.ActualAmount,
                DueAmount = entity.DueAmount,
                FixedAmount = entity.FixedAmount,
                Comments = entity.Comments,
                RecoverFracInLastTrans = entity.RecoverFracInLastTrans,
                IncludeInLoanPay = entity.IncludeInLoanPay
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeLoanDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeLoanDto input)
    {
        var entity = MapToEntity(input, new EmployeeLoan());
        entity.TenantId = AbpSession.TenantId ?? 0;
        entity.BalanceDueAmount = entity.DueAmount;

        await ApplyCalculatedRecoveryEndDateAsync(entity);

        ValidateEntity(entity);
        // Routed for approval when the catalog says so, and simply filed when it does not -- the same
        // question the self-service form asks, so the answer no longer depends on which screen was used.
        // recordDirectly is refused unless the type permits a bypass AND the user holds the permission.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            LoanWorkflowTransactionHandler.LoanRequestType,
            "LOAN",
            entity.LoanTypeId,
            input.RecordDirectly);

        // In force already (no workflow, or recorded without approval): the loan's first instalment row now.
        if (entity.WorkflowStatus is null or RequestStatus.Approved or RequestStatus.Closed)
        {
            await _installmentService.EnsureInitialInstallmentAsync(entity);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeLoanDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        // Legacy's prevXxx values, as loaded.
        var previousEmployeeId = entity.EmployeeId;
        var previousLoanTypeId = entity.LoanTypeId;
        var previousFixedAmount = entity.FixedAmount;
        var previousStartDate = entity.StartDate;
        var previousRecoveryStartDate = entity.RecoveryStartDate;
        var previousEndDate = entity.EndDate;

        MapToEntity(input, entity);

        await ApplyCalculatedRecoveryEndDateAsync(entity);

        // Legacy EmployeeLoan.OnSaving: when only the recovery end date moved, nothing payroll reads changed, so
        // the save sets no retro date. Legacy compares exactly these five fields, and so does this.
        if (entity.EmployeeId == previousEmployeeId
            && entity.LoanTypeId == previousLoanTypeId
            && entity.FixedAmount == previousFixedAmount
            && entity.StartDate == previousStartDate
            && entity.RecoveryStartDate == previousRecoveryStartDate
            && entity.EndDate != previousEndDate)
        {
            _retroTrigger.MarkStopRetroEffect(entity);
        }

        ValidateEntity(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoans_Delete)]
    public async Task Delete(EntityDto input)
    {
        // Legacy aggregated the schedule under the loan, so it goes with it.
        await _installmentRepository.DeleteAsync(r => r.EmployeeLoanId == input.Id);
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>EmployeeLoan.OnSaving</c>: with <c>AutoUpdateLoanRecoveryEndDate</c> on, the recovery end date is
    /// the calculated one, whatever was typed.
    /// </summary>
    private async Task ApplyCalculatedRecoveryEndDateAsync(EmployeeLoan entity)
    {
        var calculatedEndDate = await _installmentService.CalculateRecoveryEndDateAsync(entity);

        if (calculatedEndDate.HasValue && calculatedEndDate.Value != default && entity.EndDate != calculatedEndDate.Value)
        {
            entity.EndDate = calculatedEndDate.Value;
        }
    }

    private void ValidateEntity(EmployeeLoan entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        if (entity.RecoveryStartDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("RecoveryStartDateCannotBeEarlierThanStartDate"));
        }

        if (entity.ActualAmount < entity.DueAmount)
        {
            throw new UserFriendlyException(L("ActualAmountCannotBeLessThanDueAmount"));
        }
    }

    private static EmployeeLoan MapToEntity(CreateOrEditEmployeeLoanDto input, EmployeeLoan entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.LoanTypeId = input.LoanTypeId;
        entity.StartDate = input.StartDate.Date;
        entity.RecoveryStartDate = input.RecoveryStartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.ActualAmount = input.ActualAmount;
        entity.DueAmount = input.DueAmount;
        entity.FixedAmount = input.FixedAmount;
        entity.Comments = input.Comments;
        entity.RecoverFracInLastTrans = input.RecoverFracInLastTrans;
        entity.IncludeInLoanPay = input.IncludeInLoanPay;
        return entity;
    }

    private static EmployeeLoanDto ToDto(EmployeeLoan entity)
    {
        return new EmployeeLoanDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            LoanTypeId = entity.LoanTypeId,
            LoanTypeName = entity.LoanType?.NameEn,
            StartDate = entity.StartDate,
            RecoveryStartDate = entity.RecoveryStartDate,
            EndDate = entity.EndDate,
            ActualAmount = entity.ActualAmount,
            DueAmount = entity.DueAmount,
            BalanceDueAmount = entity.BalanceDueAmount,
            FixedAmount = entity.FixedAmount,
            Comments = entity.Comments,
            RecoverFracInLastTrans = entity.RecoverFracInLastTrans,
            IncludeInLoanPay = entity.IncludeInLoanPay
        };
    }
}
