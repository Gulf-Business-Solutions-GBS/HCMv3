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
/// Application service for the per-day breakdown of a <see cref="SimulationSalaryRegisterDetail"/> (ported
/// from legacy XAF SimulationSalaryRegisterDaysDetail business object), listed separately and linked back
/// via <see cref="SimulationSalaryRegisterDaysDetail.SimulationSalaryRegisterDetailId"/>. No lock guard
/// (see <see cref="SimulationSalaryRegister"/> remarks) — plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails)]
public class SimulationSalaryRegisterDaysDetailsAppService : HCMv3AppServiceBase, ISimulationSalaryRegisterDaysDetailsAppService
{
    private readonly IRepository<SimulationSalaryRegisterDaysDetail> _simulationSalaryRegisterDaysDetailRepository;

    public SimulationSalaryRegisterDaysDetailsAppService(IRepository<SimulationSalaryRegisterDaysDetail> simulationSalaryRegisterDaysDetailRepository)
    {
        _simulationSalaryRegisterDaysDetailRepository = simulationSalaryRegisterDaysDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SimulationSalaryRegisterDaysDetailDto>> GetAll(GetAllSimulationSalaryRegisterDaysDetailsInput input)
    {
        input.Normalize();

        var query = _simulationSalaryRegisterDaysDetailRepository.GetAll()
            .WhereIf(input.SimulationSalaryRegisterDetailId.HasValue, d => d.SimulationSalaryRegisterDetailId == input.SimulationSalaryRegisterDetailId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SimulationSalaryRegisterDaysDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSimulationSalaryRegisterDaysDetailForEditOutput> GetSimulationSalaryRegisterDaysDetailForEdit(EntityDto input)
    {
        var entity = await _simulationSalaryRegisterDaysDetailRepository.GetAsync(input.Id);

        return new GetSimulationSalaryRegisterDaysDetailForEditOutput
        {
            SimulationSalaryRegisterDaysDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSimulationSalaryRegisterDaysDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _simulationSalaryRegisterDaysDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSimulationSalaryRegisterDaysDetailDto input)
    {
        var entity = MapToEntity(input, new SimulationSalaryRegisterDaysDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _simulationSalaryRegisterDaysDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDaysDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSimulationSalaryRegisterDaysDetailDto input)
    {
        var entity = await _simulationSalaryRegisterDaysDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _simulationSalaryRegisterDaysDetailRepository.UpdateAsync(entity);
    }

    private static SimulationSalaryRegisterDaysDetail MapToEntity(CreateOrEditSimulationSalaryRegisterDaysDetailDto input, SimulationSalaryRegisterDaysDetail entity)
    {
        entity.SimulationSalaryRegisterDetailId = input.SimulationSalaryRegisterDetailId;
        entity.Period = input.Period;
        entity.Rate = input.Rate;
        entity.Number = input.Number;
        entity.Amount = input.Amount;

        return entity;
    }

    private static SimulationSalaryRegisterDaysDetailDto ToDto(SimulationSalaryRegisterDaysDetail entity)
    {
        return new SimulationSalaryRegisterDaysDetailDto
        {
            Id = entity.Id,
            SimulationSalaryRegisterDetailId = entity.SimulationSalaryRegisterDetailId,
            Period = entity.Period,
            Rate = entity.Rate,
            Number = entity.Number,
            Amount = entity.Amount
        };
    }

    private static CreateOrEditSimulationSalaryRegisterDaysDetailDto ToCreateOrEditDto(SimulationSalaryRegisterDaysDetail entity)
    {
        return new CreateOrEditSimulationSalaryRegisterDaysDetailDto
        {
            Id = entity.Id,
            SimulationSalaryRegisterDetailId = entity.SimulationSalaryRegisterDetailId,
            Period = entity.Period,
            Rate = entity.Rate,
            Number = entity.Number,
            Amount = entity.Amount
        };
    }
}
