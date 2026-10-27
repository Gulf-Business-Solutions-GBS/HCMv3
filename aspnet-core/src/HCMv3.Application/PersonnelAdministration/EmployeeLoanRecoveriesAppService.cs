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

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeLoanRecovery"/> (ported from legacy XAF EmployeeLoanRecovery
/// business object, Wave 2 deferred bucket). Ports the essential active validations only (recovery amount
/// not exceeding the loan's DueAmount, recovery date not earlier than the loan's RecoveryStartDate). The
/// legacy LoanType RecoveryOption/MaxInstallments cross-checks and the cascade that recomputes
/// <see cref="EmployeeLoan.BalanceDueAmount"/> are NOT ported -- see class remarks on
/// <see cref="EmployeeLoanRecovery"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanRecoveries)]
public class EmployeeLoanRecoveriesAppService : HCMv3AppServiceBase, IEmployeeLoanRecoveriesAppService
{
    private readonly IRepository<EmployeeLoanRecovery> _repository;
    private readonly IRepository<EmployeeLoan> _loanRepository;

    public EmployeeLoanRecoveriesAppService(
        IRepository<EmployeeLoanRecovery> repository,
        IRepository<EmployeeLoan> loanRepository)
    {
        _repository = repository;
        _loanRepository = loanRepository;
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
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanRecoveries_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeLoanRecoveryDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeLoanRecoveries_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
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
