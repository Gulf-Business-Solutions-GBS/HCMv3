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
/// Application service for <see cref="EmployeesNotification"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.Util.EmployeesNotification). Plain CRUD; raising these rows from a
/// <see cref="TaskEvent"/> fan-out is deferred notification/email business logic, not implemented here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmployeesNotifications)]
public class EmployeesNotificationsAppService : HCMv3AppServiceBase, IEmployeesNotificationsAppService
{
    private readonly IRepository<EmployeesNotification> _repository;

    public EmployeesNotificationsAppService(IRepository<EmployeesNotification> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeesNotificationDto>> GetAll(GetAllEmployeesNotificationsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(n => n.Employee)
            .Include(n => n.TaskEvent)
            .WhereIf(input.EmployeeId.HasValue, n => n.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeesNotificationDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeesNotificationForEditOutput> GetEmployeesNotificationForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeesNotificationForEditOutput
        {
            EmployeesNotification = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeesNotificationDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmployeesNotifications_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmployeesNotifications_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeesNotificationDto input)
    {
        var entity = MapToEntity(input, new EmployeesNotification());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmployeesNotifications_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeesNotificationDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static EmployeesNotification MapToEntity(CreateOrEditEmployeesNotificationDto input, EmployeesNotification entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.TaskEventId = input.TaskEventId;
        entity.IsNotificationDeleted = input.IsNotificationDeleted;
        entity.IsNotificationRead = input.IsNotificationRead;
        return entity;
    }

    private static EmployeesNotificationDto ToDto(EmployeesNotification entity)
    {
        return new EmployeesNotificationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            TaskEventId = entity.TaskEventId,
            TaskEventSubject = entity.TaskEvent?.Subject,
            IsNotificationDeleted = entity.IsNotificationDeleted,
            IsNotificationRead = entity.IsNotificationRead
        };
    }

    private static CreateOrEditEmployeesNotificationDto ToCreateOrEditDto(EmployeesNotification entity)
    {
        return new CreateOrEditEmployeesNotificationDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            TaskEventId = entity.TaskEventId,
            IsNotificationDeleted = entity.IsNotificationDeleted,
            IsNotificationRead = entity.IsNotificationRead
        };
    }
}
