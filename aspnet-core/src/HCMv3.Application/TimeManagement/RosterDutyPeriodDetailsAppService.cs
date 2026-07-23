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
/// Application service for the per-week child of <see cref="RosterDutyPeriod"/> (ported from legacy XAF
/// RosterDutyPeriodDetail business object), listed separately and linked back via
/// <see cref="RosterDutyPeriodDetail.RosterDutyPeriodId"/>.
///
/// The legacy <c>PerformValidation()</c> is an empty no-op stub, so only basic required-field validation
/// (RosterDutyPeriodId, Week &gt; 0) is ported here — there is no legacy behavior to replicate.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutyPeriodDetails)]
public class RosterDutyPeriodDetailsAppService : HCMv3AppServiceBase, IRosterDutyPeriodDetailsAppService
{
    private readonly IRepository<RosterDutyPeriodDetail> _rosterDutyPeriodDetailRepository;

    public RosterDutyPeriodDetailsAppService(IRepository<RosterDutyPeriodDetail> rosterDutyPeriodDetailRepository)
    {
        _rosterDutyPeriodDetailRepository = rosterDutyPeriodDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RosterDutyPeriodDetailDto>> GetAll(GetAllRosterDutyPeriodDetailsInput input)
    {
        input.Normalize();

        var query = _rosterDutyPeriodDetailRepository.GetAll()
            .Include(r => r.RosterDutyPeriod)
            .WhereIf(input.RosterDutyPeriodId.HasValue, r => r.RosterDutyPeriodId == input.RosterDutyPeriodId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RosterDutyPeriodDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetRosterDutyPeriodDetailForEditOutput> GetRosterDutyPeriodDetailForEdit(EntityDto input)
    {
        var entity = await _rosterDutyPeriodDetailRepository.GetAsync(input.Id);

        return new GetRosterDutyPeriodDetailForEditOutput
        {
            RosterDutyPeriodDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditRosterDutyPeriodDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutyPeriodDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _rosterDutyPeriodDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutyPeriodDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRosterDutyPeriodDetailDto input)
    {
        var entity = MapToEntity(input, new RosterDutyPeriodDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        Validate(entity);

        await _rosterDutyPeriodDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutyPeriodDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRosterDutyPeriodDetailDto input)
    {
        var entity = await _rosterDutyPeriodDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        Validate(entity);

        await _rosterDutyPeriodDetailRepository.UpdateAsync(entity);
    }

    private static RosterDutyPeriodDetail MapToEntity(CreateOrEditRosterDutyPeriodDetailDto input, RosterDutyPeriodDetail entity)
    {
        entity.RosterDutyPeriodId = input.RosterDutyPeriodId;
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
    /// Legacy <c>PerformValidation</c> parity: none (empty no-op stub in the legacy source). Only basic
    /// required-field validation is added here.
    /// </summary>
    private void Validate(RosterDutyPeriodDetail entity)
    {
        if (entity.RosterDutyPeriodId <= 0)
        {
            throw new UserFriendlyException(L("RosterDutyPeriodCannotBeBlank"));
        }

        if (entity.Week <= 0)
        {
            throw new UserFriendlyException(L("WeekMustBeGreaterThanZero"));
        }
    }

    private static RosterDutyPeriodDetailDto ToDto(RosterDutyPeriodDetail entity)
    {
        return new RosterDutyPeriodDetailDto
        {
            Id = entity.Id,
            RosterDutyPeriodId = entity.RosterDutyPeriodId,
            RosterDutyPeriodName = entity.RosterDutyPeriod?.NameEn,
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

    private static CreateOrEditRosterDutyPeriodDetailDto ToCreateOrEditDto(RosterDutyPeriodDetail entity)
    {
        return new CreateOrEditRosterDutyPeriodDetailDto
        {
            Id = entity.Id,
            RosterDutyPeriodId = entity.RosterDutyPeriodId,
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
