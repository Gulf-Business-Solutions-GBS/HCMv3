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
/// Application service for the calculated payroll cost-distribution header (ported from legacy XAF
/// CostRegister business object). System-generated payroll-calculation output — no business validation in
/// the legacy source (plain CRUD).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostRegisters)]
public class CostRegistersAppService : HCMv3AppServiceBase, ICostRegistersAppService
{
    private readonly IRepository<CostRegister> _costRegisterRepository;

    public CostRegistersAppService(IRepository<CostRegister> costRegisterRepository)
    {
        _costRegisterRepository = costRegisterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CostRegisterDto>> GetAll(GetAllCostRegistersInput input)
    {
        input.Normalize();

        var query = _costRegisterRepository.GetAll()
            .Include(c => c.Employee)
            .WhereIf(input.EmployeeId.HasValue, c => c.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CostRegisterDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    /// <summary>One register with its display names, for the register's own page.</summary>
    public async Task<CostRegisterDto> GetCostRegister(EntityDto input)
    {
        var entity = await _costRegisterRepository.GetAll()
            .Include(c => c.Employee)
            .FirstOrDefaultAsync(c => c.Id == input.Id)
            ?? throw new Abp.Domain.Entities.EntityNotFoundException(typeof(CostRegister), input.Id);

        return ToDto(entity);
    }

    public async Task<GetCostRegisterForEditOutput> GetCostRegisterForEdit(EntityDto input)
    {
        var entity = await _costRegisterRepository.GetAsync(input.Id);

        return new GetCostRegisterForEditOutput
        {
            CostRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCostRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _costRegisterRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCostRegisterDto input)
    {
        var entity = MapToEntity(input, new CostRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _costRegisterRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_CostRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCostRegisterDto input)
    {
        var entity = await _costRegisterRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _costRegisterRepository.UpdateAsync(entity);
    }

    private static CostRegister MapToEntity(CreateOrEditCostRegisterDto input, CostRegister entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.Period = input.Period;
        entity.PayrollRunType = input.PayrollRunType;

        return entity;
    }

    private static CostRegisterDto ToDto(CostRegister entity)
    {
        return new CostRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            Period = entity.Period,
            PayrollRunType = entity.PayrollRunType
        };
    }

    private static CreateOrEditCostRegisterDto ToCreateOrEditDto(CostRegister entity)
    {
        return new CreateOrEditCostRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            Period = entity.Period,
            PayrollRunType = entity.PayrollRunType
        };
    }
}
