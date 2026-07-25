using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.SuccessionManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SuccessionManagement;

/// <summary>
/// Application service for <see cref="EmpPerformanceHistory"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SuccessionManagement.EmpPerformanceHistory</c>). Ports the essential
/// active validation: required fields, EndDate &gt;= StartDate, and no overlapping StartDate/EndDate window
/// for the same Employee.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmpPerformanceHistories)]
public class EmpPerformanceHistoriesAppService : HCMv3AppServiceBase, IEmpPerformanceHistoriesAppService
{
    private readonly IRepository<EmpPerformanceHistory> _repository;

    public EmpPerformanceHistoriesAppService(IRepository<EmpPerformanceHistory> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmpPerformanceHistoryDto>> GetAll(GetAllEmpPerformanceHistoriesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.Rating)
            .Include(e => e.NineBoxRating)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmpPerformanceHistoryDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmpPerformanceHistoryForEditOutput> GetEmpPerformanceHistoryForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmpPerformanceHistoryForEditOutput
        {
            EmpPerformanceHistory = new CreateOrEditEmpPerformanceHistoryDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Description = entity.Description,
                Score = entity.Score,
                RatingId = entity.RatingId,
                Source = entity.Source,
                NineBoxRatingId = entity.NineBoxRatingId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmpPerformanceHistoryDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmpPerformanceHistories_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmpPerformanceHistoryDto input)
    {
        var entity = MapToEntity(input, new EmpPerformanceHistory());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmpPerformanceHistories_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmpPerformanceHistoryDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmpPerformanceHistories_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateEntityAsync(EmpPerformanceHistory entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var hasOverlap = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.EmployeeId == entity.EmployeeId
                && e.StartDate.Date <= entity.EndDate.Date && e.EndDate.Date >= entity.StartDate.Date);
        if (hasOverlap)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static EmpPerformanceHistory MapToEntity(CreateOrEditEmpPerformanceHistoryDto input, EmpPerformanceHistory entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.Description = input.Description;
        entity.Score = input.Score;
        entity.RatingId = input.RatingId;
        entity.Source = input.Source;
        entity.NineBoxRatingId = input.NineBoxRatingId;
        return entity;
    }

    private static EmpPerformanceHistoryDto ToDto(EmpPerformanceHistory entity)
    {
        return new EmpPerformanceHistoryDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Description = entity.Description,
            Score = entity.Score,
            RatingId = entity.RatingId,
            RatingNameEn = entity.Rating?.NameEn,
            Source = entity.Source,
            NineBoxRatingId = entity.NineBoxRatingId,
            NineBoxRatingNameEn = entity.NineBoxRating?.NameEn
        };
    }
}
