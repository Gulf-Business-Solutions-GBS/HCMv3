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
/// Application service for <see cref="SponsorGovtRepresentative"/> (ported from legacy XAF
/// SponsorGovtRepresentative business object, one of <see cref="Sponsor"/>'s aggregated child collections --
/// see <see cref="Sponsor"/> remarks). Ports the legacy <c>PerformValidation</c> parity: no overlapping date
/// ranges among siblings for the same Sponsor.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorGovtRepresentatives)]
public class SponsorGovtRepresentativesAppService : HCMv3AppServiceBase, ISponsorGovtRepresentativesAppService
{
    private readonly IRepository<SponsorGovtRepresentative> _repository;

    public SponsorGovtRepresentativesAppService(IRepository<SponsorGovtRepresentative> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SponsorGovtRepresentativeDto>> GetAll(GetAllSponsorGovtRepresentativesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.Sponsor)
            .Include(r => r.Employee)
            .WhereIf(input.SponsorId.HasValue, r => r.SponsorId == input.SponsorId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SponsorGovtRepresentativeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSponsorGovtRepresentativeForEditOutput> GetSponsorGovtRepresentativeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSponsorGovtRepresentativeForEditOutput
        {
            SponsorGovtRepresentative = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSponsorGovtRepresentativeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorGovtRepresentatives_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorGovtRepresentatives_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSponsorGovtRepresentativeDto input)
    {
        var entity = MapToEntity(input, new SponsorGovtRepresentative());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_SponsorGovtRepresentatives_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSponsorGovtRepresentativeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static SponsorGovtRepresentative MapToEntity(CreateOrEditSponsorGovtRepresentativeDto input, SponsorGovtRepresentative entity)
    {
        entity.SponsorId = input.SponsorId;
        entity.EmployeeId = input.EmployeeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: date range validity plus no overlapping date ranges among
    /// siblings for the same Sponsor.
    /// </summary>
    private async Task ValidateAsync(SponsorGovtRepresentative entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlap = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id &&
                           r.SponsorId == entity.SponsorId &&
                           !(r.StartDate.Date > entity.EndDate.Date || r.EndDate.Date < entity.StartDate.Date));
        if (overlap)
        {
            throw new UserFriendlyException(L("DateRangeOverlapFound"));
        }
    }

    private static SponsorGovtRepresentativeDto ToDto(SponsorGovtRepresentative entity)
    {
        return new SponsorGovtRepresentativeDto
        {
            Id = entity.Id,
            SponsorId = entity.SponsorId,
            SponsorName = entity.Sponsor?.NameEn,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.Employee?.FullNameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }

    private static CreateOrEditSponsorGovtRepresentativeDto ToCreateOrEditDto(SponsorGovtRepresentative entity)
    {
        return new CreateOrEditSponsorGovtRepresentativeDto
        {
            Id = entity.Id,
            SponsorId = entity.SponsorId,
            EmployeeId = entity.EmployeeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
