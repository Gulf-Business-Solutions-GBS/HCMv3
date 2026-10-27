using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement.Review;

/// <summary>Recalculates a reviewer's and an appraisal's averages after a rating changes.</summary>
public interface IPAEmployeeScoreCalculator : IDomainService
{
    /// <summary>
    /// Legacy <c>PAEmployee.UpdateAverageRating</c> for one reviewer, then the appraisal's overall, peer and
    /// employee averages.
    /// </summary>
    Task UpdateAverageRatingAsync(int reviewerId);
}

/// <summary>
/// The scoring that <c>PAEmployeeRatingsAppService</c> has always done, moved here unchanged so the
/// self-service review screen scores a rating exactly as the admin grid does. "No new scoring formulas" was
/// an explicit condition of the appraisal review work: two copies of this would be two formulas the day one
/// of them was edited.
///
/// <para>
/// All three <see cref="ManageAppraisalSettings"/> modes, as before: marks off (rating codes averaged),
/// marks scored against ratings (weighted marks against the highest rating code), marks scored against KPI
/// weight (marks summed).
/// </para>
/// </summary>
public class PAEmployeeScoreCalculator : HCMv3DomainServiceBase, IPAEmployeeScoreCalculator
{
    private readonly IRepository<PAEmployeeRating> _ratingRepository;
    private readonly IRepository<PAEmployeeReviewer> _reviewerRepository;
    private readonly IRepository<PAEmployee> _paEmployeeRepository;
    private readonly IRepository<PARating> _paRatingRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public PAEmployeeScoreCalculator(
        IRepository<PAEmployeeRating> ratingRepository,
        IRepository<PAEmployeeReviewer> reviewerRepository,
        IRepository<PAEmployee> paEmployeeRepository,
        IRepository<PARating> paRatingRepository,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _ratingRepository = ratingRepository;
        _reviewerRepository = reviewerRepository;
        _paEmployeeRepository = paEmployeeRepository;
        _paRatingRepository = paRatingRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    public async Task UpdateAverageRatingAsync(int reviewerId)
    {
        var reviewer = await _reviewerRepository.FirstOrDefaultAsync(reviewerId);
        if (reviewer == null)
        {
            return;
        }

        var ratings = await _ratingRepository.GetAll()
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
        MarksOn marksOn)
    {
        var totalMarks = ratings.Sum(r => r.Marks);
        reviewer.TotalMarks = totalMarks;

        if (marksOn != MarksOn.Ratings)
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
        // The appraisal is rated WHILE it is in flight, which is exactly when ApprovedOnly hides it. Without
        // this the averages of every appraisal still under review would silently never be written --
        // legacy updated them on every rating save, whatever the request's status.
        PAEmployee paEmployee;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            paEmployee = await _paEmployeeRepository.FirstOrDefaultAsync(paEmployeeId);
        }

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
}
