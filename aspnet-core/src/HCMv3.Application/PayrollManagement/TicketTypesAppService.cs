using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for <see cref="TicketType"/>. Ported from legacy XAF TicketType business object.
/// The legacy's dynamic <c>TicketTypeWageType</c> combo-box filter is a pure XAF UI concern and is not
/// ported -- see <see cref="TicketType"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketTypes)]
public class TicketTypesAppService : HCMv3AppServiceBase, ITicketTypesAppService
{
    private readonly IRepository<TicketType> _repository;

    public TicketTypesAppService(IRepository<TicketType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TicketTypeDto>> GetAll(GetAllTicketTypesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.WageType)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TicketTypeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetTicketTypeForEditOutput> GetTicketTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTicketTypeForEditOutput
        {
            TicketType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTicketTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTicketTypeDto input)
    {
        var entity = MapToEntity(input, new TicketType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTicketTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static TicketType MapToEntity(CreateOrEditTicketTypeDto input, TicketType entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.TicketBasisPeriod = input.TicketBasisPeriod;
        entity.TicketBasisFixedDays = input.TicketBasisFixedDays;
        entity.Lapse = input.Lapse;
        entity.LapseAfter = input.LapseAfter;
        entity.StopDependantAccrualAfter = input.StopDependantAccrualAfter;
        entity.MinCompensation = input.MinCompensation;
        entity.DeductionPeriod = input.DeductionPeriod;
        entity.QuotaEntitlement = input.QuotaEntitlement;
        entity.WageTypeId = input.WageTypeId;

        return entity;
    }

    /// <summary>Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.</summary>
    private async Task AssignCodeIfEmptyAsync(TicketType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(e => e.Code != null && e.Code != "")
            .Select(e => e.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code, unique NameEn/NameAr, WageType required.
    /// </summary>
    private async Task ValidateAsync(TicketType entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id &&
                           (e.NameEn == entity.NameEn || e.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (!entity.WageTypeId.HasValue)
        {
            throw new UserFriendlyException(L("WageTypeRequired"));
        }

        // Not in legacy: mirrors the AbsenceType ReInitializeAfter rule; 0 would hang the air ticket accrual.
        if (entity.Lapse && entity.LapseAfter <= 0)
        {
            throw new UserFriendlyException(L("LapseAfterMustBeGreaterThanZero"));
        }
    }

    private static TicketTypeDto ToDto(TicketType entity)
    {
        return new TicketTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            TicketBasisPeriod = entity.TicketBasisPeriod,
            TicketBasisFixedDays = entity.TicketBasisFixedDays,
            Lapse = entity.Lapse,
            LapseAfter = entity.LapseAfter,
            StopDependantAccrualAfter = entity.StopDependantAccrualAfter,
            MinCompensation = entity.MinCompensation,
            DeductionPeriod = entity.DeductionPeriod,
            QuotaEntitlement = entity.QuotaEntitlement,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn
        };
    }

    private static CreateOrEditTicketTypeDto ToCreateOrEditDto(TicketType entity)
    {
        return new CreateOrEditTicketTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            TicketBasisPeriod = entity.TicketBasisPeriod,
            TicketBasisFixedDays = entity.TicketBasisFixedDays,
            Lapse = entity.Lapse,
            LapseAfter = entity.LapseAfter,
            StopDependantAccrualAfter = entity.StopDependantAccrualAfter,
            MinCompensation = entity.MinCompensation,
            DeductionPeriod = entity.DeductionPeriod,
            QuotaEntitlement = entity.QuotaEntitlement,
            WageTypeId = entity.WageTypeId
        };
    }
}
