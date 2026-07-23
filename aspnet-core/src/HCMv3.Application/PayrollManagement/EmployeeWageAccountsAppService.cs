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
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for <see cref="EmployeeWageAccount"/> (ported from legacy XAF
/// EmployeeWageAccount business object). Ports the legacy EndDate auto-chaining (<c>SetEndDate</c>),
/// mirroring <c>OrgUnitWageAccGroupsAppService</c>'s/<c>TicketFaresAppService</c>'s pattern, scoped by
/// (EmployeeId, WageTypeId). See <see cref="EmployeeWageAccount"/> class remarks for the dropped
/// CostElement-slot CostElementType cross-check.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeWageAccounts)]
public class EmployeeWageAccountsAppService : HCMv3AppServiceBase, IEmployeeWageAccountsAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<EmployeeWageAccount> _repository;

    public EmployeeWageAccountsAppService(IRepository<EmployeeWageAccount> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeWageAccountDto>> GetAll(GetAllEmployeeWageAccountsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.WageType)
            .Include(a => a.AccountInfo)
            .Include(a => a.OffsetAccountInfo)
            .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeWageAccountDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeWageAccountForEditOutput> GetEmployeeWageAccountForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeWageAccountForEditOutput
        {
            EmployeeWageAccount = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeWageAccountDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeWageAccounts_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeWageAccountDto input)
    {
        var entity = MapToEntity(input, new EmployeeWageAccount());
        entity.TenantId = AbpSession.TenantId ?? 0;
        entity.EndDate = InfiniteEndDate;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.EmployeeId, entity.WageTypeId);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeWageAccounts_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeWageAccountDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.EmployeeId, entity.WageTypeId);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeWageAccounts_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var employeeId = entity.EmployeeId;
        var wageTypeId = entity.WageTypeId;

        await _repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(employeeId, wageTypeId);
    }

    private static EmployeeWageAccount MapToEntity(CreateOrEditEmployeeWageAccountDto input, EmployeeWageAccount entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.WageTypeId = input.WageTypeId;
        entity.StartDate = input.StartDate.Date;
        entity.AccountInfoId = input.AccountInfoId;
        entity.OffsetAccountInfoId = input.OffsetAccountInfoId;
        entity.CostElement1Id = input.CostElement1Id;
        entity.CostElement2Id = input.CostElement2Id;
        entity.CostElement3Id = input.CostElement3Id;
        entity.CostElement4Id = input.CostElement4Id;
        entity.CostElement5Id = input.CostElement5Id;
        entity.CostElement6Id = input.CostElement6Id;
        entity.CostElement7Id = input.CostElement7Id;
        entity.CostPercentage = input.CostPercentage;
        entity.MergeOffsetAccount = input.MergeOffsetAccount;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: required fields, no duplicate (EmployeeId, WageTypeId,
    /// StartDate, AccountInfoId, CostElement1..7) combination, and the aggregate rule that the sum of
    /// CostPercentage across all rows sharing the same (EmployeeId, WageTypeId, StartDate) must not exceed
    /// 100. The legacy CostElement-slot CostElementType cross-check is NOT ported (see class remarks).
    /// </summary>
    private async Task ValidateAsync(EmployeeWageAccount entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(a => a.Id != entity.Id &&
                           a.EmployeeId == entity.EmployeeId &&
                           a.WageTypeId == entity.WageTypeId &&
                           a.StartDate == entity.StartDate &&
                           a.AccountInfoId == entity.AccountInfoId &&
                           a.CostElement1Id == entity.CostElement1Id &&
                           a.CostElement2Id == entity.CostElement2Id &&
                           a.CostElement3Id == entity.CostElement3Id &&
                           a.CostElement4Id == entity.CostElement4Id &&
                           a.CostElement5Id == entity.CostElement5Id &&
                           a.CostElement6Id == entity.CostElement6Id &&
                           a.CostElement7Id == entity.CostElement7Id);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }

        var existingTotal = await _repository.GetAll()
            .Where(a => a.Id != entity.Id &&
                        a.EmployeeId == entity.EmployeeId &&
                        a.WageTypeId == entity.WageTypeId &&
                        a.StartDate == entity.StartDate)
            .SumAsync(a => (double?)a.CostPercentage) ?? 0;

        var totalCostPercentage = existingTotal + entity.CostPercentage;
        if (totalCostPercentage > 100)
        {
            throw new UserFriendlyException(L("TotalCostPercentageCannotExceed100"));
        }
    }

    /// <summary>
    /// Legacy <c>SetEndDate</c> parity: for each remaining record of the (Employee, WageType) scope
    /// (ordered by StartDate), EndDate = (next record's StartDate - 1 day), or infinite for the last one.
    /// </summary>
    private async Task SetEndDatesAsync(int employeeId, int wageTypeId)
    {
        var records = await _repository.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.WageTypeId == wageTypeId)
            .OrderBy(a => a.StartDate)
            .ToListAsync();

        for (var i = 0; i < records.Count; i++)
        {
            var next = records.Skip(i + 1).FirstOrDefault();
            var newEndDate = next != null ? next.StartDate.AddDays(-1) : InfiniteEndDate;
            if (records[i].EndDate != newEndDate)
            {
                records[i].EndDate = newEndDate;
            }
        }
    }

    private static EmployeeWageAccountDto ToDto(EmployeeWageAccount entity)
    {
        return new EmployeeWageAccountDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.Employee?.FullNameEn,
            WageTypeId = entity.WageTypeId,
            WageTypeName = entity.WageType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            AccountInfoId = entity.AccountInfoId,
            AccountInfoName = entity.AccountInfo?.NameEn,
            OffsetAccountInfoId = entity.OffsetAccountInfoId,
            OffsetAccountInfoName = entity.OffsetAccountInfo?.NameEn,
            CostElement1Id = entity.CostElement1Id,
            CostElement2Id = entity.CostElement2Id,
            CostElement3Id = entity.CostElement3Id,
            CostElement4Id = entity.CostElement4Id,
            CostElement5Id = entity.CostElement5Id,
            CostElement6Id = entity.CostElement6Id,
            CostElement7Id = entity.CostElement7Id,
            CostPercentage = entity.CostPercentage,
            MergeOffsetAccount = entity.MergeOffsetAccount
        };
    }

    private static CreateOrEditEmployeeWageAccountDto ToCreateOrEditDto(EmployeeWageAccount entity)
    {
        return new CreateOrEditEmployeeWageAccountDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            WageTypeId = entity.WageTypeId,
            StartDate = entity.StartDate,
            AccountInfoId = entity.AccountInfoId,
            OffsetAccountInfoId = entity.OffsetAccountInfoId,
            CostElement1Id = entity.CostElement1Id,
            CostElement2Id = entity.CostElement2Id,
            CostElement3Id = entity.CostElement3Id,
            CostElement4Id = entity.CostElement4Id,
            CostElement5Id = entity.CostElement5Id,
            CostElement6Id = entity.CostElement6Id,
            CostElement7Id = entity.CostElement7Id,
            CostPercentage = entity.CostPercentage,
            MergeOffsetAccount = entity.MergeOffsetAccount
        };
    }
}
