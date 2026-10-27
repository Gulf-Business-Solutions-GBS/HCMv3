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
/// Application service for the per-TimeType child of <see cref="SimulationTimeRegister"/> (ported from
/// legacy XAF SimulationTimeRegisterDetail business object, Wave 4), listed separately and linked back via
/// <see cref="SimulationTimeRegisterDetail.SimulationTimeRegisterId"/>. Same shape/dead-code-skip as
/// <see cref="MonthlyTimeRegisterDetailsAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_SimulationTimeRegisterDetails)]
public class SimulationTimeRegisterDetailsAppService : HCMv3AppServiceBase, ISimulationTimeRegisterDetailsAppService
{
    private readonly IRepository<SimulationTimeRegisterDetail> _repository;

    public SimulationTimeRegisterDetailsAppService(IRepository<SimulationTimeRegisterDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SimulationTimeRegisterDetailDto>> GetAll(GetAllSimulationTimeRegisterDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.TimeType)
            .WhereIf(input.SimulationTimeRegisterId.HasValue, d => d.SimulationTimeRegisterId == input.SimulationTimeRegisterId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SimulationTimeRegisterDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSimulationTimeRegisterDetailForEditOutput> GetSimulationTimeRegisterDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSimulationTimeRegisterDetailForEditOutput
        {
            SimulationTimeRegisterDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSimulationTimeRegisterDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_SimulationTimeRegisterDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSimulationTimeRegisterDetailDto input)
    {
        var entity = MapToEntity(input, new SimulationTimeRegisterDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_SimulationTimeRegisterDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSimulationTimeRegisterDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_SimulationTimeRegisterDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static SimulationTimeRegisterDetail MapToEntity(CreateOrEditSimulationTimeRegisterDetailDto input, SimulationTimeRegisterDetail entity)
    {
        entity.SimulationTimeRegisterId = input.SimulationTimeRegisterId;
        entity.TimeTypeId = input.TimeTypeId;
        entity.TimeValue = input.TimeValue;

        return entity;
    }

    private void ValidateAsync(SimulationTimeRegisterDetail entity)
    {
        if (entity.SimulationTimeRegisterId <= 0)
        {
            throw new UserFriendlyException(L("SimulationTimeRegisterCannotBeBlank"));
        }

        if (entity.TimeTypeId <= 0)
        {
            throw new UserFriendlyException(L("TimeTypeCannotBeBlank"));
        }
    }

    private static SimulationTimeRegisterDetailDto ToDto(SimulationTimeRegisterDetail entity)
    {
        return new SimulationTimeRegisterDetailDto
        {
            Id = entity.Id,
            SimulationTimeRegisterId = entity.SimulationTimeRegisterId,
            TimeTypeId = entity.TimeTypeId,
            TimeTypeName = entity.TimeType?.NameEn,
            TimeValue = entity.TimeValue
        };
    }

    private static CreateOrEditSimulationTimeRegisterDetailDto ToCreateOrEditDto(SimulationTimeRegisterDetail entity)
    {
        return new CreateOrEditSimulationTimeRegisterDetailDto
        {
            Id = entity.Id,
            SimulationTimeRegisterId = entity.SimulationTimeRegisterId,
            TimeTypeId = entity.TimeTypeId,
            TimeValue = entity.TimeValue
        };
    }
}
