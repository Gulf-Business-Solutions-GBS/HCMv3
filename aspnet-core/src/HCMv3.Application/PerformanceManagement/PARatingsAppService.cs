using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Application service for <see cref="PARating"/>. The legacy range-overlap validation runs, as legacy's
/// did, only when <c>ManageAppraisalSettings.EnableMarksInPAReview</c> is on: that is the mode in which a
/// score is mapped onto a rating by its From/To range, so two ranges must not overlap.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PARatings)]
public class PARatingsAppService : HCMv3AppServiceBase, IPARatingsAppService
{
    private readonly IRepository<PARating> _repository;
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;

    public PARatingsAppService(
        IRepository<PARating> repository,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor)
    {
        _repository = repository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    [HttpPost]
    public async Task<PagedResultDto<PARatingDto>> GetAll(GetAllPARatingsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), e =>
                e.Code.Contains(input.Filter) || e.NameEn.Contains(input.Filter) || e.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PARatingDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPARatingForEditOutput> GetPARatingForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPARatingForEditOutput { PARating = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPARatingDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PARatings_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PARatings_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPARatingDto input)
    {
        var entity = new PARating
        {
            TenantId = AbpSession.TenantId ?? 0,
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            IsCommentRequired = input.IsCommentRequired,
            Status = input.Status,
            MaxLimit = input.MaxLimit,
            DescriptionEn = input.DescriptionEn,
            DescriptionAr = input.DescriptionAr,
            FromRange = input.FromRange,
            ToRange = input.ToRange
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PARatings_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPARatingDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.IsCommentRequired = input.IsCommentRequired;
        entity.Status = input.Status;
        entity.MaxLimit = input.MaxLimit;
        entity.DescriptionEn = input.DescriptionEn;
        entity.DescriptionAr = input.DescriptionAr;
        entity.FromRange = input.FromRange;
        entity.ToRange = input.ToRange;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task AssignCodeIfEmptyAsync(PARating entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var codes = await _repository.GetAll()
            .Where(e => e.Code != null && e.Code != "")
            .Select(e => e.Code)
            .ToListAsync();

        var maxCode = codes.Select(c => int.TryParse(c, out var n) ? n : 0).DefaultIfEmpty(0).Max();
        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateAsync(PARating entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && (e.NameEn == entity.NameEn || e.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        // Legacy PARating.PerformValidation: with marks scoring on, rating ranges may not intersect.
        var appraisalSettings = await _sysSettingsAccessor.GetAppraisalSettingsAsync();
        if (appraisalSettings != null && appraisalSettings.EnableMarksInPAReview)
        {
            var overlaps = await _repository.GetAll()
                .AnyAsync(e => e.Id != entity.Id
                               && !(e.FromRange > entity.ToRange || e.ToRange < entity.FromRange));
            if (overlaps)
            {
                throw new UserFriendlyException(L("PARatingRangeOverlaps"));
            }
        }
    }

    private static PARatingDto ToDto(PARating entity)
    {
        return new PARatingDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IsCommentRequired = entity.IsCommentRequired,
            Status = entity.Status,
            MaxLimit = entity.MaxLimit,
            DescriptionEn = entity.DescriptionEn,
            DescriptionAr = entity.DescriptionAr,
            FromRange = entity.FromRange,
            ToRange = entity.ToRange
        };
    }

    private static CreateOrEditPARatingDto ToCreateOrEditDto(PARating entity)
    {
        return new CreateOrEditPARatingDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IsCommentRequired = entity.IsCommentRequired,
            Status = entity.Status,
            MaxLimit = entity.MaxLimit,
            DescriptionEn = entity.DescriptionEn,
            DescriptionAr = entity.DescriptionAr,
            FromRange = entity.FromRange,
            ToRange = entity.ToRange
        };
    }
}
