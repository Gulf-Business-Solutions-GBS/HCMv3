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
public class PAQuarter1ReviewsAppService : HCMv3AppServiceBase, IPAQuarter1ReviewsAppService
{
    private readonly IRepository<PAQuarter1Review> _repository;

    public PAQuarter1ReviewsAppService(IRepository<PAQuarter1Review> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAQuarter1ReviewDto>> GetAll(GetAllPAQuarter1ReviewsInput input)
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

        return new PagedResultDto<PAQuarter1ReviewDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAQuarter1ReviewForEditOutput> GetPAQuarter1ReviewForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAQuarter1ReviewForEditOutput { PAQuarter1Review = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAQuarter1ReviewDto input)
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
    protected virtual async Task CreateAsync(CreateOrEditPAQuarter1ReviewDto input)
    {
        var entity = MapToEntity(input, new PAQuarter1Review());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAQuarter1ReviewDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static PAQuarter1Review MapToEntity(CreateOrEditPAQuarter1ReviewDto input, PAQuarter1Review entity)
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

    private async Task ValidateAsync(PAQuarter1Review entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.PAFormId == entity.PAFormId && e.KpiInfoId == entity.KpiInfoId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("KpiInfoAlreadyExistsForHeader"));
        }
    }

    private static PAQuarter1ReviewDto ToDto(PAQuarter1Review entity)
    {
        return new PAQuarter1ReviewDto
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

    private static CreateOrEditPAQuarter1ReviewDto ToCreateOrEditDto(PAQuarter1Review entity)
    {
        return new CreateOrEditPAQuarter1ReviewDto
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
