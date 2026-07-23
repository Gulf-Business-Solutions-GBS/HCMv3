using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for the per-TimeType child of <see cref="MonthlyTimeRegister"/> (ported from legacy
/// XAF MonthlyTimeRegisterDetail business object, Wave 4), listed separately and linked back via
/// <see cref="MonthlyTimeRegisterDetail.MonthlyTimeRegisterId"/>. See the entity class remarks for the
/// dropped ~35 commented-out (dead) legacy fields.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisterDetails)]
public class MonthlyTimeRegisterDetailsAppService : HCMv3AppServiceBase, IMonthlyTimeRegisterDetailsAppService
{
    private readonly IRepository<MonthlyTimeRegisterDetail> _repository;

    public MonthlyTimeRegisterDetailsAppService(IRepository<MonthlyTimeRegisterDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<MonthlyTimeRegisterDetailDto>> GetAll(GetAllMonthlyTimeRegisterDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.TimeType)
            .WhereIf(input.MonthlyTimeRegisterId.HasValue, d => d.MonthlyTimeRegisterId == input.MonthlyTimeRegisterId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<MonthlyTimeRegisterDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetMonthlyTimeRegisterDetailForEditOutput> GetMonthlyTimeRegisterDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetMonthlyTimeRegisterDetailForEditOutput
        {
            MonthlyTimeRegisterDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditMonthlyTimeRegisterDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisterDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditMonthlyTimeRegisterDetailDto input)
    {
        var entity = MapToEntity(input, new MonthlyTimeRegisterDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisterDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditMonthlyTimeRegisterDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_MonthlyTimeRegisterDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static MonthlyTimeRegisterDetail MapToEntity(CreateOrEditMonthlyTimeRegisterDetailDto input, MonthlyTimeRegisterDetail entity)
    {
        entity.MonthlyTimeRegisterId = input.MonthlyTimeRegisterId;
        entity.TimeTypeId = input.TimeTypeId;
        entity.TimeValue = input.TimeValue;

        return entity;
    }

    private void ValidateAsync(MonthlyTimeRegisterDetail entity)
    {
        if (entity.MonthlyTimeRegisterId <= 0)
        {
            throw new UserFriendlyException(L("MonthlyTimeRegisterCannotBeBlank"));
        }

        if (entity.TimeTypeId <= 0)
        {
            throw new UserFriendlyException(L("TimeTypeCannotBeBlank"));
        }
    }

    private static MonthlyTimeRegisterDetailDto ToDto(MonthlyTimeRegisterDetail entity)
    {
        return new MonthlyTimeRegisterDetailDto
        {
            Id = entity.Id,
            MonthlyTimeRegisterId = entity.MonthlyTimeRegisterId,
            TimeTypeId = entity.TimeTypeId,
            TimeTypeName = entity.TimeType?.NameEn,
            TimeValue = entity.TimeValue
        };
    }

    private static CreateOrEditMonthlyTimeRegisterDetailDto ToCreateOrEditDto(MonthlyTimeRegisterDetail entity)
    {
        return new CreateOrEditMonthlyTimeRegisterDetailDto
        {
            Id = entity.Id,
            MonthlyTimeRegisterId = entity.MonthlyTimeRegisterId,
            TimeTypeId = entity.TimeTypeId,
            TimeValue = entity.TimeValue
        };
    }
}
