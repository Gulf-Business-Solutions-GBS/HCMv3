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
/// Application service for <see cref="PAEmployeeDetail"/>. Ports the legacy <c>FillReview</c>: whenever a
/// new KPI line is added, a <see cref="PAEmployeeRating"/> row is auto-created for every existing
/// <see cref="PAEmployeeReviewer"/> of the parent <see cref="PAEmployee"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees)]
public class PAEmployeeDetailsAppService : HCMv3AppServiceBase, IPAEmployeeDetailsAppService
{
    private readonly IRepository<PAEmployeeDetail> _repository;
    private readonly IRepository<PAEmployeeReviewer> _reviewerRepository;
    private readonly IRepository<PAEmployeeRating> _ratingRepository;

    public PAEmployeeDetailsAppService(
        IRepository<PAEmployeeDetail> repository,
        IRepository<PAEmployeeReviewer> reviewerRepository,
        IRepository<PAEmployeeRating> ratingRepository)
    {
        _repository = repository;
        _reviewerRepository = reviewerRepository;
        _ratingRepository = ratingRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAEmployeeDetailDto>> GetAll(GetAllPAEmployeeDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.KpiInfo)
            .WhereIf(input.PAEmployeeId.HasValue, e => e.PAEmployeeId == input.PAEmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PAEmployeeDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAEmployeeDetailForEditOutput> GetPAEmployeeDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAEmployeeDetailForEditOutput { PAEmployeeDetail = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAEmployeeDetailDto input)
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

        var relatedRatings = await _ratingRepository.GetAll()
            .Where(r => r.PAEmployeeDetailId == entity.Id)
            .ToListAsync();
        foreach (var rating in relatedRatings)
        {
            await _ratingRepository.DeleteAsync(rating);
        }

        await _repository.DeleteAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees_Edit)]
    protected virtual async Task CreateAsync(CreateOrEditPAEmployeeDetailDto input)
    {
        var entity = MapToEntity(input, new PAEmployeeDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await FillReviewAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAEmployeeDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static PAEmployeeDetail MapToEntity(CreateOrEditPAEmployeeDetailDto input, PAEmployeeDetail entity)
    {
        entity.PAEmployeeId = input.PAEmployeeId;
        entity.KpiInfoId = input.KpiInfoId;
        entity.Weight = input.Weight;
        entity.Target = input.Target;

        return entity;
    }

    private async Task ValidateAsync(PAEmployeeDetail entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.PAEmployeeId == entity.PAEmployeeId && e.KpiInfoId == entity.KpiInfoId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("KpiInfoAlreadyExistsForHeader"));
        }
    }

    /// <summary>Legacy <c>FillReview</c> parity.</summary>
    private async Task FillReviewAsync(PAEmployeeDetail detail)
    {
        var reviewers = await _reviewerRepository.GetAll()
            .Where(r => r.PAEmployeeId == detail.PAEmployeeId)
            .ToListAsync();

        foreach (var reviewer in reviewers)
        {
            var exists = await _ratingRepository.GetAll()
                .AnyAsync(r => r.PAEmployeeReviewerId == reviewer.Id && r.PAEmployeeDetailId == detail.Id);
            if (!exists)
            {
                await _ratingRepository.InsertAsync(new PAEmployeeRating
                {
                    TenantId = AbpSession.TenantId ?? 0,
                    PAEmployeeId = detail.PAEmployeeId,
                    PAEmployeeReviewerId = reviewer.Id,
                    PAEmployeeDetailId = detail.Id
                });
            }
        }

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    private static PAEmployeeDetailDto ToDto(PAEmployeeDetail entity)
    {
        return new PAEmployeeDetailDto
        {
            Id = entity.Id,
            PAEmployeeId = entity.PAEmployeeId,
            KpiInfoId = entity.KpiInfoId,
            KpiInfoName = entity.KpiInfo?.NameEn,
            Weight = entity.Weight,
            Target = entity.Target
        };
    }

    private static CreateOrEditPAEmployeeDetailDto ToCreateOrEditDto(PAEmployeeDetail entity)
    {
        return new CreateOrEditPAEmployeeDetailDto
        {
            Id = entity.Id,
            PAEmployeeId = entity.PAEmployeeId,
            KpiInfoId = entity.KpiInfoId,
            Weight = entity.Weight,
            Target = entity.Target
        };
    }
}
