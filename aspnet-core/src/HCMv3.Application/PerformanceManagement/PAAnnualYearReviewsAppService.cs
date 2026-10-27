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
/// Application service for <see cref="PAAnnualYearReview"/>. Ports the legacy <c>UpdateOverallScore</c>
/// (recomputes <see cref="PAForm.OverallScore"/> as sum of (Rating.Code * Weight / 100) across all annual
/// review lines) on every Create/Update/Delete.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms)]
public class PAAnnualYearReviewsAppService : HCMv3AppServiceBase, IPAAnnualYearReviewsAppService
{
    private readonly IRepository<PAAnnualYearReview> _repository;
    private readonly IRepository<PAForm> _paFormRepository;
    private readonly IRepository<PARating> _paRatingRepository;

    public PAAnnualYearReviewsAppService(
        IRepository<PAAnnualYearReview> repository,
        IRepository<PAForm> paFormRepository,
        IRepository<PARating> paRatingRepository)
    {
        _repository = repository;
        _paFormRepository = paFormRepository;
        _paRatingRepository = paRatingRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAAnnualYearReviewDto>> GetAll(GetAllPAAnnualYearReviewsInput input)
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

        return new PagedResultDto<PAAnnualYearReviewDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAAnnualYearReviewForEditOutput> GetPAAnnualYearReviewForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAAnnualYearReviewForEditOutput { PAAnnualYearReview = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAAnnualYearReviewDto input)
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
        var entity = await _repository.GetAsync(input.Id);
        var paFormId = entity.PAFormId;

        await _repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await UpdateOverallScoreAsync(paFormId);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task CreateAsync(CreateOrEditPAAnnualYearReviewDto input)
    {
        var entity = MapToEntity(input, new PAAnnualYearReview());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await UpdateOverallScoreAsync(entity.PAFormId);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAAnnualYearReviewDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await UpdateOverallScoreAsync(entity.PAFormId);
    }

    private static PAAnnualYearReview MapToEntity(CreateOrEditPAAnnualYearReviewDto input, PAAnnualYearReview entity)
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

    private async Task ValidateAsync(PAAnnualYearReview entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.PAFormId == entity.PAFormId && e.KpiInfoId == entity.KpiInfoId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("KpiInfoAlreadyExistsForHeader"));
        }
    }

    /// <summary>Legacy <c>UpdateOverallScore</c> parity: OverallScore = sum(Rating.Code * Weight / 100).</summary>
    private async Task UpdateOverallScoreAsync(int paFormId)
    {
        var lines = await _repository.GetAll()
            .Where(e => e.PAFormId == paFormId && e.RatingId != null)
            .ToListAsync();

        double overallScore = 0;
        if (lines.Count > 0)
        {
            var ratingIds = lines.Select(l => l.RatingId.Value).Distinct().ToList();
            var ratings = await _paRatingRepository.GetAll()
                .Where(r => ratingIds.Contains(r.Id))
                .ToListAsync();

            foreach (var line in lines)
            {
                var rating = ratings.FirstOrDefault(r => r.Id == line.RatingId);
                if (rating != null && int.TryParse(rating.Code, out var ratingCode))
                {
                    overallScore += ratingCode * line.Weight / 100;
                }
            }
        }

        var paForm = await _paFormRepository.FirstOrDefaultAsync(paFormId);
        if (paForm != null)
        {
            paForm.OverallScore = Math.Round(overallScore, 2);
            await _paFormRepository.UpdateAsync(paForm);
            await CurrentUnitOfWork.SaveChangesAsync();
        }
    }

    private static PAAnnualYearReviewDto ToDto(PAAnnualYearReview entity)
    {
        return new PAAnnualYearReviewDto
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

    private static CreateOrEditPAAnnualYearReviewDto ToCreateOrEditDto(PAAnnualYearReview entity)
    {
        return new CreateOrEditPAAnnualYearReviewDto
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
