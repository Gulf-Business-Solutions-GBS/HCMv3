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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeLoanRecovery"/> (ported from legacy XAF EmployeeLoanRecovery
/// business object). Ports legacy's validations (recovery amount not exceeding the loan's DueAmount, recovery
/// date not earlier than the loan's RecoveryStartDate, the loan type's RecoveryOption and MaxInstallments) and
/// its <c>UpdateDueBalance</c>: every add, change or delete recomputes the loan's
/// <see cref="EmployeeLoan.BalanceDueAmount"/>. See <see cref="IEmployeeLoanRulesService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanRecoveries)]
public class EmployeeLoanRecoveriesAppService : HCMv3AppServiceBase, IEmployeeLoanRecoveriesAppService
{
    private readonly IRepository<EmployeeLoanRecovery> _repository;
    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly IEmployeeLoanRulesService _loanRules;

    public EmployeeLoanRecoveriesAppService(
        IRepository<EmployeeLoanRecovery> repository,
        IRepository<EmployeeLoan> loanRepository,
        IEmployeeLoanRulesService loanRules)
    {
        _repository = repository;
        _loanRepository = loanRepository;
        _loanRules = loanRules;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeLoanRecoveryDto>> GetAll(GetAllEmployeeLoanRecoveriesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.EmployeeLoanId.HasValue, r => r.EmployeeLoanId == input.EmployeeLoanId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeLoanRecoveryDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeLoanRecoveryForEditOutput> GetEmployeeLoanRecoveryForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeLoanRecoveryForEditOutput
        {
            EmployeeLoanRecovery = new CreateOrEditEmployeeLoanRecoveryDto
            {
                Id = entity.Id,
                EmployeeLoanId = entity.EmployeeLoanId,
                RecoveryDate = entity.RecoveryDate,
                RecoveryAmount = entity.RecoveryAmount,
                LoanRecoveryMethod = entity.LoanRecoveryMethod,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeLoanRecoveryDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanRecoveries_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeLoanRecoveryDto input)
    {
        var entity = MapToEntity(input, new EmployeeLoanRecovery());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
        await _loanRules.UpdateBalanceDueAsync(entity.EmployeeLoanId);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanRecoveries_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeLoanRecoveryDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var previousLoanId = entity.EmployeeLoanId;
        MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
        await _loanRules.UpdateBalanceDueAsync(entity.EmployeeLoanId);

        // Moved to another loan: the one it left is owed more again.
        if (previousLoanId != entity.EmployeeLoanId)
        {
            await _loanRules.UpdateBalanceDueAsync(previousLoanId);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanRecoveries_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        await _repository.DeleteAsync(entity);

        // Legacy OnDeleting: the balance as though this recovery had never been made.
        await _loanRules.UpdateBalanceDueAsync(entity.EmployeeLoanId, excludeRecoveryId: entity.Id);
    }

    private async Task ValidateAsync(EmployeeLoanRecovery entity)
    {
        var loan = await _loanRepository.GetAsync(entity.EmployeeLoanId);

        var recoveredAmount = await _repository.GetAll()
            .Where(r => r.Id != entity.Id && r.EmployeeLoanId == entity.EmployeeLoanId)
            .SumAsync(r => (double?)r.RecoveryAmount) ?? 0;

        if (recoveredAmount + entity.RecoveryAmount > loan.DueAmount)
        {
            throw new UserFriendlyException(L("RecoveryAmountExceedsDueAmount"));
        }

        if (entity.RecoveryDate < loan.RecoveryStartDate)
        {
            throw new UserFriendlyException(L("RecoveryDateCannotBeEarlierThanRecoveryStartDate"));
        }

        // The loan type's recovery option and instalment limit.
        await _loanRules.ValidateRecoveryAsync(entity);
    }

    private static EmployeeLoanRecovery MapToEntity(CreateOrEditEmployeeLoanRecoveryDto input, EmployeeLoanRecovery entity)
    {
        entity.EmployeeLoanId = input.EmployeeLoanId;
        entity.RecoveryDate = input.RecoveryDate.Date;
        entity.RecoveryAmount = input.RecoveryAmount;
        entity.LoanRecoveryMethod = input.LoanRecoveryMethod;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeLoanRecoveryDto ToDto(EmployeeLoanRecovery entity)
    {
        return new EmployeeLoanRecoveryDto
        {
            Id = entity.Id,
            EmployeeLoanId = entity.EmployeeLoanId,
            RecoveryDate = entity.RecoveryDate,
            RecoveryAmount = entity.RecoveryAmount,
            LoanRecoveryMethod = entity.LoanRecoveryMethod,
            Comments = entity.Comments
        };
    }
}
