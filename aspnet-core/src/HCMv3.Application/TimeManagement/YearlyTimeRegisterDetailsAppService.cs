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
/// Application service for the per-TimeType child of <see cref="YearlyTimeRegister"/> (ported from legacy
/// XAF YearlyTimeRegisterDetail business object, Wave 4), listed separately and linked back via
/// <see cref="YearlyTimeRegisterDetail.YearlyTimeRegisterId"/>. Same shape/dead-code-skip as
/// <see cref="MonthlyTimeRegisterDetailsAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_YearlyTimeRegisterDetails)]
public class YearlyTimeRegisterDetailsAppService : HCMv3AppServiceBase, IYearlyTimeRegisterDetailsAppService
{
    private readonly IRepository<YearlyTimeRegisterDetail> _repository;

    public YearlyTimeRegisterDetailsAppService(IRepository<YearlyTimeRegisterDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<YearlyTimeRegisterDetailDto>> GetAll(GetAllYearlyTimeRegisterDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.TimeType)
            .WhereIf(input.YearlyTimeRegisterId.HasValue, d => d.YearlyTimeRegisterId == input.YearlyTimeRegisterId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<YearlyTimeRegisterDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetYearlyTimeRegisterDetailForEditOutput> GetYearlyTimeRegisterDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetYearlyTimeRegisterDetailForEditOutput
        {
            YearlyTimeRegisterDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditYearlyTimeRegisterDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_YearlyTimeRegisterDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditYearlyTimeRegisterDetailDto input)
    {
        var entity = MapToEntity(input, new YearlyTimeRegisterDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_YearlyTimeRegisterDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditYearlyTimeRegisterDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_YearlyTimeRegisterDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static YearlyTimeRegisterDetail MapToEntity(CreateOrEditYearlyTimeRegisterDetailDto input, YearlyTimeRegisterDetail entity)
    {
        entity.YearlyTimeRegisterId = input.YearlyTimeRegisterId;
        entity.TimeTypeId = input.TimeTypeId;
        entity.TimeValue = input.TimeValue;

        return entity;
    }

    private void ValidateAsync(YearlyTimeRegisterDetail entity)
    {
        if (entity.YearlyTimeRegisterId <= 0)
        {
            throw new UserFriendlyException(L("YearlyTimeRegisterCannotBeBlank"));
        }

        if (entity.TimeTypeId <= 0)
        {
            throw new UserFriendlyException(L("TimeTypeCannotBeBlank"));
        }
    }

    private static YearlyTimeRegisterDetailDto ToDto(YearlyTimeRegisterDetail entity)
    {
        return new YearlyTimeRegisterDetailDto
        {
            Id = entity.Id,
            YearlyTimeRegisterId = entity.YearlyTimeRegisterId,
            TimeTypeId = entity.TimeTypeId,
            TimeTypeName = entity.TimeType?.NameEn,
            TimeValue = entity.TimeValue
        };
    }

    private static CreateOrEditYearlyTimeRegisterDetailDto ToCreateOrEditDto(YearlyTimeRegisterDetail entity)
    {
        return new CreateOrEditYearlyTimeRegisterDetailDto
        {
            Id = entity.Id,
            YearlyTimeRegisterId = entity.YearlyTimeRegisterId,
            TimeTypeId = entity.TimeTypeId,
            TimeValue = entity.TimeValue
        };
    }
}
