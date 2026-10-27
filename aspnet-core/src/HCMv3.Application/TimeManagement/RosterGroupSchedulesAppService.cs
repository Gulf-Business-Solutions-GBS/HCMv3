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
/// Application service for the Roster Group Schedule (ported from legacy XAF RosterGroupSchedule business
/// object), linking a <see cref="RosterGroup"/> to a duty period template for a date range. Its
/// <see cref="RosterGroupSchedule.DutyPeriodId"/> FK targets the CLASSIC <see cref="TimeManagement.DutyPeriod"/>
/// entity, not <see cref="RosterDutyPeriod"/> — same legacy naming inconsistency as
/// <see cref="RosterDutySchedulesAppService"/>, ported faithfully. The legacy <c>IExcelImport</c> marker is
/// an XAF UI concept and is not ported.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroupSchedules)]
public class RosterGroupSchedulesAppService : HCMv3AppServiceBase, IRosterGroupSchedulesAppService
{
    private readonly IRepository<RosterGroupSchedule> _rosterGroupScheduleRepository;

    public RosterGroupSchedulesAppService(IRepository<RosterGroupSchedule> rosterGroupScheduleRepository)
    {
        _rosterGroupScheduleRepository = rosterGroupScheduleRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RosterGroupScheduleDto>> GetAll(GetAllRosterGroupSchedulesInput input)
    {
        input.Normalize();

        var query = _rosterGroupScheduleRepository.GetAll()
            .Include(r => r.RosterGroup)
            .Include(r => r.DutyPeriod)
            .WhereIf(input.RosterGroupId.HasValue, r => r.RosterGroupId == input.RosterGroupId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RosterGroupScheduleDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetRosterGroupScheduleForEditOutput> GetRosterGroupScheduleForEdit(EntityDto input)
    {
        var entity = await _rosterGroupScheduleRepository.GetAsync(input.Id);

        return new GetRosterGroupScheduleForEditOutput
        {
            RosterGroupSchedule = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditRosterGroupScheduleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroupSchedules_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _rosterGroupScheduleRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroupSchedules_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRosterGroupScheduleDto input)
    {
        var entity = MapToEntity(input, new RosterGroupSchedule());
        entity.TenantId = AbpSession.TenantId ?? 0;

        Validate(entity);

        await _rosterGroupScheduleRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroupSchedules_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRosterGroupScheduleDto input)
    {
        var entity = await _rosterGroupScheduleRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        Validate(entity);

        await _rosterGroupScheduleRepository.UpdateAsync(entity);
    }

    private static RosterGroupSchedule MapToEntity(CreateOrEditRosterGroupScheduleDto input, RosterGroupSchedule entity)
    {
        entity.RosterGroupId = input.RosterGroupId;
        entity.DutyPeriodId = input.DutyPeriodId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.StartPoint = input.StartPoint;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: RosterGroup required, DutyPeriod required, StartDate/EndDate
    /// required with EndDate not earlier than StartDate, and StartPoint &gt; 0.
    /// </summary>
    private void Validate(RosterGroupSchedule entity)
    {
        if (entity.RosterGroupId <= 0)
        {
            throw new UserFriendlyException(L("RosterGroupCannotBeBlank"));
        }

        if (entity.DutyPeriodId <= 0)
        {
            throw new UserFriendlyException(L("DutyPeriodCannotBeBlank"));
        }

        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        if (entity.StartPoint <= 0)
        {
            throw new UserFriendlyException(L("StartPointMustBeGreaterThanZero"));
        }
    }

    private static RosterGroupScheduleDto ToDto(RosterGroupSchedule entity)
    {
        return new RosterGroupScheduleDto
        {
            Id = entity.Id,
            RosterGroupId = entity.RosterGroupId,
            RosterGroupName = entity.RosterGroup?.NameEn,
            DutyPeriodId = entity.DutyPeriodId,
            DutyPeriodName = entity.DutyPeriod?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartPoint = entity.StartPoint
        };
    }

    private static CreateOrEditRosterGroupScheduleDto ToCreateOrEditDto(RosterGroupSchedule entity)
    {
        return new CreateOrEditRosterGroupScheduleDto
        {
            Id = entity.Id,
            RosterGroupId = entity.RosterGroupId,
            DutyPeriodId = entity.DutyPeriodId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartPoint = entity.StartPoint
        };
    }
}
