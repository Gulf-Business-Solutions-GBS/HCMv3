using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for <see cref="PayrollLog"/> (ported from legacy XAF PayrollLog business object).
/// No lifecycle overrides/business validation in the legacy source -- plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollLogs)]
public class PayrollLogsAppService : HCMv3AppServiceBase, IPayrollLogsAppService
{
    private readonly IRepository<PayrollLog> _repository;

    public PayrollLogsAppService(IRepository<PayrollLog> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PayrollLogDto>> GetAll(GetAllPayrollLogsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(l => l.Employee)
            .WhereIf(input.EmployeeId.HasValue, l => l.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PayrollLogDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPayrollLogForEditOutput> GetPayrollLogForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetPayrollLogForEditOutput
        {
            PayrollLog = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPayrollLogDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollLogs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPayrollLogDto input)
    {
        var entity = MapToEntity(input, new PayrollLog());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollLogs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPayrollLogDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static PayrollLog MapToEntity(CreateOrEditPayrollLogDto input, PayrollLog entity)
    {
        entity.Message = input.Message;
        entity.RunTimeStamp = input.RunTimeStamp;
        entity.EmployeeId = input.EmployeeId;
        entity.Status = input.Status;
        entity.InPeriod = input.InPeriod;
        entity.StopFlag = input.StopFlag;
        entity.NoOfEmployee = input.NoOfEmployee;

        return entity;
    }

    private static PayrollLogDto ToDto(PayrollLog entity)
    {
        return new PayrollLogDto
        {
            Id = entity.Id,
            Message = entity.Message,
            RunTimeStamp = entity.RunTimeStamp,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.Employee?.FullNameEn,
            Status = entity.Status,
            InPeriod = entity.InPeriod,
            StopFlag = entity.StopFlag,
            NoOfEmployee = entity.NoOfEmployee
        };
    }

    private static CreateOrEditPayrollLogDto ToCreateOrEditDto(PayrollLog entity)
    {
        return new CreateOrEditPayrollLogDto
        {
            Id = entity.Id,
            Message = entity.Message,
            RunTimeStamp = entity.RunTimeStamp,
            EmployeeId = entity.EmployeeId,
            Status = entity.Status,
            InPeriod = entity.InPeriod,
            StopFlag = entity.StopFlag,
            NoOfEmployee = entity.NoOfEmployee
        };
    }
}
