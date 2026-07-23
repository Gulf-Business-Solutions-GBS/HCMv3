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
/// Application service for the per-week child of <see cref="DutyPeriod"/> (ported from legacy XAF
/// DutyPeriodDetail business object), listed separately and linked back via
/// <see cref="DutyPeriodDetail.DutyPeriodId"/>.
///
/// DELIBERATE SIMPLIFICATION: the legacy requirement that all 7 Day1Duty..Day7Duty FKs be non-null is NOT
/// ported (see <see cref="TimeManagement.DutyPeriodDetail"/> remarks). Only "Week not duplicated within the
/// same DutyPeriod" (and Week &gt; 0) are validated.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_DutyPeriodDetails)]
public class DutyPeriodDetailsAppService : HCMv3AppServiceBase, IDutyPeriodDetailsAppService
{
    private readonly IRepository<DutyPeriodDetail> _dutyPeriodDetailRepository;

    public DutyPeriodDetailsAppService(IRepository<DutyPeriodDetail> dutyPeriodDetailRepository)
    {
        _dutyPeriodDetailRepository = dutyPeriodDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<DutyPeriodDetailDto>> GetAll(GetAllDutyPeriodDetailsInput input)
    {
        input.Normalize();

        var query = _dutyPeriodDetailRepository.GetAll()
            .Include(d => d.DutyPeriod)
            .WhereIf(input.DutyPeriodId.HasValue, d => d.DutyPeriodId == input.DutyPeriodId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<DutyPeriodDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetDutyPeriodDetailForEditOutput> GetDutyPeriodDetailForEdit(EntityDto input)
    {
        var entity = await _dutyPeriodDetailRepository.GetAsync(input.Id);

        return new GetDutyPeriodDetailForEditOutput
        {
            DutyPeriodDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditDutyPeriodDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_DutyPeriodDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _dutyPeriodDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_DutyPeriodDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditDutyPeriodDetailDto input)
    {
        var entity = MapToEntity(input, new DutyPeriodDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _dutyPeriodDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_DutyPeriodDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditDutyPeriodDetailDto input)
    {
        var entity = await _dutyPeriodDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _dutyPeriodDetailRepository.UpdateAsync(entity);
    }

    private static DutyPeriodDetail MapToEntity(CreateOrEditDutyPeriodDetailDto input, DutyPeriodDetail entity)
    {
        entity.DutyPeriodId = input.DutyPeriodId;
        entity.Week = input.Week;
        entity.Day1DutyId = input.Day1DutyId;
        entity.Day2DutyId = input.Day2DutyId;
        entity.Day3DutyId = input.Day3DutyId;
        entity.Day4DutyId = input.Day4DutyId;
        entity.Day5DutyId = input.Day5DutyId;
        entity.Day6DutyId = input.Day6DutyId;
        entity.Day7DutyId = input.Day7DutyId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity (simplified — see class remarks): Week &gt; 0, and no other
    /// (non-deleted) DutyPeriodDetail row for the same DutyPeriod uses the same Week.
    /// </summary>
    private async Task ValidateAsync(DutyPeriodDetail entity)
    {
        if (entity.Week <= 0)
        {
            throw new UserFriendlyException(L("WeekMustBeGreaterThanZero"));
        }

        var duplicateWeek = await _dutyPeriodDetailRepository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.DutyPeriodId == entity.DutyPeriodId &&
                           d.Week == entity.Week);
        if (duplicateWeek)
        {
            throw new UserFriendlyException(L("WeekAlreadyExists", entity.Week));
        }
    }

    private static DutyPeriodDetailDto ToDto(DutyPeriodDetail entity)
    {
        return new DutyPeriodDetailDto
        {
            Id = entity.Id,
            DutyPeriodId = entity.DutyPeriodId,
            DutyPeriodName = entity.DutyPeriod?.NameEn,
            Week = entity.Week,
            Day1DutyId = entity.Day1DutyId,
            Day2DutyId = entity.Day2DutyId,
            Day3DutyId = entity.Day3DutyId,
            Day4DutyId = entity.Day4DutyId,
            Day5DutyId = entity.Day5DutyId,
            Day6DutyId = entity.Day6DutyId,
            Day7DutyId = entity.Day7DutyId
        };
    }

    private static CreateOrEditDutyPeriodDetailDto ToCreateOrEditDto(DutyPeriodDetail entity)
    {
        return new CreateOrEditDutyPeriodDetailDto
        {
            Id = entity.Id,
            DutyPeriodId = entity.DutyPeriodId,
            Week = entity.Week,
            Day1DutyId = entity.Day1DutyId,
            Day2DutyId = entity.Day2DutyId,
            Day3DutyId = entity.Day3DutyId,
            Day4DutyId = entity.Day4DutyId,
            Day5DutyId = entity.Day5DutyId,
            Day6DutyId = entity.Day6DutyId,
            Day7DutyId = entity.Day7DutyId
        };
    }
}
