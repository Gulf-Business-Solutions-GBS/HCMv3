using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Authorization.Users;
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
/// Application service for <see cref="EmployeeNonPayrollTrans"/> (ported from legacy XAF
/// EmployeeNonPayrollTrans business object). The wage types offered and accepted follow legacy's
/// <c>NonPayrollWageType</c> data-source filter, through <see cref="INonPayrollWageTypeSelector"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans)]
public class EmployeeNonPayrollTransAppService : HCMv3AppServiceBase, IEmployeeNonPayrollTransAppService
{
    private readonly IRepository<EmployeeNonPayrollTrans> _repository;
    private readonly INonPayrollWageTypeSelector _wageTypeSelector;
    private readonly IRepository<UserRole, long> _userRoleRepository;

    public EmployeeNonPayrollTransAppService(
        IRepository<EmployeeNonPayrollTrans> repository,
        INonPayrollWageTypeSelector wageTypeSelector,
        IRepository<UserRole, long> userRoleRepository)
    {
        _repository = repository;
        _wageTypeSelector = wageTypeSelector;
        _userRoleRepository = userRoleRepository;
    }

    /// <summary>The wage types the signed-in user may choose on a non-payroll transaction (legacy NonPayrollWageType).</summary>
    public async Task<ListResultDto<NonPayrollWageTypeDto>> GetNonPayrollWageTypes()
    {
        var roleIds = await GetCurrentUserRoleIdsAsync();
        var items = await _wageTypeSelector.Query(roleIds)
            .OrderBy(w => w.Code)
            .Select(w => new NonPayrollWageTypeDto { Id = w.Id, Code = w.Code, NameEn = w.NameEn, NameAr = w.NameAr })
            .ToListAsync();

        return new ListResultDto<NonPayrollWageTypeDto>(items);
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeNonPayrollTransDto>> GetAll(GetAllEmployeeNonPayrollTransInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.WageType)
            .Include(e => e.Currency)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeNonPayrollTransDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeNonPayrollTransForEditOutput> GetEmployeeNonPayrollTransForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeNonPayrollTransForEditOutput
        {
            EmployeeNonPayrollTrans = new CreateOrEditEmployeeNonPayrollTransDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                WageTypeId = entity.WageTypeId,
                Amount = entity.Amount,
                CurrencyId = entity.CurrencyId,
                PaymentDate = entity.PaymentDate,
                IsLocked = entity.IsLocked,
                Comments = entity.Comments,
                NotificationSent = entity.NotificationSent,
                PayableBy = entity.PayableBy,
                PaymentMethod = entity.PaymentMethod
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeNonPayrollTransDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeNonPayrollTransDto input)
    {
        await EnsureWageTypeAllowedAsync(input.WageTypeId);

        var entity = MapToEntity(input, new EmployeeNonPayrollTrans());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeNonPayrollTransDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        // Only a changed wage type is checked: legacy filtered the dropdown but never refused a save, so a record
        // already holding a wage type that no longer qualifies can still be edited.
        if (entity.WageTypeId != input.WageTypeId)
        {
            await EnsureWageTypeAllowedAsync(input.WageTypeId);
        }

        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeNonPayrollTrans_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task EnsureWageTypeAllowedAsync(int wageTypeId)
    {
        var roleIds = await GetCurrentUserRoleIdsAsync();
        if (!await _wageTypeSelector.Query(roleIds).AnyAsync(w => w.Id == wageTypeId))
        {
            throw new UserFriendlyException(L("NonPayrollWageTypeNotAllowed"));
        }
    }

    private async Task<List<int>> GetCurrentUserRoleIdsAsync()
    {
        var userId = AbpSession.UserId;
        if (!userId.HasValue)
        {
            return new List<int>();
        }

        return await _userRoleRepository.GetAll().Where(r => r.UserId == userId.Value).Select(r => r.RoleId).ToListAsync();
    }

    private static EmployeeNonPayrollTrans MapToEntity(CreateOrEditEmployeeNonPayrollTransDto input, EmployeeNonPayrollTrans entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.WageTypeId = input.WageTypeId;
        entity.Amount = input.Amount;
        entity.CurrencyId = input.CurrencyId;
        entity.PaymentDate = input.PaymentDate.Date;
        entity.IsLocked = input.IsLocked;
        entity.Comments = input.Comments;
        entity.NotificationSent = input.NotificationSent;
        entity.PayableBy = input.PayableBy;
        entity.PaymentMethod = input.PaymentMethod;
        return entity;
    }

    private static EmployeeNonPayrollTransDto ToDto(EmployeeNonPayrollTrans entity)
    {
        return new EmployeeNonPayrollTransDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            WageTypeId = entity.WageTypeId,
            WageTypeNameEn = entity.WageType?.NameEn,
            Amount = entity.Amount,
            CurrencyId = entity.CurrencyId,
            CurrencyNameEn = entity.Currency?.NameEn,
            PaymentDate = entity.PaymentDate,
            IsLocked = entity.IsLocked,
            Comments = entity.Comments,
            NotificationSent = entity.NotificationSent,
            PayableBy = entity.PayableBy,
            PaymentMethod = entity.PaymentMethod
        };
    }
}
