using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.SystemSecurity.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Application service for <see cref="PunchNotificationLog"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.PunchNotificationLog). Plain audit-log CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_PunchNotificationLogs)]
public class PunchNotificationLogsAppService : HCMv3AppServiceBase, IPunchNotificationLogsAppService
{
    private readonly IRepository<PunchNotificationLog> _repository;

    public PunchNotificationLogsAppService(IRepository<PunchNotificationLog> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PunchNotificationLogDto>> GetAll(GetAllPunchNotificationLogsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.EmployeeDutyPeriodId.HasValue,
                p => p.EmployeeDutyPeriodId == input.EmployeeDutyPeriodId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PunchNotificationLogDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetPunchNotificationLogForEditOutput> GetPunchNotificationLogForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetPunchNotificationLogForEditOutput
        {
            PunchNotificationLog = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPunchNotificationLogDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_PunchNotificationLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_PunchNotificationLogs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPunchNotificationLogDto input)
    {
        var entity = MapToEntity(input, new PunchNotificationLog());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_PunchNotificationLogs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPunchNotificationLogDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static PunchNotificationLog MapToEntity(CreateOrEditPunchNotificationLogDto input, PunchNotificationLog entity)
    {
        entity.EmployeeDutyPeriodId = input.EmployeeDutyPeriodId;
        entity.Type = input.Type;
        entity.Date = input.Date;
        entity.EmailNotificationLogId = input.EmailNotificationLogId;

        return entity;
    }

    private static PunchNotificationLogDto ToDto(PunchNotificationLog entity)
    {
        return new PunchNotificationLogDto
        {
            Id = entity.Id,
            EmployeeDutyPeriodId = entity.EmployeeDutyPeriodId,
            Type = entity.Type,
            Date = entity.Date,
            EmailNotificationLogId = entity.EmailNotificationLogId
        };
    }

    private static CreateOrEditPunchNotificationLogDto ToCreateOrEditDto(PunchNotificationLog entity)
    {
        return new CreateOrEditPunchNotificationLogDto
        {
            Id = entity.Id,
            EmployeeDutyPeriodId = entity.EmployeeDutyPeriodId,
            Type = entity.Type,
            Date = entity.Date,
            EmailNotificationLogId = entity.EmailNotificationLogId
        };
    }
}
