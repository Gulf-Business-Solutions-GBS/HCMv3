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
/// Application service for the per-WageType "except WageType" link (ported from legacy XAF
/// WageTypeException business object), listed separately and linked back via
/// <see cref="WageTypeException.WageTypeId"/>. See <see cref="WageTypeException"/> class remarks for the
/// legacy ExceptWageType aliasing bug that this port deliberately does NOT preserve.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeExceptions)]
public class WageTypeExceptionsAppService : HCMv3AppServiceBase, IWageTypeExceptionsAppService
{
    private readonly IRepository<WageTypeException> _wageTypeExceptionRepository;

    public WageTypeExceptionsAppService(IRepository<WageTypeException> wageTypeExceptionRepository)
    {
        _wageTypeExceptionRepository = wageTypeExceptionRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WageTypeExceptionDto>> GetAll(GetAllWageTypeExceptionsInput input)
    {
        input.Normalize();

        var query = _wageTypeExceptionRepository.GetAll()
            .Include(e => e.WageType)
            .Include(e => e.ExceptWageType)
            .WhereIf(input.WageTypeId.HasValue, e => e.WageTypeId == input.WageTypeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WageTypeExceptionDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetWageTypeExceptionForEditOutput> GetWageTypeExceptionForEdit(EntityDto input)
    {
        var entity = await _wageTypeExceptionRepository.GetAsync(input.Id);

        return new GetWageTypeExceptionForEditOutput
        {
            WageTypeException = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditWageTypeExceptionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeExceptions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _wageTypeExceptionRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeExceptions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWageTypeExceptionDto input)
    {
        var entity = MapToEntity(input, new WageTypeException());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _wageTypeExceptionRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeExceptions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWageTypeExceptionDto input)
    {
        var entity = await _wageTypeExceptionRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _wageTypeExceptionRepository.UpdateAsync(entity);
    }

    private static WageTypeException MapToEntity(CreateOrEditWageTypeExceptionDto input, WageTypeException entity)
    {
        entity.WageTypeId = input.WageTypeId;
        entity.ExceptWageTypeId = input.ExceptWageTypeId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate (WageTypeId, ExceptWageTypeId) pair.
    /// </summary>
    private async Task ValidateAsync(WageTypeException entity)
    {
        var duplicate = await _wageTypeExceptionRepository.GetAll()
            .AnyAsync(e => e.Id != entity.Id &&
                           e.WageTypeId == entity.WageTypeId &&
                           e.ExceptWageTypeId == entity.ExceptWageTypeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("ExceptWageTypeAlreadyExists"));
        }
    }

    private static WageTypeExceptionDto ToDto(WageTypeException entity)
    {
        return new WageTypeExceptionDto
        {
            Id = entity.Id,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn,
            ExceptWageTypeId = entity.ExceptWageTypeId,
            ExceptWageTypeName = entity.ExceptWageType?.NameEn
        };
    }

    private static CreateOrEditWageTypeExceptionDto ToCreateOrEditDto(WageTypeException entity)
    {
        return new CreateOrEditWageTypeExceptionDto
        {
            Id = entity.Id,
            WageTypeId = entity.WageTypeId,
            ExceptWageTypeId = entity.ExceptWageTypeId
        };
    }
}
