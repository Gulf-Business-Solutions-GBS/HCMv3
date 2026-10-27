using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the fixed asset-allowance wage type entry of an <see cref="EmployeeSubGroup"/>
/// (ported from legacy XAF EmployeeSubGroupAssetAllowance business object), listed separately and linked
/// back via <see cref="EmployeeSubGroupAssetAllowance.EmployeeSubGroupId"/>. <c>WageTypeId</c> is a nullable
/// FK to the not-yet-ported <c>WageType</c> lookup (a later wave).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAssetAllowances)]
public class EmployeeSubGroupAssetAllowancesAppService : HCMv3AppServiceBase, IEmployeeSubGroupAssetAllowancesAppService
{
    private readonly IRepository<EmployeeSubGroupAssetAllowance> _employeeSubGroupAssetAllowanceRepository;

    public EmployeeSubGroupAssetAllowancesAppService(IRepository<EmployeeSubGroupAssetAllowance> employeeSubGroupAssetAllowanceRepository)
    {
        _employeeSubGroupAssetAllowanceRepository = employeeSubGroupAssetAllowanceRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupAssetAllowanceDto>> GetAll(GetAllEmployeeSubGroupAssetAllowancesInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupAssetAllowanceRepository.GetAll()
            .Include(a => a.EmployeeSubGroup)
            .WhereIf(input.EmployeeSubGroupId.HasValue, a => a.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupAssetAllowanceDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupAssetAllowanceForEditOutput> GetEmployeeSubGroupAssetAllowanceForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupAssetAllowanceRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupAssetAllowanceForEditOutput
        {
            EmployeeSubGroupAssetAllowance = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupAssetAllowanceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAssetAllowances_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupAssetAllowanceRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAssetAllowances_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupAssetAllowanceDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupAssetAllowance());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeSubGroupAssetAllowanceRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupAssetAllowances_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupAssetAllowanceDto input)
    {
        var entity = await _employeeSubGroupAssetAllowanceRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeSubGroupAssetAllowanceRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupAssetAllowance MapToEntity(CreateOrEditEmployeeSubGroupAssetAllowanceDto input, EmployeeSubGroupAssetAllowance entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.WageTypeId = input.WageTypeId;
        entity.Amount = input.Amount;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate WageTypeId per EmployeeSubGroup (the legacy
    /// source ran this duplicate check twice with equivalent predicates -- only run once here).
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroupAssetAllowance entity)
    {
        var duplicate = await _employeeSubGroupAssetAllowanceRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id &&
                           a.EmployeeSubGroupId == entity.EmployeeSubGroupId &&
                           a.WageTypeId == entity.WageTypeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("WageTypeAlreadyExists"));
        }
    }

    private static EmployeeSubGroupAssetAllowanceDto ToDto(EmployeeSubGroupAssetAllowance entity)
    {
        return new EmployeeSubGroupAssetAllowanceDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            WageTypeId = entity.WageTypeId,
            Amount = entity.Amount
        };
    }

    private static CreateOrEditEmployeeSubGroupAssetAllowanceDto ToCreateOrEditDto(EmployeeSubGroupAssetAllowance entity)
    {
        return new CreateOrEditEmployeeSubGroupAssetAllowanceDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            WageTypeId = entity.WageTypeId,
            Amount = entity.Amount
        };
    }
}
