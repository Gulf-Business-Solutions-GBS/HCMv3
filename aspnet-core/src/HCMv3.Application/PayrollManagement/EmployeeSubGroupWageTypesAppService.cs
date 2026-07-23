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
/// Application service for the wage type entry of an <see cref="EmployeeSubGroup"/> (ported from legacy XAF
/// EmployeeSubGroupWageType business object), listed separately and linked back via
/// <see cref="EmployeeSubGroupWageType.EmployeeSubGroupId"/>. <c>WageTypeId</c> is a nullable FK to the
/// not-yet-ported <c>WageType</c> lookup (a later wave).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupWageTypes)]
public class EmployeeSubGroupWageTypesAppService : HCMv3AppServiceBase, IEmployeeSubGroupWageTypesAppService
{
    private readonly IRepository<EmployeeSubGroupWageType> _employeeSubGroupWageTypeRepository;

    public EmployeeSubGroupWageTypesAppService(IRepository<EmployeeSubGroupWageType> employeeSubGroupWageTypeRepository)
    {
        _employeeSubGroupWageTypeRepository = employeeSubGroupWageTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupWageTypeDto>> GetAll(GetAllEmployeeSubGroupWageTypesInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupWageTypeRepository.GetAll()
            .Include(w => w.EmployeeSubGroup)
            .WhereIf(input.EmployeeSubGroupId.HasValue, w => w.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupWageTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupWageTypeForEditOutput> GetEmployeeSubGroupWageTypeForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupWageTypeRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupWageTypeForEditOutput
        {
            EmployeeSubGroupWageType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupWageTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupWageTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupWageTypeRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupWageTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupWageTypeDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupWageType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeSubGroupWageTypeRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupWageTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupWageTypeDto input)
    {
        var entity = await _employeeSubGroupWageTypeRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeSubGroupWageTypeRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupWageType MapToEntity(CreateOrEditEmployeeSubGroupWageTypeDto input, EmployeeSubGroupWageType entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.WageTypeId = input.WageTypeId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate WageTypeId per EmployeeSubGroup (the legacy
    /// source ran this duplicate check twice with equivalent predicates -- only run once here).
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroupWageType entity)
    {
        var duplicate = await _employeeSubGroupWageTypeRepository.GetAll()
            .AnyAsync(w => w.Id != entity.Id &&
                           w.EmployeeSubGroupId == entity.EmployeeSubGroupId &&
                           w.WageTypeId == entity.WageTypeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("WageTypeAlreadyExists"));
        }
    }

    private static EmployeeSubGroupWageTypeDto ToDto(EmployeeSubGroupWageType entity)
    {
        return new EmployeeSubGroupWageTypeDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            WageTypeId = entity.WageTypeId
        };
    }

    private static CreateOrEditEmployeeSubGroupWageTypeDto ToCreateOrEditDto(EmployeeSubGroupWageType entity)
    {
        return new CreateOrEditEmployeeSubGroupWageTypeDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            WageTypeId = entity.WageTypeId
        };
    }
}
