using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Application service for <see cref="KpiByOrgUnit"/>. Ports the legacy EndDate auto-chaining
/// (<c>SetEndDate</c>) and no-duplicate-StartDate validation, same pattern as
/// <see cref="KpiByJobTitlesAppService"/>/<c>PersonnelAdministration.EmployeeOrgAssignmentsAppService</c>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits)]
public class KpiByOrgUnitsAppService : HCMv3AppServiceBase, IKpiByOrgUnitsAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<KpiByOrgUnit> _repository;
    private readonly IRepository<KpiByOrgUnitDetail> _detailRepository;

    public KpiByOrgUnitsAppService(IRepository<KpiByOrgUnit> repository, IRepository<KpiByOrgUnitDetail> detailRepository)
    {
        _repository = repository;
        _detailRepository = detailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<KpiByOrgUnitDto>> GetAll(GetAllKpiByOrgUnitsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.OrganizationUnit)
            .WhereIf(input.OrganizationUnitId.HasValue, e => e.OrganizationUnitId == input.OrganizationUnitId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var dtos = new List<KpiByOrgUnitDto>();
        foreach (var item in items)
        {
            dtos.Add(await ToDtoAsync(item));
        }

        return new PagedResultDto<KpiByOrgUnitDto>(totalCount, dtos);
    }

    public async Task<GetKpiByOrgUnitForEditOutput> GetKpiByOrgUnitForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetKpiByOrgUnitForEditOutput
        {
            KpiByOrgUnit = new CreateOrEditKpiByOrgUnitDto
            {
                Id = entity.Id,
                OrganizationUnitId = entity.OrganizationUnitId,
                StartDate = entity.StartDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditKpiByOrgUnitDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var organizationUnitId = entity.OrganizationUnitId;

        await _repository.DeleteAsync(entity);

        await SetEndDatesAsync(organizationUnitId);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits_Create)]
    protected virtual async Task CreateAsync(CreateOrEditKpiByOrgUnitDto input)
    {
        var entity = new KpiByOrgUnit
        {
            TenantId = AbpSession.TenantId ?? 0,
            OrganizationUnitId = input.OrganizationUnitId,
            StartDate = input.StartDate.Date,
            EndDate = InfiniteEndDate
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.OrganizationUnitId);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByOrgUnits_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditKpiByOrgUnitDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.StartDate = input.StartDate.Date;

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.OrganizationUnitId);
    }

    private async Task ValidateAsync(KpiByOrgUnit entity)
    {
        var duplicateStartDate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.OrganizationUnitId == entity.OrganizationUnitId && e.StartDate == entity.StartDate);
        if (duplicateStartDate)
        {
            throw new UserFriendlyException(L("StartDateAlreadyExists", entity.StartDate.ToString("dd/MM/yyyy")));
        }
    }

    private async Task SetEndDatesAsync(long organizationUnitId)
    {
        var records = await _repository.GetAll()
            .Where(e => e.OrganizationUnitId == organizationUnitId)
            .OrderBy(e => e.StartDate)
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

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    private async Task<KpiByOrgUnitDto> ToDtoAsync(KpiByOrgUnit entity)
    {
        var totalWeight = await _detailRepository.GetAll()
            .Where(d => d.KpiByOrgUnitId == entity.Id)
            .SumAsync(d => (double?)d.Weight) ?? 0;

        return new KpiByOrgUnitDto
        {
            Id = entity.Id,
            OrganizationUnitId = entity.OrganizationUnitId,
            OrganizationUnitName = entity.OrganizationUnit?.DisplayName,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TotalWeight = totalWeight
        };
    }
}
