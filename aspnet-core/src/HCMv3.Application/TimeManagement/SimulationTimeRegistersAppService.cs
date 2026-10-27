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
/// Application service for <see cref="SimulationTimeRegister"/> (ported from legacy XAF
/// SimulationTimeRegister business object, Wave 4). "Dry-run" preview header row for an employee/day.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_SimulationTimeRegisters)]
public class SimulationTimeRegistersAppService : HCMv3AppServiceBase, ISimulationTimeRegistersAppService
{
    private readonly IRepository<SimulationTimeRegister> _repository;

    public SimulationTimeRegistersAppService(IRepository<SimulationTimeRegister> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SimulationTimeRegisterDto>> GetAll(GetAllSimulationTimeRegistersInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(s => s.Employee)
            .Include(s => s.DutyPeriod)
            .WhereIf(input.EmployeeId.HasValue, s => s.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SimulationTimeRegisterDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSimulationTimeRegisterForEditOutput> GetSimulationTimeRegisterForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSimulationTimeRegisterForEditOutput
        {
            SimulationTimeRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSimulationTimeRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_SimulationTimeRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSimulationTimeRegisterDto input)
    {
        var entity = MapToEntity(input, new SimulationTimeRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_SimulationTimeRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSimulationTimeRegisterDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_SimulationTimeRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static SimulationTimeRegister MapToEntity(CreateOrEditSimulationTimeRegisterDto input, SimulationTimeRegister entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.DayDate = input.DayDate.Date;
        entity.DutyPeriodId = input.DutyPeriodId;

        return entity;
    }

    private void ValidateAsync(SimulationTimeRegister entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }
    }

    private static SimulationTimeRegisterDto ToDto(SimulationTimeRegister entity)
    {
        return new SimulationTimeRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            DayDate = entity.DayDate,
            DutyPeriodId = entity.DutyPeriodId,
            DutyPeriodName = entity.DutyPeriod?.NameEn
        };
    }

    private static CreateOrEditSimulationTimeRegisterDto ToCreateOrEditDto(SimulationTimeRegister entity)
    {
        return new CreateOrEditSimulationTimeRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            DayDate = entity.DayDate,
            DutyPeriodId = entity.DutyPeriodId
        };
    }
}
