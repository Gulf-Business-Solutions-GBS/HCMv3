using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeTask"/> (ported from legacy XAF EmployeeTask business
/// object, Wave 4 deferred bucket). Ports the essential required-field validation only.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTasks)]
public class EmployeeTasksAppService : HCMv3AppServiceBase, IEmployeeTasksAppService
{
    private readonly IRepository<EmployeeTask> _repository;

    public EmployeeTasksAppService(IRepository<EmployeeTask> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTaskDto>> GetAll(GetAllEmployeeTasksInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.TaskType)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeTaskDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeTaskForEditOutput> GetEmployeeTaskForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeTaskForEditOutput
        {
            EmployeeTask = new CreateOrEditEmployeeTaskDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                TaskTypeId = entity.TaskTypeId,
                TaskDate = entity.TaskDate,
                TaskStatus = entity.TaskStatus,
                ReminderDate = entity.ReminderDate,
                MonitoringType = entity.MonitoringType,
                MonitoringCount = entity.MonitoringCount,
                MonitoringUnit = entity.MonitoringUnit,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeTaskDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTasks_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeTaskDto input)
    {
        var entity = MapToEntity(input, new EmployeeTask());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTasks_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeTaskDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTasks_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeTask MapToEntity(CreateOrEditEmployeeTaskDto input, EmployeeTask entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.TaskTypeId = input.TaskTypeId;
        entity.TaskDate = input.TaskDate.Date;
        entity.TaskStatus = input.TaskStatus;
        entity.ReminderDate = input.ReminderDate;
        entity.MonitoringType = input.MonitoringType;
        entity.MonitoringCount = input.MonitoringCount;
        entity.MonitoringUnit = input.MonitoringUnit;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeTaskDto ToDto(EmployeeTask entity)
    {
        return new EmployeeTaskDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            TaskTypeId = entity.TaskTypeId,
            TaskTypeNameEn = entity.TaskType?.NameEn,
            TaskDate = entity.TaskDate,
            TaskStatus = entity.TaskStatus,
            ReminderDate = entity.ReminderDate,
            MonitoringType = entity.MonitoringType,
            MonitoringCount = entity.MonitoringCount,
            MonitoringUnit = entity.MonitoringUnit,
            Comments = entity.Comments
        };
    }
}
