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
/// Application service for <see cref="WageTypeRegister"/> (ported from legacy XAF WageTypeRegister
/// business object -- a payroll-run tracking log row, NOT a WageType detail table despite the name; see
/// <see cref="WageTypeRegister"/> class remarks). No business validation is ported (the legacy has none
/// beyond plain CRUD).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeRegisters)]
public class WageTypeRegistersAppService : HCMv3AppServiceBase, IWageTypeRegistersAppService
{
    private readonly IRepository<WageTypeRegister> _repository;

    public WageTypeRegistersAppService(IRepository<WageTypeRegister> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WageTypeRegisterDto>> GetAll(GetAllWageTypeRegistersInput input)
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

        return new PagedResultDto<WageTypeRegisterDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetWageTypeRegisterForEditOutput> GetWageTypeRegisterForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetWageTypeRegisterForEditOutput
        {
            WageTypeRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditWageTypeRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWageTypeRegisterDto input)
    {
        var entity = MapToEntity(input, new WageTypeRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_WageTypeRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWageTypeRegisterDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static WageTypeRegister MapToEntity(CreateOrEditWageTypeRegisterDto input, WageTypeRegister entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.ForPeriod = input.ForPeriod;
        entity.InPeriod = input.InPeriod;

        return entity;
    }

    private static WageTypeRegisterDto ToDto(WageTypeRegister entity)
    {
        return new WageTypeRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.Employee?.FullNameEn,
            ForPeriod = entity.ForPeriod,
            InPeriod = entity.InPeriod
        };
    }

    private static CreateOrEditWageTypeRegisterDto ToCreateOrEditDto(WageTypeRegister entity)
    {
        return new CreateOrEditWageTypeRegisterDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            ForPeriod = entity.ForPeriod,
            InPeriod = entity.InPeriod
        };
    }
}
