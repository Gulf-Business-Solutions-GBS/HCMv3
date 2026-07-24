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
public class PAQuarter3ReviewsAppService : HCMv3AppServiceBase, IPAQuarter3ReviewsAppService
{
    private readonly IRepository<PAQuarter3Review> _repository;

    public PAQuarter3ReviewsAppService(IRepository<PAQuarter3Review> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAQuarter3ReviewDto>> GetAll(GetAllPAQuarter3ReviewsInput input)
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

        return new PagedResultDto<PAQuarter3ReviewDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAQuarter3ReviewForEditOutput> GetPAQuarter3ReviewForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAQuarter3ReviewForEditOutput { PAQuarter3Review = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAQuarter3ReviewDto input)
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
    protected virtual async Task CreateAsync(CreateOrEditPAQuarter3ReviewDto input)
    {
        var entity = MapToEntity(input, new PAQuarter3Review());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAQuarter3ReviewDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static PAQuarter3Review MapToEntity(CreateOrEditPAQuarter3ReviewDto input, PAQuarter3Review entity)
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

    private async Task ValidateAsync(PAQuarter3Review entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.PAFormId == entity.PAFormId && e.KpiInfoId == entity.KpiInfoId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("KpiInfoAlreadyExistsForHeader"));
        }
    }

    private static PAQuarter3ReviewDto ToDto(PAQuarter3Review entity)
    {
        return new PAQuarter3ReviewDto
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

    private static CreateOrEditPAQuarter3ReviewDto ToCreateOrEditDto(PAQuarter3Review entity)
    {
        return new CreateOrEditPAQuarter3ReviewDto
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
