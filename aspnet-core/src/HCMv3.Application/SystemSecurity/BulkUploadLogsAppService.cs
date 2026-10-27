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
/// Application service for <see cref="BulkUploadLog"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.BulkUploadLog). Plain audit-log CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_BulkUploadLogs)]
public class BulkUploadLogsAppService : HCMv3AppServiceBase, IBulkUploadLogsAppService
{
    private readonly IRepository<BulkUploadLog> _repository;

    public BulkUploadLogsAppService(IRepository<BulkUploadLog> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<BulkUploadLogDto>> GetAll(GetAllBulkUploadLogsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(l => l.Employee)
            .WhereIf(input.LogType.HasValue, l => l.LogType == input.LogType);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<BulkUploadLogDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetBulkUploadLogForEditOutput> GetBulkUploadLogForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetBulkUploadLogForEditOutput
        {
            BulkUploadLog = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditBulkUploadLogDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_BulkUploadLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_BulkUploadLogs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditBulkUploadLogDto input)
    {
        var entity = MapToEntity(input, new BulkUploadLog());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_BulkUploadLogs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditBulkUploadLogDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static BulkUploadLog MapToEntity(CreateOrEditBulkUploadLogDto input, BulkUploadLog entity)
    {
        entity.LogType = input.LogType;
        entity.Message = input.Message;
        entity.RunTimeStamp = input.RunTimeStamp;
        entity.EmployeeId = input.EmployeeId;
        entity.Status = input.Status;
        entity.InPeriod = input.InPeriod;
        entity.StopFlag = input.StopFlag;
        entity.NoOfEmployee = input.NoOfEmployee;
        entity.RecordNo = input.RecordNo;

        return entity;
    }

    private static BulkUploadLogDto ToDto(BulkUploadLog entity)
    {
        return new BulkUploadLogDto
        {
            Id = entity.Id,
            LogType = entity.LogType,
            Message = entity.Message,
            RunTimeStamp = entity.RunTimeStamp,
            EmployeeId = entity.EmployeeId,
            EmployeeFullName = entity.Employee != null ? entity.Employee.FirstNameEn : null,
            Status = entity.Status,
            InPeriod = entity.InPeriod,
            StopFlag = entity.StopFlag,
            NoOfEmployee = entity.NoOfEmployee,
            RecordNo = entity.RecordNo
        };
    }

    private static CreateOrEditBulkUploadLogDto ToCreateOrEditDto(BulkUploadLog entity)
    {
        return new CreateOrEditBulkUploadLogDto
        {
            Id = entity.Id,
            LogType = entity.LogType,
            Message = entity.Message,
            RunTimeStamp = entity.RunTimeStamp,
            EmployeeId = entity.EmployeeId,
            Status = entity.Status,
            InPeriod = entity.InPeriod,
            StopFlag = entity.StopFlag,
            NoOfEmployee = entity.NoOfEmployee,
            RecordNo = entity.RecordNo
        };
    }
}
