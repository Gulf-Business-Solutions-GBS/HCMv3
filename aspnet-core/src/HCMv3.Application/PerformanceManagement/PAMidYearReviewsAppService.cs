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

[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms)]
public class PAMidYearReviewsAppService : HCMv3AppServiceBase, IPAMidYearReviewsAppService
{
    private readonly IRepository<PAMidYearReview> _repository;

    public PAMidYearReviewsAppService(IRepository<PAMidYearReview> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAMidYearReviewDto>> GetAll(GetAllPAMidYearReviewsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.KpiInfo)
            .WhereIf(input.PAFormId.HasValue, e => e.PAFormId == input.PAFormId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PAMidYearReviewDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAMidYearReviewForEditOutput> GetPAMidYearReviewForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAMidYearReviewForEditOutput { PAMidYearReview = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAMidYearReviewDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task CreateAsync(CreateOrEditPAMidYearReviewDto input)
    {
        var entity = MapToEntity(input, new PAMidYearReview());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAMidYearReviewDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static PAMidYearReview MapToEntity(CreateOrEditPAMidYearReviewDto input, PAMidYearReview entity)
    {
        entity.PAFormId = input.PAFormId;
        entity.KpiInfoId = input.KpiInfoId;
        entity.Weight = input.Weight;
        entity.Target = input.Target;
        entity.AppraiseeRatingId = input.AppraiseeRatingId;
        entity.RatingId = input.RatingId;
        entity.AppraiseeComments = input.AppraiseeComments;
        entity.AppraiserComments = input.AppraiserComments;

        return entity;
    }

    private async Task ValidateAsync(PAMidYearReview entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.PAFormId == entity.PAFormId && e.KpiInfoId == entity.KpiInfoId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("KpiInfoAlreadyExistsForHeader"));
        }
    }

    private static PAMidYearReviewDto ToDto(PAMidYearReview entity)
    {
        return new PAMidYearReviewDto
        {
            Id = entity.Id,
            PAFormId = entity.PAFormId,
            KpiInfoId = entity.KpiInfoId,
            KpiInfoName = entity.KpiInfo?.NameEn,
            Weight = entity.Weight,
            Target = entity.Target,
            AppraiseeRatingId = entity.AppraiseeRatingId,
            RatingId = entity.RatingId,
            AppraiseeComments = entity.AppraiseeComments,
            AppraiserComments = entity.AppraiserComments
        };
    }

    private static CreateOrEditPAMidYearReviewDto ToCreateOrEditDto(PAMidYearReview entity)
    {
        return new CreateOrEditPAMidYearReviewDto
        {
            Id = entity.Id,
            PAFormId = entity.PAFormId,
            KpiInfoId = entity.KpiInfoId,
            Weight = entity.Weight,
            Target = entity.Target,
            AppraiseeRatingId = entity.AppraiseeRatingId,
            RatingId = entity.RatingId,
            AppraiseeComments = entity.AppraiseeComments,
            AppraiserComments = entity.AppraiserComments
        };
    }
}
