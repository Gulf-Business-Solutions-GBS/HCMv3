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
/// Application service for <see cref="PAEmployeeReviewer"/>. Ports the legacy max-reviewer-count check
/// (<see cref="FormType.MaxReviewerLimit"/> of the parent <see cref="PAEmployee"/>) and the legacy
/// <c>FillReview</c> (auto-create a <see cref="PAEmployeeRating"/> row for every existing
/// <see cref="PAEmployeeDetail"/> KPI line when a reviewer is added). See <see cref="PAEmployeeReviewer"/>
/// remarks for dropped legacy behaviors (candidate-list filtering, AllowedPeerCount/CanAddPeer via the
/// unported Workflow request-master coupling).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees)]
public class PAEmployeeReviewersAppService : HCMv3AppServiceBase, IPAEmployeeReviewersAppService
{
    private readonly IRepository<PAEmployeeReviewer> _repository;
    private readonly IRepository<PAEmployee> _paEmployeeRepository;
    private readonly IRepository<FormType> _formTypeRepository;
    private readonly IRepository<PAEmployeeDetail> _detailRepository;
    private readonly IRepository<PAEmployeeRating> _ratingRepository;

    public PAEmployeeReviewersAppService(
        IRepository<PAEmployeeReviewer> repository,
        IRepository<PAEmployee> paEmployeeRepository,
        IRepository<FormType> formTypeRepository,
        IRepository<PAEmployeeDetail> detailRepository,
        IRepository<PAEmployeeRating> ratingRepository)
    {
        _repository = repository;
        _paEmployeeRepository = paEmployeeRepository;
        _formTypeRepository = formTypeRepository;
        _detailRepository = detailRepository;
        _ratingRepository = ratingRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAEmployeeReviewerDto>> GetAll(GetAllPAEmployeeReviewersInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .WhereIf(input.PAEmployeeId.HasValue, e => e.PAEmployeeId == input.PAEmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PAEmployeeReviewerDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAEmployeeReviewerForEditOutput> GetPAEmployeeReviewerForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAEmployeeReviewerForEditOutput { PAEmployeeReviewer = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAEmployeeReviewerDto input)
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
        if (!entity.IsPeer)
        {
            throw new UserFriendlyException(L("OnlyPeerReviewerCanBeDeleted"));
        }

        var relatedRatings = await _ratingRepository.GetAll()
            .Where(r => r.PAEmployeeReviewerId == entity.Id)
            .ToListAsync();
        foreach (var rating in relatedRatings)
        {
            await _ratingRepository.DeleteAsync(rating);
        }

        await _repository.DeleteAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees_Edit)]
    protected virtual async Task CreateAsync(CreateOrEditPAEmployeeReviewerDto input)
    {
        var entity = MapToEntity(input, new PAEmployeeReviewer());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await CurrentUnitOfWork.SaveChangesAsync();

        await FillReviewAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAEmployees_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAEmployeeReviewerDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static PAEmployeeReviewer MapToEntity(CreateOrEditPAEmployeeReviewerDto input, PAEmployeeReviewer entity)
    {
        entity.PAEmployeeId = input.PAEmployeeId;
        entity.EmployeeId = input.EmployeeId;
        entity.IsPeer = input.IsPeer;
        entity.PeerAddedById = input.PeerAddedById;
        entity.CustomTextField1 = input.CustomTextField1;
        entity.CustomTextField2 = input.CustomTextField2;
        entity.CustomTextField3 = input.CustomTextField3;

        return entity;
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: no duplicate Person, max reviewer count.</summary>
    private async Task ValidateAsync(PAEmployeeReviewer entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.PAEmployeeId == entity.PAEmployeeId && e.EmployeeId == entity.EmployeeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("PersonAlreadyExistsAsReviewer"));
        }

        var paEmployee = await _paEmployeeRepository.GetAsync(entity.PAEmployeeId);
        var formType = await _formTypeRepository.GetAsync(paEmployee.FormTypeId);

        if (formType.MaxReviewerLimit > 0)
        {
            var reviewerCount = await _repository.GetAll()
                .CountAsync(e => e.Id != entity.Id && e.PAEmployeeId == entity.PAEmployeeId);
            reviewerCount += 1;

            if (reviewerCount > formType.MaxReviewerLimit)
            {
                throw new UserFriendlyException(L("MaxReviewerLimitReached", formType.MaxReviewerLimit));
            }
        }
    }

    /// <summary>Legacy <c>FillReview</c> parity.</summary>
    private async Task FillReviewAsync(PAEmployeeReviewer reviewer)
    {
        var details = await _detailRepository.GetAll()
            .Where(d => d.PAEmployeeId == reviewer.PAEmployeeId)
            .ToListAsync();

        foreach (var detail in details)
        {
            var exists = await _ratingRepository.GetAll()
                .AnyAsync(r => r.PAEmployeeReviewerId == reviewer.Id && r.PAEmployeeDetailId == detail.Id);
            if (!exists)
            {
                await _ratingRepository.InsertAsync(new PAEmployeeRating
                {
                    TenantId = AbpSession.TenantId ?? 0,
                    PAEmployeeId = reviewer.PAEmployeeId,
                    PAEmployeeReviewerId = reviewer.Id,
                    PAEmployeeDetailId = detail.Id
                });
            }
        }

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    private static PAEmployeeReviewerDto ToDto(PAEmployeeReviewer entity)
    {
        return new PAEmployeeReviewerDto
        {
            Id = entity.Id,
            PAEmployeeId = entity.PAEmployeeId,
            EmployeeId = entity.EmployeeId,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AverageRatingId = entity.AverageRatingId,
            AverageScore = entity.AverageScore,
            TotalMarks = entity.TotalMarks,
            IsPeer = entity.IsPeer,
            ReviewSubmitted = entity.ReviewSubmitted,
            PeerAddedById = entity.PeerAddedById,
            CustomTextField1 = entity.CustomTextField1,
            CustomTextField2 = entity.CustomTextField2,
            CustomTextField3 = entity.CustomTextField3
        };
    }

    private static CreateOrEditPAEmployeeReviewerDto ToCreateOrEditDto(PAEmployeeReviewer entity)
    {
        return new CreateOrEditPAEmployeeReviewerDto
        {
            Id = entity.Id,
            PAEmployeeId = entity.PAEmployeeId,
            EmployeeId = entity.EmployeeId,
            IsPeer = entity.IsPeer,
            PeerAddedById = entity.PeerAddedById,
            CustomTextField1 = entity.CustomTextField1,
            CustomTextField2 = entity.CustomTextField2,
            CustomTextField3 = entity.CustomTextField3
        };
    }
}
