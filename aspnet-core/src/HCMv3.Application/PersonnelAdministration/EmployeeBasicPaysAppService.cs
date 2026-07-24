using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeBasicPay"/> (ported from legacy XAF EmployeeBasicPay business
/// object, Wave 2 deferred bucket). Ports the essential active validations only (required fields via DTO
/// annotations, no duplicate StartDate per employee). The legacy auto-EndDate cascade against sibling
/// records and the detail-driven Amount total are NOT recomputed here beyond what
/// <see cref="EmployeeBasicPayDetailsAppService"/> already maintains -- see class remarks on
/// <see cref="EmployeeBasicPay"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays)]
public class EmployeeBasicPaysAppService : HCMv3AppServiceBase, IEmployeeBasicPaysAppService
{
    private readonly IRepository<EmployeeBasicPay> _repository;

    public EmployeeBasicPaysAppService(IRepository<EmployeeBasicPay> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeBasicPayDto>> GetAll(GetAllEmployeeBasicPaysInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(p => p.Employee)
            .WhereIf(input.EmployeeId.HasValue, p => p.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeBasicPayDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeBasicPayForEditOutput> GetEmployeeBasicPayForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeBasicPayForEditOutput
        {
            EmployeeBasicPay = new CreateOrEditEmployeeBasicPayDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                PayType = entity.PayType,
                PayInterval = entity.PayInterval,
                EmployeeActionId = entity.EmployeeActionId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeBasicPayDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeBasicPayDto input)
    {
        var entity = MapToEntity(input, new EmployeeBasicPay());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeBasicPayDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(EmployeeBasicPay entity)
    {
        var duplicateStartDate = await _repository.GetAll()
            .AnyAsync(p => p.Id != entity.Id
                           && p.EmployeeId == entity.EmployeeId
                           && p.StartDate.Date == entity.StartDate.Date);
        if (duplicateStartDate)
        {
            throw new UserFriendlyException(L("StartDateAlreadyExists", entity.StartDate.ToString("dd/MM/yyyy")));
        }
    }

    private static EmployeeBasicPay MapToEntity(CreateOrEditEmployeeBasicPayDto input, EmployeeBasicPay entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate == default ? new System.DateTime(9999, 12, 31) : input.EndDate.Date;
        entity.PayType = input.PayType;
        entity.PayInterval = input.PayInterval;
        entity.EmployeeActionId = input.EmployeeActionId;
        return entity;
    }

    private static EmployeeBasicPayDto ToDto(EmployeeBasicPay entity)
    {
        return new EmployeeBasicPayDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Amount = entity.Amount,
            PayType = entity.PayType,
            PayInterval = entity.PayInterval,
            EmployeeActionId = entity.EmployeeActionId
        };
    }
}
