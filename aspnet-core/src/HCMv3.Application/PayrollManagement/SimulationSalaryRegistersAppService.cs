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
/// Application service for the "what-if" trial payroll run header (ported from legacy XAF
/// SimulationSalaryRegister business object). NO lock guard exists in the legacy source for this sandbox
/// family (see <see cref="SimulationSalaryRegister"/> remarks) — plain CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisters)]
public class SimulationSalaryRegistersAppService : HCMv3AppServiceBase, ISimulationSalaryRegistersAppService
{
    private readonly IRepository<SimulationSalaryRegister> _simulationSalaryRegisterRepository;

    public SimulationSalaryRegistersAppService(IRepository<SimulationSalaryRegister> simulationSalaryRegisterRepository)
    {
        _simulationSalaryRegisterRepository = simulationSalaryRegisterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SimulationSalaryRegisterDto>> GetAll(GetAllSimulationSalaryRegistersInput input)
    {
        input.Normalize();

        var query = _simulationSalaryRegisterRepository.GetAll()
            .Include(r => r.Employee)
            .Include(r => r.PayrollReasonType)
            .WhereIf(input.EmployeeId.HasValue, r => r.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SimulationSalaryRegisterDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    /// <summary>One register with its display names, for the register's own page.</summary>
    public async Task<SimulationSalaryRegisterDto> GetSimulationSalaryRegister(EntityDto input)
    {
        var entity = await _simulationSalaryRegisterRepository.GetAll()
            .Include(r => r.Employee)
            .Include(r => r.PayrollReasonType)
            .FirstOrDefaultAsync(r => r.Id == input.Id)
            ?? throw new Abp.Domain.Entities.EntityNotFoundException(typeof(SimulationSalaryRegister), input.Id);

        return ToDto(entity);
    }

    public async Task<GetSimulationSalaryRegisterForEditOutput> GetSimulationSalaryRegisterForEdit(EntityDto input)
    {
        var entity = await _simulationSalaryRegisterRepository.GetAsync(input.Id);

        return new GetSimulationSalaryRegisterForEditOutput
        {
            SimulationSalaryRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSimulationSalaryRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _simulationSalaryRegisterRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSimulationSalaryRegisterDto input)
    {
        var entity = MapToEntity(input, new SimulationSalaryRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _simulationSalaryRegisterRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SimulationSalaryRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSimulationSalaryRegisterDto input)
    {
        var entity = await _simulationSalaryRegisterRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _simulationSalaryRegisterRepository.UpdateAsync(entity);
    }

    private static SimulationSalaryRegister MapToEntity(CreateOrEditSimulationSalaryRegisterDto input, SimulationSalaryRegister entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.ForPeriod = input.ForPeriod;
        entity.InPeriod = input.InPeriod;
        entity.PayrollRunType = input.PayrollRunType;
        entity.PayrollResultType = input.PayrollResultType;
        // The column is optional (a regular run has no reason); the DTO still carries 0 for "none".
        entity.PayrollReasonTypeId = input.PayrollReasonTypeId == 0 ? null : input.PayrollReasonTypeId;

        return entity;
    }

    private static SimulationSalaryRegisterDto ToDto(SimulationSalaryRegister entity)
    {
        return new SimulationSalaryRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            ForPeriod = entity.ForPeriod,
            InPeriod = entity.InPeriod,
            PayrollRunType = entity.PayrollRunType,
            PayrollResultType = entity.PayrollResultType,
            PayrollReasonTypeId = entity.PayrollReasonTypeId ?? 0,
            PayrollReasonTypeName = entity.PayrollReasonType?.NameEn
        };
    }

    private static CreateOrEditSimulationSalaryRegisterDto ToCreateOrEditDto(SimulationSalaryRegister entity)
    {
        return new CreateOrEditSimulationSalaryRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            ForPeriod = entity.ForPeriod,
            InPeriod = entity.InPeriod,
            PayrollRunType = entity.PayrollRunType,
            PayrollResultType = entity.PayrollResultType,
            PayrollReasonTypeId = entity.PayrollReasonTypeId ?? 0
        };
    }
}
