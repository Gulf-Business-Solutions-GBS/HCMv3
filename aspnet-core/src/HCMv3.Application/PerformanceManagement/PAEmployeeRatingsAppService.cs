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
    /// Legacy <c>PAEmployee.UpdateAverageRating</c>, all three branches of <c>ManageAppraisalSettings</c>.
    /// The calculation itself lives in <see cref="Review.PAEmployeeScoreCalculator"/> so the self-service
    /// review screen scores a rating exactly as this grid does.
    /// </summary>
    private Task UpdateAverageRatingAsync(int reviewerId) => ScoreCalculator.UpdateAverageRatingAsync(reviewerId);

    private Review.PAEmployeeScoreCalculator _scoreCalculator;

    /// <summary>
    /// Built on first use rather than in the constructor: it saves through this service's unit of work, and
    /// that is property-injected only after construction. Built here rather than injected so the
    /// constructor -- and everything that constructs this service -- stays as it was.
    /// </summary>
    private Review.PAEmployeeScoreCalculator ScoreCalculator =>
        _scoreCalculator ??= new Review.PAEmployeeScoreCalculator(
            _repository,
            _reviewerRepository,
            _paEmployeeRepository,
            _paRatingRepository,
            _sysSettingsAccessor)
        {
            UnitOfWorkManager = UnitOfWorkManager
        };

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
