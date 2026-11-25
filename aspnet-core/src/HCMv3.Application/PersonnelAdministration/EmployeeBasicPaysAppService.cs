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
/// Application service for <see cref="EmployeeBasicPay"/> (ported from legacy XAF EmployeeBasicPay business
/// object). Ports the required fields (DTO annotations), no duplicate Effective Date per employee, and the
/// legacy end-date cascade: the end date is never entered, each record ends the day before the employee's next
/// one and the latest stays open-ended, recomputed for all the employee's records on every save and delete.
/// The detail-driven Amount total is maintained by <see cref="EmployeeBasicPayDetailsAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays)]
public class EmployeeBasicPaysAppService : HCMv3AppServiceBase, IEmployeeBasicPaysAppService
{
    public static readonly DateTime OpenEnded = new(9999, 12, 31);

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
                PayType = entity.PayType,
                PayInterval = entity.PayInterval,
                EmployeeActionId = entity.EmployeeActionId
            }
        };
    }

    public async Task<int> CreateOrEdit(CreateOrEditEmployeeBasicPayDto input)
    {
        if (input.Id.HasValue)
        {
            return await UpdateAsync(input);
        }

        return await CreateAsync(input);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Create)]
    protected virtual async Task<int> CreateAsync(CreateOrEditEmployeeBasicPayDto input)
    {
        var entity = MapToEntity(input, new EmployeeBasicPay());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);
        var id = await _repository.InsertAndGetIdAsync(entity);

        await RecalculateEndDatesAsync(entity.EmployeeId);
        return id;
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Edit)]
    protected virtual async Task<int> UpdateAsync(CreateOrEditEmployeeBasicPayDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        var previousEmployeeId = entity.EmployeeId;
        MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await RecalculateEndDatesAsync(entity.EmployeeId);
        if (previousEmployeeId != entity.EmployeeId)
        {
            await RecalculateEndDatesAsync(previousEmployeeId);
        }

        return entity.Id;
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        await _repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await RecalculateEndDatesAsync(entity.EmployeeId);
    }

    /// <summary>
    /// Legacy <c>EmployeeBasicPay.SetEndDate</c>: every record of the employee ends the day before the next
    /// Effective Date, and the latest one is open-ended, so the periods never overlap or leave gaps.
    /// </summary>
    private async Task RecalculateEndDatesAsync(int employeeId)
    {
        var records = await _repository.GetAll()
            .Where(p => p.EmployeeId == employeeId)
            .OrderBy(p => p.StartDate)
            .ToListAsync();

        for (var i = 0; i < records.Count; i++)
        {
            records[i].EndDate = i + 1 < records.Count ? records[i + 1].StartDate.Date.AddDays(-1) : OpenEnded;
        }

        await CurrentUnitOfWork.SaveChangesAsync();
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
        if (entity.EndDate == default)
        {
            // Placeholder until RecalculateEndDatesAsync runs after the save.
            entity.EndDate = OpenEnded;
        }
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
