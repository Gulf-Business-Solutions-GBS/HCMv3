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
/// Application service for the commission wage type entry of an <see cref="EmployeeSubGroup"/> (ported from
/// legacy XAF EmployeeSubGroupCommission business object), listed separately and linked back via
/// <see cref="EmployeeSubGroupCommission.EmployeeSubGroupId"/>. <c>WageTypeId</c> is a nullable FK to the
/// not-yet-ported <c>WageType</c> lookup (a later wave).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupCommissions)]
public class EmployeeSubGroupCommissionsAppService : HCMv3AppServiceBase, IEmployeeSubGroupCommissionsAppService
{
    private readonly IRepository<EmployeeSubGroupCommission> _employeeSubGroupCommissionRepository;

    public EmployeeSubGroupCommissionsAppService(IRepository<EmployeeSubGroupCommission> employeeSubGroupCommissionRepository)
    {
        _employeeSubGroupCommissionRepository = employeeSubGroupCommissionRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubGroupCommissionDto>> GetAll(GetAllEmployeeSubGroupCommissionsInput input)
    {
        input.Normalize();

        var query = _employeeSubGroupCommissionRepository.GetAll()
            .Include(c => c.EmployeeSubGroup)
            .WhereIf(input.EmployeeSubGroupId.HasValue, c => c.EmployeeSubGroupId == input.EmployeeSubGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubGroupCommissionDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubGroupCommissionForEditOutput> GetEmployeeSubGroupCommissionForEdit(EntityDto input)
    {
        var entity = await _employeeSubGroupCommissionRepository.GetAsync(input.Id);

        return new GetEmployeeSubGroupCommissionForEditOutput
        {
            EmployeeSubGroupCommission = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubGroupCommissionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupCommissions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _employeeSubGroupCommissionRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupCommissions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubGroupCommissionDto input)
    {
        var entity = MapToEntity(input, new EmployeeSubGroupCommission());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _employeeSubGroupCommissionRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeSubGroupCommissions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubGroupCommissionDto input)
    {
        var entity = await _employeeSubGroupCommissionRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _employeeSubGroupCommissionRepository.UpdateAsync(entity);
    }

    private static EmployeeSubGroupCommission MapToEntity(CreateOrEditEmployeeSubGroupCommissionDto input, EmployeeSubGroupCommission entity)
    {
        entity.EmployeeSubGroupId = input.EmployeeSubGroupId;
        entity.WageTypeId = input.WageTypeId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate WageTypeId per EmployeeSubGroup (the legacy
    /// source ran this duplicate check twice with equivalent predicates -- only run once here).
    /// </summary>
    private async Task ValidateAsync(EmployeeSubGroupCommission entity)
    {
        var duplicate = await _employeeSubGroupCommissionRepository.GetAll()
            .AnyAsync(c => c.Id != entity.Id &&
                           c.EmployeeSubGroupId == entity.EmployeeSubGroupId &&
                           c.WageTypeId == entity.WageTypeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("WageTypeAlreadyExists"));
        }
    }

    private static EmployeeSubGroupCommissionDto ToDto(EmployeeSubGroupCommission entity)
    {
        return new EmployeeSubGroupCommissionDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            EmployeeSubGroupName = entity.EmployeeSubGroup?.NameEn,
            WageTypeId = entity.WageTypeId
        };
    }

    private static CreateOrEditEmployeeSubGroupCommissionDto ToCreateOrEditDto(EmployeeSubGroupCommission entity)
    {
        return new CreateOrEditEmployeeSubGroupCommissionDto
        {
            Id = entity.Id,
            EmployeeSubGroupId = entity.EmployeeSubGroupId,
            WageTypeId = entity.WageTypeId
        };
    }
}
