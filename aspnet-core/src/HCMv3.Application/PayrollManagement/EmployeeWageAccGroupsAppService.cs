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
/// Application service for <see cref="EmployeeWageAccGroup"/> (ported from legacy XAF
/// EmployeeWageAccGroup business object). Ports the legacy EndDate auto-chaining (<c>SetEndDate</c>),
/// mirroring <c>OrgUnitWageAccGroupsAppService</c>'s pattern exactly, scoped by EmployeeId.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeWageAccGroups)]
public class EmployeeWageAccGroupsAppService : HCMv3AppServiceBase, IEmployeeWageAccGroupsAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<EmployeeWageAccGroup> _repository;

    public EmployeeWageAccGroupsAppService(IRepository<EmployeeWageAccGroup> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeWageAccGroupDto>> GetAll(GetAllEmployeeWageAccGroupsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(g => g.Employee)
            .Include(g => g.WageAccGroup)
            .WhereIf(input.EmployeeId.HasValue, g => g.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeWageAccGroupDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeWageAccGroupForEditOutput> GetEmployeeWageAccGroupForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeWageAccGroupForEditOutput
        {
            EmployeeWageAccGroup = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeWageAccGroupDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeWageAccGroups_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeWageAccGroupDto input)
    {
        var entity = new EmployeeWageAccGroup
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            StartDate = input.StartDate.Date,
            EndDate = InfiniteEndDate,
            WageAccGroupId = input.WageAccGroupId
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.EmployeeId);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeWageAccGroups_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeWageAccGroupDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.WageAccGroupId = input.WageAccGroupId;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.EmployeeId);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_EmployeeWageAccGroups_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var employeeId = entity.EmployeeId;

        await _repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(employeeId);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: required fields, no duplicate (EmployeeId, WageAccGroupId,
    /// StartDate) triple.
    /// </summary>
    private async Task ValidateAsync(EmployeeWageAccGroup entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(g => g.Id != entity.Id &&
                           g.EmployeeId == entity.EmployeeId &&
                           g.WageAccGroupId == entity.WageAccGroupId &&
                           g.StartDate == entity.StartDate);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    /// <summary>
    /// Legacy <c>SetEndDate</c> parity: for each remaining record of the employee (ordered by StartDate),
    /// EndDate = (next record's StartDate - 1 day), or infinite for the last one.
    /// </summary>
    private async Task SetEndDatesAsync(int employeeId)
    {
        var records = await _repository.GetAll()
            .Where(g => g.EmployeeId == employeeId)
            .OrderBy(g => g.StartDate)
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

    private static EmployeeWageAccGroupDto ToDto(EmployeeWageAccGroup entity)
    {
        return new EmployeeWageAccGroupDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            WageAccGroupId = entity.WageAccGroupId,
            WageAccGroupName = entity.WageAccGroup?.NameEn
        };
    }

    private static CreateOrEditEmployeeWageAccGroupDto ToCreateOrEditDto(EmployeeWageAccGroup entity)
    {
        return new CreateOrEditEmployeeWageAccGroupDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            StartDate = entity.StartDate,
            WageAccGroupId = entity.WageAccGroupId
        };
    }
}
