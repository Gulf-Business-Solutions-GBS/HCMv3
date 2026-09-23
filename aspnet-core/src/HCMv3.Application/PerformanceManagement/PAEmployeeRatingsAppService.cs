using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Application service for <see cref="PAEmployeeRating"/>. Ports the legacy <c>UpdateAverageRating</c>
/// (reviewer AverageScore/AverageRating/TotalMarks, and the parent <see cref="PAEmployee"/>'s Overall/Peer/Employee
/// average score+rating) on every Create/Update/Delete, in all three ManageAppraisalSettings modes: ratings,
/// marks against ratings, and marks against KPI weight.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees)]
public class PAEmployeeRatingsAppService : HCMv3AppServiceBase, IPAEmployeeRatingsAppService
{
    private readonly IRepository<PAEmployeeRating> _repository;
    private readonly IRepository<PAEmployeeReviewer> _reviewerRepository;
    private readonly IRepository<PAEmployee> _paEmployeeRepository;
    private readonly IRepository<PARating> _paRatingRepository;

    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;

    public PAEmployeeRatingsAppService(
        IRepository<PAEmployeeRating> repository,
        IRepository<PAEmployeeReviewer> reviewerRepository,
        IRepository<PAEmployee> paEmployeeRepository,
        IRepository<PARating> paRatingRepository,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor)
    {
        _sysSettingsAccessor = sysSettingsAccessor;
        _repository = repository;
        _reviewerRepository = reviewerRepository;
        _paEmployeeRepository = paEmployeeRepository;
        _paRatingRepository = paRatingRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAEmployeeRatingDto>> GetAll(GetAllPAEmployeeRatingsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.PAEmployeeReviewer).ThenInclude(r => r.Employee)
            .Include(e => e.PAEmployeeDetail).ThenInclude(d => d.KpiInfo)
            .WhereIf(input.PAEmployeeId.HasValue, e => e.PAEmployeeId == input.PAEmployeeId)
            .WhereIf(input.PAEmployeeReviewerId.HasValue, e => e.PAEmployeeReviewerId == input.PAEmployeeReviewerId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PAEmployeeRatingDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAEmployeeRatingForEditOutput> GetPAEmployeeRatingForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAEmployeeRatingForEditOutput { PAEmployeeRating = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAEmployeeRatingDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var reviewerId = entity.PAEmployeeReviewerId;

        await _repository.DeleteAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await UpdateAverageRatingAsync(reviewerId);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees_Edit)]
    protected virtual async Task CreateAsync(CreateOrEditPAEmployeeRatingDto input)
    {
        var entity = MapToEntity(input, new PAEmployeeRating());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await UpdateAverageRatingAsync(entity.PAEmployeeReviewerId);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAEmployeeRatingDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await UpdateAverageRatingAsync(entity.PAEmployeeReviewerId);
    }

    private static PAEmployeeRating MapToEntity(CreateOrEditPAEmployeeRatingDto input, PAEmployeeRating entity)
    {
        entity.PAEmployeeId = input.PAEmployeeId;
        entity.PAEmployeeReviewerId = input.PAEmployeeReviewerId;
        entity.PAEmployeeDetailId = input.PAEmployeeDetailId;
        entity.Marks = input.Marks;
        entity.RatingId = input.RatingId;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PAEmployee.UpdateAverageRating</c>, all three branches of <c>ManageAppraisalSettings</c>:
    /// marks off (rating codes averaged), marks on scored against ratings (weighted marks against the highest
    /// rating code), and marks on scored against KPI weight (marks summed).
    /// </summary>
    private async Task UpdateAverageRatingAsync(int reviewerId)
    {
        var reviewer = await _reviewerRepository.FirstOrDefaultAsync(reviewerId);
        if (reviewer == null)
        {
            return;
        }

        var ratings = await _repository.GetAll()
            .Include(r => r.PAEmployeeDetail)
            .Where(r => r.PAEmployeeReviewerId == reviewerId)
            .ToListAsync();

        var appraisalSettings = await _sysSettingsAccessor.GetAppraisalSettingsAsync();
        if (appraisalSettings != null && appraisalSettings.EnableMarksInPAReview)
        {
            await UpdateMarksBasedAveragesAsync(reviewer, ratings, appraisalSettings.MarksOn);
            await _reviewerRepository.UpdateAsync(reviewer);
            await CurrentUnitOfWork.SaveChangesAsync();
            await UpdatePAEmployeeAveragesAsync(reviewer.PAEmployeeId);
            return;
        }

        var ratedLines = ratings.Where(r => r.RatingId != null).ToList();
        if (ratedLines.Count > 0)
        {
            var ratingIds = ratedLines.Select(r => r.RatingId.Value).Distinct().ToList();
            var paRatings = await _paRatingRepository.GetAll().Where(r => ratingIds.Contains(r.Id)).ToListAsync();

            double totalRatingCodes = 0;
            foreach (var line in ratedLines)
            {
                var rating = paRatings.FirstOrDefault(r => r.Id == line.RatingId);
                if (rating != null && int.TryParse(rating.Code, out var code))
                {
                    totalRatingCodes += code;
                }
            }

            var averageRating = Math.Round(totalRatingCodes / ratedLines.Count);
            reviewer.AverageScore = Math.Round(averageRating, 2);
            reviewer.AverageRating = paRatings
                .Where(r => int.TryParse(r.Code, out var c) && c <= averageRating)
                .OrderByDescending(r => int.Parse(r.Code))
                .FirstOrDefault();
            reviewer.AverageRatingId = reviewer.AverageRating?.Id;
        }
        else
        {
            reviewer.AverageScore = 0;
            reviewer.AverageRatingId = null;
        }

        await _reviewerRepository.UpdateAsync(reviewer);
        await CurrentUnitOfWork.SaveChangesAsync();

        await UpdatePAEmployeeAveragesAsync(reviewer.PAEmployeeId);
    }

    /// <summary>
    /// Legacy "Marks enabled" branches. Against ratings: each line's marks are weighted by its KPI weight over
    /// the highest rating code, and the reviewer's score is that total scaled back to the rating range;
    /// against weight: the reviewer's total marks are simply summed. Legacy set no average rating in either
    /// branch (that assignment was commented out), so it is left untouched here too.
    /// </summary>
    private async Task UpdateMarksBasedAveragesAsync(
        PAEmployeeReviewer reviewer,
        List<PAEmployeeRating> ratings,
        HCMv3.SystemSettings.MarksOn marksOn)
    {
        var totalMarks = ratings.Sum(r => r.Marks);
        reviewer.TotalMarks = totalMarks;

        if (marksOn != HCMv3.SystemSettings.MarksOn.Ratings)
        {
            return;
        }

        if (ratings.Count == 0)
        {
            reviewer.AverageScore = 0;
            reviewer.AverageRatingId = null;
            return;
        }

        var maxRating = (await _paRatingRepository.GetAll().Select(r => r.Code).ToListAsync())
            .Select(code => int.TryParse(code, out var c) ? c : 0)
            .DefaultIfEmpty(0)
            .Max();

        double calculatedScore = 0;
        if (maxRating != 0)
        {
            calculatedScore = ratings.Sum(r => ((r.PAEmployeeDetail?.Weight ?? 0) / maxRating) * r.Marks);
        }

        reviewer.AverageScore = Math.Round((calculatedScore / 100) * maxRating, 2);
    }

    private async Task UpdatePAEmployeeAveragesAsync(int paEmployeeId)
    {
        var paEmployee = await _paEmployeeRepository.FirstOrDefaultAsync(paEmployeeId);
        if (paEmployee == null)
        {
            return;
        }

        var reviewers = await _reviewerRepository.GetAll()
            .Where(r => r.PAEmployeeId == paEmployeeId)
            .ToListAsync();

        if (reviewers.Count > 0)
        {
            paEmployee.OverallAverageScore = Math.Round(reviewers.Average(r => r.AverageScore), 2);
        }
        else
        {
            paEmployee.OverallAverageScore = 0;
        }

        var peerReviewers = reviewers.Where(r => r.EmployeeId != paEmployee.EmployeeId).ToList();
        paEmployee.PeerAverageScore = peerReviewers.Count > 0 ? Math.Round(peerReviewers.Average(r => r.AverageScore), 2) : 0;

        var employeeReviewer = reviewers.FirstOrDefault(r => r.EmployeeId == paEmployee.EmployeeId);
        paEmployee.EmployeeAverageScore = employeeReviewer?.AverageScore ?? 0;
        paEmployee.EmployeeAverageRatingId = employeeReviewer?.AverageRatingId;

        await _paEmployeeRepository.UpdateAsync(paEmployee);
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    private static PAEmployeeRatingDto ToDto(PAEmployeeRating entity)
    {
        return new PAEmployeeRatingDto
        {
            Id = entity.Id,
            PAEmployeeId = entity.PAEmployeeId,
            PAEmployeeReviewerId = entity.PAEmployeeReviewerId,
            ReviewerEmployeeId = entity.PAEmployeeReviewer?.EmployeeId ?? 0,
            ReviewerEmployeeFullNameEn = entity.PAEmployeeReviewer?.Employee?.FullNameEn,
            PAEmployeeDetailId = entity.PAEmployeeDetailId,
            KpiInfoId = entity.PAEmployeeDetail?.KpiInfoId ?? 0,
            KpiInfoName = entity.PAEmployeeDetail?.KpiInfo?.NameEn,
            Weight = entity.PAEmployeeDetail?.Weight ?? 0,
            Target = entity.PAEmployeeDetail?.Target,
            Marks = entity.Marks,
            RatingId = entity.RatingId,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditPAEmployeeRatingDto ToCreateOrEditDto(PAEmployeeRating entity)
    {
        return new CreateOrEditPAEmployeeRatingDto
        {
            Id = entity.Id,
            PAEmployeeId = entity.PAEmployeeId,
            PAEmployeeReviewerId = entity.PAEmployeeReviewerId,
            PAEmployeeDetailId = entity.PAEmployeeDetailId,
            Marks = entity.Marks,
            RatingId = entity.RatingId,
            Comments = entity.Comments
        };
    }
}
