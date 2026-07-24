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
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Application service for <see cref="KpiByJobTitle"/>. Ports the legacy EndDate auto-chaining
/// (<c>SetEndDate</c>) and no-duplicate-StartDate validation, same pattern as
/// <c>PersonnelAdministration.EmployeeOrgAssignmentsAppService</c>. The legacy computed <c>TotalWeight</c>
/// getter is exposed as a DTO-only aggregate (see <see cref="KpiByJobTitleDto"/>).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles)]
public class KpiByJobTitlesAppService : HCMv3AppServiceBase, IKpiByJobTitlesAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<KpiByJobTitle> _repository;
    private readonly IRepository<KpiByJobTitleDetail> _detailRepository;

    public KpiByJobTitlesAppService(IRepository<KpiByJobTitle> repository, IRepository<KpiByJobTitleDetail> detailRepository)
    {
        _repository = repository;
        _detailRepository = detailRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<KpiByJobTitleDto>> GetAll(GetAllKpiByJobTitlesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.JobTitle)
            .WhereIf(input.JobTitleId.HasValue, e => e.JobTitleId == input.JobTitleId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var dtos = new System.Collections.Generic.List<KpiByJobTitleDto>();
        foreach (var item in items)
        {
            dtos.Add(await ToDtoAsync(item));
        }

        return new PagedResultDto<KpiByJobTitleDto>(totalCount, dtos);
    }

    public async Task<GetKpiByJobTitleForEditOutput> GetKpiByJobTitleForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetKpiByJobTitleForEditOutput
        {
            KpiByJobTitle = new CreateOrEditKpiByJobTitleDto
            {
                Id = entity.Id,
                JobTitleId = entity.JobTitleId,
                StartDate = entity.StartDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditKpiByJobTitleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var jobTitleId = entity.JobTitleId;

        await _repository.DeleteAsync(entity);

        await SetEndDatesAsync(jobTitleId);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles_Create)]
    protected virtual async Task CreateAsync(CreateOrEditKpiByJobTitleDto input)
    {
        var entity = new KpiByJobTitle
        {
            TenantId = AbpSession.TenantId ?? 0,
            JobTitleId = input.JobTitleId,
            StartDate = input.StartDate.Date,
            EndDate = InfiniteEndDate
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.JobTitleId);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_KpiByJobTitles_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditKpiByJobTitleDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.JobTitleId = input.JobTitleId;
        entity.StartDate = input.StartDate.Date;

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await SetEndDatesAsync(entity.JobTitleId);
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: no two records for the same JobTitle may share a StartDate.</summary>
    private async Task ValidateAsync(KpiByJobTitle entity)
    {
        var duplicateStartDate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.JobTitleId == entity.JobTitleId && e.StartDate == entity.StartDate);
        if (duplicateStartDate)
        {
            throw new UserFriendlyException(L("StartDateAlreadyExists", entity.StartDate.ToString("dd/MM/yyyy")));
        }
    }

    /// <summary>Legacy <c>SetEndDate</c> parity.</summary>
    private async Task SetEndDatesAsync(int jobTitleId)
    {
        var records = await _repository.GetAll()
            .Where(e => e.JobTitleId == jobTitleId)
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

    private async Task<KpiByJobTitleDto> ToDtoAsync(KpiByJobTitle entity)
    {
        var totalWeight = await _detailRepository.GetAll()
            .Where(d => d.KpiByJobTitleId == entity.Id)
            .SumAsync(d => (double?)d.Weight) ?? 0;

        return new KpiByJobTitleDto
        {
            Id = entity.Id,
            JobTitleId = entity.JobTitleId,
            JobTitleName = entity.JobTitle?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TotalWeight = totalWeight
        };
    }
}
