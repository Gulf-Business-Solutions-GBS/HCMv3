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
using HCMv3.GovtRelationsManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Application service for <see cref="SponsorJobTitle"/> (ported from legacy XAF SponsorJobTitle business
/// object, one of <see cref="Sponsor"/>'s aggregated child collections -- see <see cref="Sponsor"/>
/// remarks). Ports the legacy computed <c>CitizenQuotaAccrued</c>/<c>NonCitizenQuotaAccrued</c> getters as
/// server-side recomputation on every Create/Update (see <see cref="SponsorJobTitle"/> remarks), and the
/// legacy <c>PerformValidation</c> parity (CitizenPercent + NonCitizenPercent must equal 100).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorJobTitles)]
public class SponsorJobTitlesAppService : HCMv3AppServiceBase, ISponsorJobTitlesAppService
{
    private readonly IRepository<SponsorJobTitle> _repository;

    public SponsorJobTitlesAppService(IRepository<SponsorJobTitle> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SponsorJobTitleDto>> GetAll(GetAllSponsorJobTitlesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.Sponsor)
            .Include(t => t.GovtJobTitle)
            .WhereIf(input.SponsorId.HasValue, t => t.SponsorId == input.SponsorId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SponsorJobTitleDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSponsorJobTitleForEditOutput> GetSponsorJobTitleForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSponsorJobTitleForEditOutput
        {
            SponsorJobTitle = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSponsorJobTitleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorJobTitles_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorJobTitles_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSponsorJobTitleDto input)
    {
        var entity = MapToEntity(input, new SponsorJobTitle());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateAndComputeQuotas(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorJobTitles_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSponsorJobTitleDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateAndComputeQuotas(entity);

        await _repository.UpdateAsync(entity);
    }

    private static SponsorJobTitle MapToEntity(CreateOrEditSponsorJobTitleDto input, SponsorJobTitle entity)
    {
        entity.SponsorId = input.SponsorId;
        entity.GovtJobTitleId = input.GovtJobTitleId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.NoofPosition = input.NoofPosition;
        entity.CitizenPercent = input.CitizenPercent;
        entity.CitizenQuotaUsed = input.CitizenQuotaUsed;
        entity.NonCitizenPercent = input.NonCitizenPercent;
        entity.NonCitizenQuotaUsed = input.NonCitizenQuotaUsed;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity (date range, percent must equal 100) plus recomputation of the
    /// legacy computed <c>CitizenQuotaAccrued</c> (floor) / <c>NonCitizenQuotaAccrued</c> (ceiling) getters.
    /// </summary>
    private void ValidateAndComputeQuotas(SponsorJobTitle entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        if (entity.CitizenPercent + entity.NonCitizenPercent != 100)
        {
            throw new UserFriendlyException(L("CitizenAndNonCitizenPercentMustEqual100"));
        }

        entity.CitizenQuotaAccrued = (int)Math.Floor(entity.NoofPosition * entity.CitizenPercent / 100.0);
        entity.NonCitizenQuotaAccrued = (int)Math.Ceiling(entity.NoofPosition * entity.NonCitizenPercent / 100.0);
    }

    private static SponsorJobTitleDto ToDto(SponsorJobTitle entity)
    {
        return new SponsorJobTitleDto
        {
            Id = entity.Id,
            SponsorId = entity.SponsorId,
            SponsorName = entity.Sponsor?.NameEn,
            GovtJobTitleId = entity.GovtJobTitleId,
            GovtJobTitleName = entity.GovtJobTitle?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            NoofPosition = entity.NoofPosition,
            CitizenPercent = entity.CitizenPercent,
            CitizenQuotaAccrued = entity.CitizenQuotaAccrued,
            CitizenQuotaUsed = entity.CitizenQuotaUsed,
            NonCitizenPercent = entity.NonCitizenPercent,
            NonCitizenQuotaAccrued = entity.NonCitizenQuotaAccrued,
            NonCitizenQuotaUsed = entity.NonCitizenQuotaUsed
        };
    }

    private static CreateOrEditSponsorJobTitleDto ToCreateOrEditDto(SponsorJobTitle entity)
    {
        return new CreateOrEditSponsorJobTitleDto
        {
            Id = entity.Id,
            SponsorId = entity.SponsorId,
            GovtJobTitleId = entity.GovtJobTitleId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            NoofPosition = entity.NoofPosition,
            CitizenPercent = entity.CitizenPercent,
            CitizenQuotaUsed = entity.CitizenQuotaUsed,
            NonCitizenPercent = entity.NonCitizenPercent,
            NonCitizenQuotaUsed = entity.NonCitizenQuotaUsed
        };
    }
}
