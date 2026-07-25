using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.CompetencyDevelopment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// Application service for the weighted competency annual-year review entries within a <see cref="CDForm"/>
/// (ported from legacy XAF CDAnnualYearReview business object). See <see cref="CDAnnualYearReview"/> class
/// remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDAnnualYearReviews)]
public class CDAnnualYearReviewsAppService : HCMv3AppServiceBase, ICDAnnualYearReviewsAppService
{
    private readonly IRepository<CDAnnualYearReview> _repository;

    public CDAnnualYearReviewsAppService(IRepository<CDAnnualYearReview> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CDAnnualYearReviewDto>> GetAll(GetAllCDAnnualYearReviewsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.Competency)
            .Include(r => r.CDRating)
            .WhereIf(input.CDFormId.HasValue, r => r.CDFormId == input.CDFormId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CDAnnualYearReviewDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetCDAnnualYearReviewForEditOutput> GetCDAnnualYearReviewForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetCDAnnualYearReviewForEditOutput
        {
            CDAnnualYearReview = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCDAnnualYearReviewDto input)
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

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDAnnualYearReviews_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDAnnualYearReviews_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCDAnnualYearReviewDto input)
    {
        var entity = MapToEntity(input, new CDAnnualYearReview());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDAnnualYearReviews_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCDAnnualYearReviewDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static CDAnnualYearReview MapToEntity(CreateOrEditCDAnnualYearReviewDto input, CDAnnualYearReview entity)
    {
        entity.CDFormId = input.CDFormId;
        entity.CompetencyId = input.CompetencyId;
        entity.Weight = input.Weight;
        entity.Target = input.Target;
        entity.EmployeeComments = input.EmployeeComments;
        entity.ManagerComments = input.ManagerComments;
        entity.CDRatingId = input.CDRatingId;

        return entity;
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: no duplicate Competency per parent CDForm.</summary>
    private async Task ValidateAsync(CDAnnualYearReview entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id &&
                           r.CDFormId == entity.CDFormId &&
                           r.CompetencyId == entity.CompetencyId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("CompetencyAlreadyAssigned"));
        }
    }

    private static CDAnnualYearReviewDto ToDto(CDAnnualYearReview entity)
    {
        return new CDAnnualYearReviewDto
        {
            Id = entity.Id,
            CDFormId = entity.CDFormId,
            CompetencyId = entity.CompetencyId,
            CompetencyName = entity.Competency?.NameEn,
            Weight = entity.Weight,
            Target = entity.Target,
            EmployeeComments = entity.EmployeeComments,
            ManagerComments = entity.ManagerComments,
            CDRatingId = entity.CDRatingId,
            CDRatingName = entity.CDRating?.NameEn
        };
    }

    private static CreateOrEditCDAnnualYearReviewDto ToCreateOrEditDto(CDAnnualYearReview entity)
    {
        return new CreateOrEditCDAnnualYearReviewDto
        {
            Id = entity.Id,
            CDFormId = entity.CDFormId,
            CompetencyId = entity.CompetencyId,
            Weight = entity.Weight,
            Target = entity.Target,
            EmployeeComments = entity.EmployeeComments,
            ManagerComments = entity.ManagerComments,
            CDRatingId = entity.CDRatingId
        };
    }
}
