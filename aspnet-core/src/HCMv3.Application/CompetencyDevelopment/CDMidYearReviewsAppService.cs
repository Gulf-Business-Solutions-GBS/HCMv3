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
/// Application service for the weighted competency mid-year review entries within a <see cref="CDForm"/>
/// (ported from legacy XAF CDMidYearReview business object). See <see cref="CDMidYearReview"/> class
/// remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDMidYearReviews)]
public class CDMidYearReviewsAppService : HCMv3AppServiceBase, ICDMidYearReviewsAppService
{
    private readonly IRepository<CDMidYearReview> _repository;

    public CDMidYearReviewsAppService(IRepository<CDMidYearReview> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CDMidYearReviewDto>> GetAll(GetAllCDMidYearReviewsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.Competency)
            .WhereIf(input.CDFormId.HasValue, r => r.CDFormId == input.CDFormId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CDMidYearReviewDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetCDMidYearReviewForEditOutput> GetCDMidYearReviewForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetCDMidYearReviewForEditOutput
        {
            CDMidYearReview = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCDMidYearReviewDto input)
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

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDMidYearReviews_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDMidYearReviews_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCDMidYearReviewDto input)
    {
        var entity = MapToEntity(input, new CDMidYearReview());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDMidYearReviews_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCDMidYearReviewDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static CDMidYearReview MapToEntity(CreateOrEditCDMidYearReviewDto input, CDMidYearReview entity)
    {
        entity.CDFormId = input.CDFormId;
        entity.CompetencyId = input.CompetencyId;
        entity.Weight = input.Weight;
        entity.Target = input.Target;
        entity.EmployeeComments = input.EmployeeComments;
        entity.ManagerComments = input.ManagerComments;

        return entity;
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: no duplicate Competency per parent CDForm.</summary>
    private async Task ValidateAsync(CDMidYearReview entity)
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

    private static CDMidYearReviewDto ToDto(CDMidYearReview entity)
    {
        return new CDMidYearReviewDto
        {
            Id = entity.Id,
            CDFormId = entity.CDFormId,
            CompetencyId = entity.CompetencyId,
            CompetencyName = entity.Competency?.NameEn,
            Weight = entity.Weight,
            Target = entity.Target,
            EmployeeComments = entity.EmployeeComments,
            ManagerComments = entity.ManagerComments
        };
    }

    private static CreateOrEditCDMidYearReviewDto ToCreateOrEditDto(CDMidYearReview entity)
    {
        return new CreateOrEditCDMidYearReviewDto
        {
            Id = entity.Id,
            CDFormId = entity.CDFormId,
            CompetencyId = entity.CompetencyId,
            Weight = entity.Weight,
            Target = entity.Target,
            EmployeeComments = entity.EmployeeComments,
            ManagerComments = entity.ManagerComments
        };
    }
}
