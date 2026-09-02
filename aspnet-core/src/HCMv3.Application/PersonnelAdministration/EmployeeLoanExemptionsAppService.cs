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
using HCMv3.PersonnelAdministration.Services;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// HR's own screen for <see cref="EmployeeLoanExemption"/> -- skipping a month's loan recovery on an
/// employee's behalf.
///
/// <para>
/// Creates go through <see cref="IAdminTransactionSubmitter"/>, so an exemption HR raises follows the same
/// approval chain a self-service one does when the type is routed, and is simply recorded when it is not.
/// Deferring an instalment costs the company the same either way; who typed it in should not decide whether
/// anybody signs it off.
/// </para>
///
/// <para>
/// The legacy end-date cascade IS now ported (it previously was not, and an exemption without it forgives an
/// instalment rather than deferring it). It lives in <see cref="ILoanExemptionService"/> and runs on
/// approval; deleting an approved exemption re-runs it with that row excluded, which walks the loan's end
/// date back to exactly where it was.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions)]
public class EmployeeLoanExemptionsAppService : HCMv3AppServiceBase, IEmployeeLoanExemptionsAppService
{
    private readonly IRepository<EmployeeLoanExemption> _repository;
    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly ILoanExemptionService _loanExemptionService;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;

    public EmployeeLoanExemptionsAppService(
        IRepository<EmployeeLoanExemption> repository,
        IRepository<EmployeeLoan> loanRepository,
        ILoanExemptionService loanExemptionService,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _repository = repository;
        _loanRepository = loanRepository;
        _loanExemptionService = loanExemptionService;
        _transactionSubmitter = transactionSubmitter;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeLoanExemptionDto>> GetAll(GetAllEmployeeLoanExemptionsInput input)
    {
        input.Normalize();

        // HR's own screen sees exemptions still awaiting approval as well as granted ones -- knowing that a
        // skip has been asked for is the point of the list, and hiding it would invite a duplicate.
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        var query = _repository.GetAll()
            .WhereIf(input.EmployeeLoanId.HasValue, e => e.EmployeeLoanId == input.EmployeeLoanId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeLoanExemptionDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeLoanExemptionForEditOutput> GetEmployeeLoanExemptionForEdit(EntityDto input)
    {
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeLoanExemptionForEditOutput
        {
            EmployeeLoanExemption = new CreateOrEditEmployeeLoanExemptionDto
            {
                Id = entity.Id,
                EmployeeLoanId = entity.EmployeeLoanId,
                ExemptionDate = entity.ExemptionDate,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeLoanExemptionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeLoanExemptionDto input)
    {
        var entity = MapToEntity(input, new EmployeeLoanExemption());
        entity.TenantId = AbpSession.TenantId ?? 0;

        var loan = await GetLoanAsync(entity.EmployeeLoanId);

        // Denormalised from the parent loan rather than taken from the form: whose exemption this is must
        // agree with whose loan it is, always. See the remarks on EmployeeLoanExemption.EmployeeId.
        entity.EmployeeId = loan.EmployeeId;

        // Checked before the row exists, so HR is told what is wrong rather than having the insert rolled
        // back underneath them. The handler applies the identical rule to anything that reaches the engine.
        await _loanExemptionService.EnsureExemptionIsAllowedAsync(entity.EmployeeLoanId, entity.ExemptionDate);

        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            LoanExemptionWorkflowTransactionHandler.LoanExemptionRequestType,
            "LEX",
            // Routed by loan type, as the loan itself is.
            loan.LoanTypeId);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeLoanExemptionDto input)
    {
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        var loan = await GetLoanAsync(entity.EmployeeLoanId);
        entity.EmployeeId = loan.EmployeeId;

        await _loanExemptionService.EnsureExemptionIsAllowedAsync(
            entity.EmployeeLoanId,
            entity.ExemptionDate,
            excludeExemptionId: entity.Id);

        await _repository.UpdateAsync(entity);

        // The month may have moved, or the exemption may have been pointed at a different loan. Both change
        // how many instalments are skipped, so the schedule is recomputed rather than left stale.
        await _loanExemptionService.RecomputeRecoveryEndDateAsync(entity.EmployeeLoanId);
    }

    /// <summary>
    /// Removes the exemption and walks the loan's recovery end date back.
    ///
    /// <para>
    /// This is the reversal half of the cascade, and it is why the recompute is a full recalculation rather
    /// than an increment: re-running it with the removed row excluded lands on exactly the date the loan had
    /// before the exemption was granted. Legacy did the same thing from <c>OnDeleting</c>, passing the Oid
    /// being deleted for the same reason.
    /// </para>
    /// </summary>
    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanExemptions_Delete)]
    public async Task Delete(EntityDto input)
    {
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        var entity = await _repository.FirstOrDefaultAsync(e => e.Id == input.Id);
        if (entity == null)
        {
            return;
        }

        var loanId = entity.EmployeeLoanId;

        await _repository.DeleteAsync(entity);

        await _loanExemptionService.RecomputeRecoveryEndDateAsync(loanId, excludeExemptionId: input.Id);
    }

    private async Task<EmployeeLoan> GetLoanAsync(int employeeLoanId)
    {
        using var _ = CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly);

        var loan = await _loanRepository.FirstOrDefaultAsync(l => l.Id == employeeLoanId);

        if (loan == null)
        {
            throw new UserFriendlyException(L("TheLoanForThisExemptionWasNotFound"));
        }

        return loan;
    }

    private static EmployeeLoanExemption MapToEntity(CreateOrEditEmployeeLoanExemptionDto input, EmployeeLoanExemption entity)
    {
        entity.EmployeeLoanId = input.EmployeeLoanId;
        entity.ExemptionDate = input.ExemptionDate.Date;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeLoanExemptionDto ToDto(EmployeeLoanExemption entity)
    {
        return new EmployeeLoanExemptionDto
        {
            Id = entity.Id,
            EmployeeLoanId = entity.EmployeeLoanId,
            EmployeeId = entity.EmployeeId,
            ExemptionDate = entity.ExemptionDate,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus,
            WorkflowRequestId = entity.WorkflowRequestId
        };
    }
}
