using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for <see cref="TimeLog"/> (ported from legacy XAF TimeLog business object, Wave 4).
/// The legacy source has NO validation at all for this entity (see <see cref="TimeLog"/> remarks); this
/// service performs plain CRUD with no validation, faithfully matching that triviality.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeLogs)]
public class TimeLogsAppService : HCMv3AppServiceBase, ITimeLogsAppService
{
    private readonly IRepository<TimeLog> _repository;

    public TimeLogsAppService(IRepository<TimeLog> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TimeLogDto>> GetAll(GetAllTimeLogsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.Employee)
            .WhereIf(input.EmployeeId.HasValue, t => t.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TimeLogDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetTimeLogForEditOutput> GetTimeLogForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTimeLogForEditOutput
        {
            TimeLog = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTimeLogDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeLogs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTimeLogDto input)
    {
        var entity = MapToEntity(input, new TimeLog());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeLogs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTimeLogDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static TimeLog MapToEntity(CreateOrEditTimeLogDto input, TimeLog entity)
    {
        entity.Message = input.Message;
        entity.RunTimeStamp = input.RunTimeStamp;
        entity.EmployeeId = input.EmployeeId;
        entity.Status = input.Status;
        entity.DayDate = input.DayDate;
        entity.StopFlag = input.StopFlag;
        entity.NoOfEmployee = input.NoOfEmployee;

        return entity;
    }

    private static TimeLogDto ToDto(TimeLog entity)
    {
        return new TimeLogDto
        {
            Id = entity.Id,
            Message = entity.Message,
            RunTimeStamp = entity.RunTimeStamp,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            Status = entity.Status,
            DayDate = entity.DayDate,
            StopFlag = entity.StopFlag,
            NoOfEmployee = entity.NoOfEmployee
        };
    }

    private static CreateOrEditTimeLogDto ToCreateOrEditDto(TimeLog entity)
    {
        return new CreateOrEditTimeLogDto
        {
            Id = entity.Id,
            Message = entity.Message,
            RunTimeStamp = entity.RunTimeStamp,
            EmployeeId = entity.EmployeeId,
            Status = entity.Status,
            DayDate = entity.DayDate,
            StopFlag = entity.StopFlag,
            NoOfEmployee = entity.NoOfEmployee
        };
    }
}
