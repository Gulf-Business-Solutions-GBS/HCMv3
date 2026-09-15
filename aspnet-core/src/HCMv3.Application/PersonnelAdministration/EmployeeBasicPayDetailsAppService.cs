using System;
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
/// Application service for <see cref="EmployeeBasicPayDetail"/> (ported from legacy XAF
/// EmployeeBasicPayDetail business object, Wave 2 deferred bucket). Records the wage-type detail row
/// itself, with the essential active validation ported (no duplicate WageType per BasicPay header). As in legacy,
/// every save or delete sets the parent <see cref="EmployeeBasicPay.Amount"/> to the total of its detail lines,
/// so the lines can be edited from the employee's Basic Pay tab and the header total follows.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails)]
public class EmployeeBasicPayDetailsAppService : HCMv3AppServiceBase, IEmployeeBasicPayDetailsAppService
{
    private readonly IRepository<EmployeeBasicPayDetail> _repository;
    private readonly IRepository<EmployeeBasicPay> _basicPayRepository;

    public EmployeeBasicPayDetailsAppService(
        IRepository<EmployeeBasicPayDetail> repository,
        IRepository<EmployeeBasicPay> basicPayRepository)
    {
        _repository = repository;
        _basicPayRepository = basicPayRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeBasicPayDetailDto>> GetAll(GetAllEmployeeBasicPayDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.WageType)
            .Include(d => d.Currency)
            .Include(d => d.DependantWageType)
            .WhereIf(input.EmployeeBasicPayId.HasValue, d => d.EmployeeBasicPayId == input.EmployeeBasicPayId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeBasicPayDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeBasicPayDetailForEditOutput> GetEmployeeBasicPayDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeBasicPayDetailForEditOutput
        {
            EmployeeBasicPayDetail = new CreateOrEditEmployeeBasicPayDetailDto
            {
                Id = entity.Id,
                EmployeeBasicPayId = entity.EmployeeBasicPayId,
                WageTypeId = entity.WageTypeId,
                Amount = entity.Amount,
                CurrencyId = entity.CurrencyId,
                DependantNumber = entity.DependantNumber,
                DependantUnit = entity.DependantUnit,
                DependantWageTypeId = entity.DependantWageTypeId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeBasicPayDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeBasicPayDetailDto input)
    {
        var entity = MapToEntity(input, new EmployeeBasicPayDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
        await UpdateBasicPayTotalAsync(entity.EmployeeBasicPayId);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeBasicPayDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var previousBasicPayId = entity.EmployeeBasicPayId;
        MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
        await UpdateBasicPayTotalAsync(entity.EmployeeBasicPayId);
        if (previousBasicPayId != entity.EmployeeBasicPayId)
        {
            await UpdateBasicPayTotalAsync(previousBasicPayId);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        await _repository.DeleteAsync(entity);
        await UpdateBasicPayTotalAsync(entity.EmployeeBasicPayId);
    }

    /// <summary>Sets the basic pay header's amount to the total of its remaining detail lines.</summary>
    private async Task UpdateBasicPayTotalAsync(int employeeBasicPayId)
    {
        await CurrentUnitOfWork.SaveChangesAsync();

        var basicPay = await _basicPayRepository.FirstOrDefaultAsync(employeeBasicPayId);
        if (basicPay == null)
        {
            return;
        }

        var total = await _repository.GetAll()
            .Where(d => d.EmployeeBasicPayId == employeeBasicPayId)
            .SumAsync(d => d.Amount);
        basicPay.Amount = Math.Round(total, 3);
    }

    private async Task ValidateAsync(EmployeeBasicPayDetail entity)
    {
        var duplicateWageType = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id
                           && d.EmployeeBasicPayId == entity.EmployeeBasicPayId
                           && d.WageTypeId == entity.WageTypeId);
        if (duplicateWageType)
        {
            throw new UserFriendlyException(L("WageTypeAlreadyExistsForBasicPay"));
        }
    }

    private static EmployeeBasicPayDetail MapToEntity(CreateOrEditEmployeeBasicPayDetailDto input, EmployeeBasicPayDetail entity)
    {
        entity.EmployeeBasicPayId = input.EmployeeBasicPayId;
        entity.WageTypeId = input.WageTypeId;
        entity.Amount = input.Amount;
        entity.CurrencyId = input.CurrencyId;
        entity.DependantNumber = input.DependantNumber;
        entity.DependantUnit = input.DependantUnit;
        entity.DependantWageTypeId = input.DependantWageTypeId;
        return entity;
    }

    private static EmployeeBasicPayDetailDto ToDto(EmployeeBasicPayDetail entity)
    {
        return new EmployeeBasicPayDetailDto
        {
            Id = entity.Id,
            EmployeeBasicPayId = entity.EmployeeBasicPayId,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn,
            Amount = entity.Amount,
            CurrencyId = entity.CurrencyId,
            CurrencyName = entity.Currency?.NameEn,
            DependantNumber = entity.DependantNumber,
            DependantUnit = entity.DependantUnit,
            DependantWageTypeId = entity.DependantWageTypeId,
            DependantWageTypeName = entity.DependantWageType?.NameEn
        };
    }
}
