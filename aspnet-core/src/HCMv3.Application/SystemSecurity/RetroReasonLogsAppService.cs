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
/// Application service for <see cref="RetroReasonLog"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.RetroReasonLog). Plain audit-log CRUD; legacy had no
/// PerformValidation.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_RetroReasonLogs)]
public class RetroReasonLogsAppService : HCMv3AppServiceBase, IRetroReasonLogsAppService
{
    private readonly IRepository<RetroReasonLog> _repository;

    public RetroReasonLogsAppService(IRepository<RetroReasonLog> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RetroReasonLogDto>> GetAll(GetAllRetroReasonLogsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.Employee)
            .WhereIf(input.EmployeeId.HasValue, r => r.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RetroReasonLogDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetRetroReasonLogForEditOutput> GetRetroReasonLogForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetRetroReasonLogForEditOutput
        {
            RetroReasonLog = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditRetroReasonLogDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_RetroReasonLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_RetroReasonLogs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRetroReasonLogDto input)
    {
        var entity = MapToEntity(input, new RetroReasonLog());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_RetroReasonLogs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRetroReasonLogDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static RetroReasonLog MapToEntity(CreateOrEditRetroReasonLogDto input, RetroReasonLog entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.ChangedOn = input.ChangedOn;
        entity.RetroDate = input.RetroDate;
        entity.EntityObjectName = input.EntityObjectName;
        entity.EntityObjectId = input.EntityObjectId;
        entity.RetroReason = input.RetroReason;

        return entity;
    }

    private static RetroReasonLogDto ToDto(RetroReasonLog entity)
    {
        return new RetroReasonLogDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeFullName = entity.Employee != null ? entity.Employee.FirstNameEn : null,
            ChangedOn = entity.ChangedOn,
            RetroDate = entity.RetroDate,
            EntityObjectName = entity.EntityObjectName,
            EntityObjectId = entity.EntityObjectId,
            RetroReason = entity.RetroReason
        };
    }

    private static CreateOrEditRetroReasonLogDto ToCreateOrEditDto(RetroReasonLog entity)
    {
        return new CreateOrEditRetroReasonLogDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            ChangedOn = entity.ChangedOn,
            RetroDate = entity.RetroDate,
            EntityObjectName = entity.EntityObjectName,
            EntityObjectId = entity.EntityObjectId,
            RetroReason = entity.RetroReason
        };
    }
}
