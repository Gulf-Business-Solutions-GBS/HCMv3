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
using HCMv3.GovtRelationsManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Application service for <see cref="SponsorSignatory"/> (ported from legacy XAF SponsorSignatory business
/// object, one of <see cref="Sponsor"/>'s aggregated child collections -- see <see cref="Sponsor"/>
/// remarks). Ports the legacy <c>PerformValidation</c> parity: no duplicate Signatory per Sponsor.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorSignatories)]
public class SponsorSignatoriesAppService : HCMv3AppServiceBase, ISponsorSignatoriesAppService
{
    private readonly IRepository<SponsorSignatory> _repository;

    public SponsorSignatoriesAppService(IRepository<SponsorSignatory> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SponsorSignatoryDto>> GetAll(GetAllSponsorSignatoriesInput input)
    {
        var query = _repository.GetAll()
            .Include(s => s.Sponsor)
            .Include(s => s.Signatory)
            .WhereIf(input.SponsorId.HasValue, s => s.SponsorId == input.SponsorId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SponsorSignatoryDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSponsorSignatoryForEditOutput> GetSponsorSignatoryForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSponsorSignatoryForEditOutput
        {
            SponsorSignatory = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSponsorSignatoryDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorSignatories_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorSignatories_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSponsorSignatoryDto input)
    {
        var entity = MapToEntity(input, new SponsorSignatory());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorSignatories_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSponsorSignatoryDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static SponsorSignatory MapToEntity(CreateOrEditSponsorSignatoryDto input, SponsorSignatory entity)
    {
        entity.SponsorId = input.SponsorId;
        entity.SignatoryId = input.SignatoryId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: no duplicate AuthorizedSignatory for the same Sponsor.
    /// </summary>
    private async Task ValidateAsync(SponsorSignatory entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(s => s.Id != entity.Id &&
                           s.SponsorId == entity.SponsorId &&
                           s.SignatoryId == entity.SignatoryId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateSignatoryForSponsor"));
        }
    }

    private static SponsorSignatoryDto ToDto(SponsorSignatory entity)
    {
        return new SponsorSignatoryDto
        {
            Id = entity.Id,
            SponsorId = entity.SponsorId,
            SponsorName = entity.Sponsor?.NameEn,
            SignatoryId = entity.SignatoryId,
            SignatoryName = entity.Signatory?.NameEn
        };
    }

    private static CreateOrEditSponsorSignatoryDto ToCreateOrEditDto(SponsorSignatory entity)
    {
        return new CreateOrEditSponsorSignatoryDto
        {
            Id = entity.Id,
            SponsorId = entity.SponsorId,
            SignatoryId = entity.SignatoryId
        };
    }
}
