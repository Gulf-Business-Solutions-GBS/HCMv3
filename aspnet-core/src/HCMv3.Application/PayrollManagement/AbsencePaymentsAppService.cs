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
/// Application service for <see cref="AbsencePayment"/> (ported from legacy XAF AbsencePayment business
/// object). See <see cref="AbsencePayment"/> class remarks: this port ADDS a duplicate-prevention check the
/// legacy left commented-out/disabled -- a deliberate fix, not a straight port of the legacy gap.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsencePayments)]
public class AbsencePaymentsAppService : HCMv3AppServiceBase, IAbsencePaymentsAppService
{
    private readonly IRepository<AbsencePayment> _repository;

    public AbsencePaymentsAppService(IRepository<AbsencePayment> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AbsencePaymentDto>> GetAll(GetAllAbsencePaymentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(p => p.AbsenceType)
            .Include(p => p.WageType)
            .WhereIf(input.AbsenceTypeId.HasValue, p => p.AbsenceTypeId == input.AbsenceTypeId)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), p =>
                p.WageType.Code.Contains(input.Filter) ||
                p.WageType.NameEn.Contains(input.Filter) ||
                p.WageType.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AbsencePaymentDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetAbsencePaymentForEditOutput> GetAbsencePaymentForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetAbsencePaymentForEditOutput
        {
            AbsencePayment = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAbsencePaymentDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsencePayments_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsencePayments_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAbsencePaymentDto input)
    {
        var entity = MapToEntity(input, new AbsencePayment());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsencePayments_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAbsencePaymentDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static AbsencePayment MapToEntity(CreateOrEditAbsencePaymentDto input, AbsencePayment entity)
    {
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.WageTypeId = input.WageTypeId;

        return entity;
    }

    /// <summary>
    /// DELIBERATE FIX: no duplicate (AbsenceTypeId, WageTypeId) pair -- see class remarks.
    /// </summary>
    private async Task ValidateAsync(AbsencePayment entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(p => p.Id != entity.Id &&
                           p.AbsenceTypeId == entity.AbsenceTypeId &&
                           p.WageTypeId == entity.WageTypeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static AbsencePaymentDto ToDto(AbsencePayment entity)
    {
        return new AbsencePaymentDto
        {
            Id = entity.Id,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn
        };
    }

    private static CreateOrEditAbsencePaymentDto ToCreateOrEditDto(AbsencePayment entity)
    {
        return new CreateOrEditAbsencePaymentDto
        {
            Id = entity.Id,
            AbsenceTypeId = entity.AbsenceTypeId,
            WageTypeId = entity.WageTypeId
        };
    }
}
