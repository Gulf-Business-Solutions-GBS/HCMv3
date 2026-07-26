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
/// Application service for <see cref="EmployeesNotificationLog"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.Util.EmployeesNotificationLog). Plain audit-log CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmployeesNotificationLogs)]
public class EmployeesNotificationLogsAppService : HCMv3AppServiceBase, IEmployeesNotificationLogsAppService
{
    private readonly IRepository<EmployeesNotificationLog> _repository;

    public EmployeesNotificationLogsAppService(IRepository<EmployeesNotificationLog> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeesNotificationLogDto>> GetAll(GetAllEmployeesNotificationLogsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(l => l.Employee)
            .Include(l => l.TaskEvent)
            .WhereIf(input.EmployeeId.HasValue, l => l.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeesNotificationLogDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeesNotificationLogForEditOutput> GetEmployeesNotificationLogForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeesNotificationLogForEditOutput
        {
            EmployeesNotificationLog = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeesNotificationLogDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmployeesNotificationLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmployeesNotificationLogs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeesNotificationLogDto input)
    {
        var entity = MapToEntity(input, new EmployeesNotificationLog());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmployeesNotificationLogs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeesNotificationLogDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static EmployeesNotificationLog MapToEntity(CreateOrEditEmployeesNotificationLogDto input, EmployeesNotificationLog entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.DismissTime = input.DismissTime;
        entity.TaskEventId = input.TaskEventId;
        entity.EmailNotificationLogId = input.EmailNotificationLogId;
        entity.NotificationType = input.NotificationType;
        entity.Date = input.Date;
        return entity;
    }

    private static EmployeesNotificationLogDto ToDto(EmployeesNotificationLog entity)
    {
        return new EmployeesNotificationLogDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            DismissTime = entity.DismissTime,
            TaskEventId = entity.TaskEventId,
            TaskEventSubject = entity.TaskEvent?.Subject,
            EmailNotificationLogId = entity.EmailNotificationLogId,
            NotificationType = entity.NotificationType,
            Date = entity.Date
        };
    }

    private static CreateOrEditEmployeesNotificationLogDto ToCreateOrEditDto(EmployeesNotificationLog entity)
    {
        return new CreateOrEditEmployeesNotificationLogDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            DismissTime = entity.DismissTime,
            TaskEventId = entity.TaskEventId,
            EmailNotificationLogId = entity.EmailNotificationLogId,
            NotificationType = entity.NotificationType,
            Date = entity.Date
        };
    }
}
