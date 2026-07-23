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
/// Application service for the date-bound Employee-Manager assignment (ported from legacy XAF
/// EmployeeManager business object). Ports the legacy EndDate auto-chaining (<c>SetEndDate</c> --
/// recalculates the previous record's EndDate to (new StartDate - 1 day) when a new record starts) and
/// the no-duplicate-StartDate validation. The legacy <c>EmployeeAction</c> FK is out of scope (Wave B).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeManagers)]
public class EmployeeManagersAppService : HCMv3AppServiceBase, IEmployeeManagersAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<EmployeeManager> _employeeManagerRepository;

    public EmployeeManagersAppService(IRepository<EmployeeManager> employeeManagerRepository)
    {
        _employeeManagerRepository = employeeManagerRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeManagerDto>> GetAll(GetAllEmployeeManagersInput input)
    {
        input.Normalize();

        var query = _employeeManagerRepository.GetAll()
            .Include(m => m.Employee)
            .Include(m => m.Manager)
            .WhereIf(input.EmployeeId.HasValue, m => m.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeManagerDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeManagerForEditOutput> GetEmployeeManagerForEdit(EntityDto input)
    {
        var entity = await _employeeManagerRepository.GetAsync(input.Id);

        return new GetEmployeeManagerForEditOutput
        {
            EmployeeManager = new CreateOrEditEmployeeManagerDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                ManagerId = entity.ManagerId,
                StartDate = entity.StartDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeManagerDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeManagers_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeManagerDto input)
    {
        var entity = new EmployeeManager
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            ManagerId = input.ManagerId,
            StartDate = input.StartDate.Date,
            EndDate = InfiniteEndDate
        };

        await ValidateAsync(entity);
        await _employeeManagerRepository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.EmployeeId, entity.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeManagers_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeManagerDto input)
    {
        var entity = await _employeeManagerRepository.GetAsync(input.Id.Value);
        entity.EmployeeId = input.EmployeeId;
        entity.ManagerId = input.ManagerId;
        entity.StartDate = input.StartDate.Date;

        await ValidateAsync(entity);
        await _employeeManagerRepository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.EmployeeId, entity.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeManagers_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _employeeManagerRepository.GetAsync(input.Id);
        var employeeId = entity.EmployeeId;

        await _employeeManagerRepository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(employeeId, entity.Id);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no two records for the same employee may share a StartDate.
    /// </summary>
    private async Task ValidateAsync(EmployeeManager entity)
    {
        var duplicateStartDate = await _employeeManagerRepository.GetAll()
            .AnyAsync(m => m.Id != entity.Id
                           && m.EmployeeId == entity.EmployeeId
                           && m.StartDate == entity.StartDate);
        if (duplicateStartDate)
        {
            throw new UserFriendlyException(L("StartDateAlreadyExists", entity.StartDate.ToString("dd/MM/yyyy")));
        }
    }

    /// <summary>
    /// Legacy <c>SetEndDate</c> parity: for each remaining record of the employee (ordered by
    /// StartDate), EndDate = (next record's StartDate - 1 day), or infinite for the last one.
    /// </summary>
    private async Task SetEndDatesAsync(int employeeId, int excludeId)
    {
        var records = await _employeeManagerRepository.GetAll()
            .Where(m => m.EmployeeId == employeeId)
            .OrderBy(m => m.StartDate)
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

    private static EmployeeManagerDto ToDto(EmployeeManager entity)
    {
        return new EmployeeManagerDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            ManagerId = entity.ManagerId,
            ManagerCode = entity.Manager?.Code,
            ManagerFullNameEn = entity.Manager?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
