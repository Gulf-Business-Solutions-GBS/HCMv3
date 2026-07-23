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
/// Application service for the wage-type calculation line of a <see cref="SimulationSalaryRegister"/>
/// (ported from legacy XAF SimulationSalaryRegisterDetail business object), listed separately and linked
/// back via <see cref="SimulationSalaryRegisterDetail.SimulationSalaryRegisterId"/>. No lock guard (see
/// <see cref="SimulationSalaryRegister"/> remarks) — plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDetails)]
public class SimulationSalaryRegisterDetailsAppService : HCMv3AppServiceBase, ISimulationSalaryRegisterDetailsAppService
{
    private readonly IRepository<SimulationSalaryRegisterDetail> _simulationSalaryRegisterDetailRepository;

    public SimulationSalaryRegisterDetailsAppService(IRepository<SimulationSalaryRegisterDetail> simulationSalaryRegisterDetailRepository)
    {
        _simulationSalaryRegisterDetailRepository = simulationSalaryRegisterDetailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SimulationSalaryRegisterDetailDto>> GetAll(GetAllSimulationSalaryRegisterDetailsInput input)
    {
        input.Normalize();

        var query = _simulationSalaryRegisterDetailRepository.GetAll()
            .Include(d => d.WageType)
            .WhereIf(input.SimulationSalaryRegisterId.HasValue, d => d.SimulationSalaryRegisterId == input.SimulationSalaryRegisterId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SimulationSalaryRegisterDetailDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSimulationSalaryRegisterDetailForEditOutput> GetSimulationSalaryRegisterDetailForEdit(EntityDto input)
    {
        var entity = await _simulationSalaryRegisterDetailRepository.GetAsync(input.Id);

        return new GetSimulationSalaryRegisterDetailForEditOutput
        {
            SimulationSalaryRegisterDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSimulationSalaryRegisterDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _simulationSalaryRegisterDetailRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSimulationSalaryRegisterDetailDto input)
    {
        var entity = MapToEntity(input, new SimulationSalaryRegisterDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _simulationSalaryRegisterDetailRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisterDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSimulationSalaryRegisterDetailDto input)
    {
        var entity = await _simulationSalaryRegisterDetailRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _simulationSalaryRegisterDetailRepository.UpdateAsync(entity);
    }

    private static SimulationSalaryRegisterDetail MapToEntity(CreateOrEditSimulationSalaryRegisterDetailDto input, SimulationSalaryRegisterDetail entity)
    {
        entity.SimulationSalaryRegisterId = input.SimulationSalaryRegisterId;
        entity.WageTypeId = input.WageTypeId;
        entity.Rate = input.Rate;
        entity.Number = input.Number;
        entity.Amount = input.Amount;

        return entity;
    }

    private static SimulationSalaryRegisterDetailDto ToDto(SimulationSalaryRegisterDetail entity)
    {
        return new SimulationSalaryRegisterDetailDto
        {
            Id = entity.Id,
            SimulationSalaryRegisterId = entity.SimulationSalaryRegisterId,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn,
            Rate = entity.Rate,
            Number = entity.Number,
            Amount = entity.Amount
        };
    }

    private static CreateOrEditSimulationSalaryRegisterDetailDto ToCreateOrEditDto(SimulationSalaryRegisterDetail entity)
    {
        return new CreateOrEditSimulationSalaryRegisterDetailDto
        {
            Id = entity.Id,
            SimulationSalaryRegisterId = entity.SimulationSalaryRegisterId,
            WageTypeId = entity.WageTypeId,
            Rate = entity.Rate,
            Number = entity.Number,
            Amount = entity.Amount
        };
    }
}
