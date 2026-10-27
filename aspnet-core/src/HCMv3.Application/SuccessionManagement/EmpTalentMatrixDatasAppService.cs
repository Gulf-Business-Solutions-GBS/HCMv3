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
/// Application service for <see cref="EmpTalentMatrixData"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SuccessionManagement.EmpTalentMatrixData</c>). Ports the essential active
/// validation: required fields, EndDate &gt;= StartDate, and no overlapping StartDate/EndDate window for the
/// same Employee.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmpTalentMatrixDatas)]
public class EmpTalentMatrixDatasAppService : HCMv3AppServiceBase, IEmpTalentMatrixDatasAppService
{
    private readonly IRepository<EmpTalentMatrixData> _repository;

    public EmpTalentMatrixDatasAppService(IRepository<EmpTalentMatrixData> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmpTalentMatrixDataDto>> GetAll(GetAllEmpTalentMatrixDatasInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.PerformanceRating)
            .Include(e => e.PotentialRating)
            .Include(e => e.OverallRating)
            .Include(e => e.CalibratedRating)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmpTalentMatrixDataDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmpTalentMatrixDataForEditOutput> GetEmpTalentMatrixDataForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmpTalentMatrixDataForEditOutput
        {
            EmpTalentMatrixData = new CreateOrEditEmpTalentMatrixDataDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                PerformanceRatingId = entity.PerformanceRatingId,
                PotentialRatingId = entity.PotentialRatingId,
                OverallRatingId = entity.OverallRatingId,
                CalibratedRatingId = entity.CalibratedRatingId
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmpTalentMatrixDataDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmpTalentMatrixDatas_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmpTalentMatrixDataDto input)
    {
        var entity = MapToEntity(input, new EmpTalentMatrixData());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmpTalentMatrixDatas_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmpTalentMatrixDataDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_EmpTalentMatrixDatas_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateEntityAsync(EmpTalentMatrixData entity)
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

    private static EmpTalentMatrixData MapToEntity(CreateOrEditEmpTalentMatrixDataDto input, EmpTalentMatrixData entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.PerformanceRatingId = input.PerformanceRatingId;
        entity.PotentialRatingId = input.PotentialRatingId;
        entity.OverallRatingId = input.OverallRatingId;
        entity.CalibratedRatingId = input.CalibratedRatingId;
        return entity;
    }

    private static EmpTalentMatrixDataDto ToDto(EmpTalentMatrixData entity)
    {
        return new EmpTalentMatrixDataDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            PerformanceRatingId = entity.PerformanceRatingId,
            PerformanceRatingNameEn = entity.PerformanceRating?.NameEn,
            PotentialRatingId = entity.PotentialRatingId,
            PotentialRatingNameEn = entity.PotentialRating?.NameEn,
            OverallRatingId = entity.OverallRatingId,
            OverallRatingNameEn = entity.OverallRating?.NameEn,
            CalibratedRatingId = entity.CalibratedRatingId,
            CalibratedRatingNameEn = entity.CalibratedRating?.NameEn
        };
    }
}
